using System.Collections.Generic;
using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// Building with one hand: take bricks from the tray, place them with snapping, pull
    /// placed bricks off, and carry the build area by its handles. A brick released away
    /// from a valid spot is discarded; detaching is undoable, so nothing is lost by mistake.
    /// </summary>
    public sealed class BrickHand : MonoBehaviour
    {
        static readonly List<BrickHand> active = new();

        [SerializeField] HandInput input;
        [SerializeField] BuildArea area;
        [Tooltip("Reach around the grab point, in metres.")]
        [SerializeField, Min(0.005f)] float grabRadius = 0.04f;

        readonly Collider[] overlaps = new Collider[32];
        bool wasPressed;
        SnapGhost ghost;

        BrickDefinition heldDefinition;
        GameObject heldObject;
        int heldColor;
        Pose heldOffset;
        bool hasSnap;
        SnapResult lastSnap;

        AreaHandle carriedHandle;
        Vector3 carryOffset;
        Quaternion carryRotation;

        public static IReadOnlyList<BrickHand> Active => active;

        public HandInput Input => input;
        public bool IsHolding => heldObject != null || carriedHandle != null;
        public GameObject HeldObject => heldObject;
        public BrickDefinition HeldDefinition => heldDefinition;

        public void Configure(HandInput handInput, BuildArea buildArea)
        {
            input = handInput;
            area = buildArea;
        }

        void OnEnable() => active.Add(this);

        void OnDisable()
        {
            active.Remove(this);
            DropHeldBrick();
            carriedHandle = null;
            wasPressed = false;
        }

        void OnDestroy() => ghost?.Destroy();

        void Update()
        {
            if (input == null || area == null)
                return;

            if (!input.IsTracked)
            {
                if (wasPressed)
                    Release();
                wasPressed = false;
                return;
            }

            var pressed = input.GrabPressed;
            if (pressed && !wasPressed)
                TryGrab();
            else if (!pressed && wasPressed)
                Release();
            wasPressed = pressed;

            if (heldObject != null)
                UpdateHeldBrick();
            else if (carriedHandle != null)
                UpdateCarriedArea();

            if (!IsHolding)
            {
                if (input.UndoPressedThisFrame)
                    area.Undo();
                if (input.RedoPressedThisFrame)
                    area.Redo();
            }
        }

        void TryGrab()
        {
            var grab = input.GrabPose;
            Physics.SyncTransforms();
            var count = Physics.OverlapSphereNonAlloc(grab.position, grabRadius, overlaps, ~0, QueryTriggerInteraction.Collide);

            Component best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var target = GrabTargetOf(overlaps[i]);
                if (target == null)
                    continue;
                var distance = (overlaps[i].ClosestPoint(grab.position) - grab.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            switch (best)
            {
                case TraySlot slot:
                    Hold(slot.Definition, area.SelectedColor, Relative(grab, slot.DisplayPose));
                    input.Pulse(0.3f, 0.04f);
                    break;
                case BrickInstance brick:
                    Detach(brick, grab);
                    break;
                case AreaHandle handle:
                    StartCarrying(handle, grab);
                    break;
            }
        }

        Component GrabTargetOf(Collider candidate)
        {
            var slot = candidate.GetComponentInParent<TraySlot>();
            if (slot != null)
                return slot.Definition != null && slot.transform.IsChildOf(area.transform) ? slot : null;

            var handle = candidate.GetComponentInParent<AreaHandle>();
            if (handle != null)
                return handle.Area == area ? handle : null;

            var brick = candidate.GetComponentInParent<BrickInstance>();
            return area.Owns(brick) ? brick : null;
        }

        void Detach(BrickInstance brick, Pose grab)
        {
            var brickPose = new Pose(brick.transform.position, brick.transform.rotation);
            if (!area.TryDetach(brick.PlacedId, out var placed))
            {
                // Something is built on top of it.
                input.Pulse(0.8f, 0.12f);
                return;
            }
            Hold(placed.Definition, placed.ColorIndex, Relative(grab, brickPose));
            input.Pulse(0.3f, 0.04f);
        }

        void Hold(BrickDefinition definition, int colorIndex, Pose offset)
        {
            heldDefinition = definition;
            heldColor = colorIndex;
            heldOffset = offset;
            heldObject = Instantiate(definition.Prefab);
            heldObject.name = $"{definition.Id} (held)";
            foreach (var heldCollider in heldObject.GetComponentsInChildren<Collider>())
                heldCollider.enabled = false;
            var material = area.MaterialFor(colorIndex);
            if (material != null)
                foreach (var heldRenderer in heldObject.GetComponentsInChildren<MeshRenderer>())
                    heldRenderer.sharedMaterial = material;

            ghost ??= new SnapGhost(area.GhostMaterial);
            hasSnap = false;
            UpdateHeldBrick();
        }

        void UpdateHeldBrick()
        {
            var pose = Apply(input.GrabPose, heldOffset);
            heldObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

            if (area.TrySnap(heldDefinition, pose, out var snap))
            {
                ghost.Show(heldDefinition, area.SnapPose(snap));
                if (!hasSnap || snap.Position != lastSnap.Position || snap.Rotation != lastSnap.Rotation)
                    input.Pulse(0.15f, 0.02f);
                hasSnap = true;
                lastSnap = snap;
            }
            else
            {
                ghost.Hide();
                hasSnap = false;
            }
        }

        void Release()
        {
            if (heldObject != null)
            {
                var pose = new Pose(heldObject.transform.position, heldObject.transform.rotation);
                if (area.TrySnap(heldDefinition, pose, out var snap) && area.Place(heldDefinition, snap, heldColor))
                    input.Pulse(0.6f, 0.06f);
                DropHeldBrick();
            }
            carriedHandle = null;
        }

        void DropHeldBrick()
        {
            if (heldObject != null)
                Destroy(heldObject);
            heldObject = null;
            heldDefinition = null;
            ghost?.Hide();
        }

        void StartCarrying(AreaHandle handle, Pose grab)
        {
            carriedHandle = handle;
            var yaw = Yaw(grab.rotation);
            carryOffset = Quaternion.Inverse(yaw) * (area.transform.position - grab.position);
            carryRotation = Quaternion.Inverse(yaw) * area.transform.rotation;
            input.Pulse(0.3f, 0.04f);
        }

        void UpdateCarriedArea()
        {
            var grab = input.GrabPose;
            var yaw = Yaw(grab.rotation);
            area.transform.SetPositionAndRotation(grab.position + yaw * carryOffset, yaw * carryRotation);
        }

        /// <summary>Rotation about the vertical axis only, so carried things stay level.</summary>
        static Quaternion Yaw(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
            {
                // Pointing straight down or up: the up axis then points where the hand faces.
                forward = rotation * Vector3.up;
                forward.y = 0f;
            }
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        static Pose Relative(Pose parent, Pose child)
        {
            var inverse = Quaternion.Inverse(parent.rotation);
            return new Pose(inverse * (child.position - parent.position), inverse * child.rotation);
        }

        static Pose Apply(Pose parent, Pose offset) =>
            new(parent.position + parent.rotation * offset.position, parent.rotation * offset.rotation);
    }
}
