using BuildMania.Interaction;
using UnityEngine;

namespace BuildMania.XR
{
    /// <summary>
    /// Hand input from a Meta Touch controller or a tracked hand, whichever is active.
    /// Controllers grab with the grip trigger; hands grab by pinching thumb and index.
    /// A and X undo, B and Y redo (controllers only; hands use the tray buttons).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MetaHandInput : HandInput
    {
        [SerializeField] OVRInput.Controller controller = OVRInput.Controller.RTouch;
        [Tooltip("The rig's LeftControllerAnchor or RightControllerAnchor.")]
        [SerializeField] Transform controllerAnchor;
        [SerializeField] OVRHand hand;
        [SerializeField] OVRSkeleton skeleton;

        [Header("Controller")]
        [Tooltip("Grab point relative to the controller anchor, in metres.")]
        [SerializeField] Vector3 controllerGrabOffset = new(0f, -0.01f, 0.03f);
        [Tooltip("Poke point (controller tip) relative to the controller anchor, in metres.")]
        [SerializeField] Vector3 controllerPokeOffset = new(0f, -0.015f, 0.06f);
        [SerializeField, Range(0f, 1f)] float gripPressThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] float gripReleaseThreshold = 0.35f;

        [Header("Hand")]
        [SerializeField, Range(0f, 1f)] float pinchPressThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] float pinchReleaseThreshold = 0.5f;

        bool grabHeld;
        float pulseEndTime = -1f;
        Transform indexTip;
        Transform thumbTip;

        public void Configure(OVRInput.Controller touchController, Transform anchor, OVRHand trackedHand, OVRSkeleton handSkeleton)
        {
            controller = touchController;
            controllerAnchor = anchor;
            hand = trackedHand;
            skeleton = handSkeleton;
        }

        bool HandActive => hand != null && hand.IsTracked;

        bool ControllerActive =>
            controllerAnchor != null &&
            OVRInput.IsControllerConnected(controller) &&
            OVRInput.GetControllerPositionTracked(controller);

        public override bool IsTracked => HandActive || ControllerActive;

        public override Pose GrabPose => HandActive
            ? new Pose(PinchPoint(), hand.transform.rotation)
            : new Pose(controllerAnchor.TransformPoint(controllerGrabOffset), controllerAnchor.rotation);

        public override bool GrabPressed => grabHeld;

        public override Vector3 PokePoint => HandActive
            ? (FindTips() ? indexTip.position : hand.transform.position)
            : controllerAnchor.TransformPoint(controllerPokeOffset);

        public override bool UndoPressedThisFrame => !HandActive && OVRInput.GetDown(OVRInput.Button.One, controller);

        public override bool RedoPressedThisFrame => !HandActive && OVRInput.GetDown(OVRInput.Button.Two, controller);

        public override void Pulse(float amplitude, float seconds)
        {
            if (HandActive || !ControllerActive)
                return;
            OVRInput.SetControllerVibration(1f, amplitude, controller);
            pulseEndTime = Time.unscaledTime + seconds;
        }

        void Update()
        {
            float strength;
            if (HandActive)
                strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            else if (ControllerActive)
                strength = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
            else
                strength = 0f;

            var (press, release) = HandActive
                ? (pinchPressThreshold, pinchReleaseThreshold)
                : (gripPressThreshold, gripReleaseThreshold);
            grabHeld = grabHeld ? strength > release : strength >= press;

            if (pulseEndTime >= 0f && Time.unscaledTime >= pulseEndTime)
            {
                OVRInput.SetControllerVibration(0f, 0f, controller);
                pulseEndTime = -1f;
            }
        }

        void OnDisable()
        {
            if (pulseEndTime >= 0f)
                OVRInput.SetControllerVibration(0f, 0f, controller);
            pulseEndTime = -1f;
            grabHeld = false;
        }

        Vector3 PinchPoint() =>
            FindTips() ? (indexTip.position + thumbTip.position) * 0.5f : hand.transform.position;

        bool FindTips()
        {
            if (indexTip != null && thumbTip != null)
                return true;
            if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null)
                return false;

            // Bone ids overlap between the OpenXR and legacy hand skeletons, so pick by type.
            var type = skeleton.GetSkeletonType();
            var openXR = type == OVRSkeleton.SkeletonType.XRHandLeft || type == OVRSkeleton.SkeletonType.XRHandRight;
            var indexId = openXR ? OVRSkeleton.BoneId.XRHand_IndexTip : OVRSkeleton.BoneId.Hand_IndexTip;
            var thumbId = openXR ? OVRSkeleton.BoneId.XRHand_ThumbTip : OVRSkeleton.BoneId.Hand_ThumbTip;

            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == indexId)
                    indexTip = bone.Transform;
                else if (bone.Id == thumbId)
                    thumbTip = bone.Transform;
            }
            return indexTip != null && thumbTip != null;
        }
    }
}
