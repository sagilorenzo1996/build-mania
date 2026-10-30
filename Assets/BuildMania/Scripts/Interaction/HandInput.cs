using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// One hand's input, whether it comes from a tracked controller or a tracked hand.
    /// Building only needs a grab pose, a grab button (grip or pinch) and a poke point,
    /// so the same interaction works on Quest with controllers and on controller-free devices.
    /// </summary>
    public abstract class HandInput : MonoBehaviour
    {
        public abstract bool IsTracked { get; }

        /// <summary>Where a grabbed object is held: the controller grip or the pinch point.</summary>
        public abstract Pose GrabPose { get; }

        /// <summary>True while the grip is squeezed or the fingers are pinched.</summary>
        public abstract bool GrabPressed { get; }

        /// <summary>Controller tip or index fingertip, used to press buttons.</summary>
        public abstract Vector3 PokePoint { get; }

        /// <summary>Optional controller shortcut for undo.</summary>
        public virtual bool UndoPressedThisFrame => false;

        /// <summary>Optional controller shortcut for redo.</summary>
        public virtual bool RedoPressedThisFrame => false;

        /// <summary>Short haptic pulse, where the device supports it.</summary>
        public virtual void Pulse(float amplitude, float seconds) { }
    }
}
