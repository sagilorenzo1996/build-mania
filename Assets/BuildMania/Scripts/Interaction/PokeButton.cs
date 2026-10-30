using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// A button pressed by touching it with a fingertip or controller tip. It needs no hover
    /// or pointer, so it works with hands, controllers and gaze-first devices alike.
    /// </summary>
    public abstract class PokeButton : MonoBehaviour
    {
        [Tooltip("Local box above the button that counts as touching it, in metres.")]
        [SerializeField] Vector3 pressZone = new(0.08f, 0.04f, 0.08f);
        [SerializeField] Vector3 pressZoneCentre = new(0f, 0.02f, 0f);

        bool touched;

        public void ConfigureZone(Vector3 size, Vector3 centre)
        {
            pressZone = size;
            pressZoneCentre = centre;
        }

        protected abstract void OnPressed();

        void Update()
        {
            BrickHand toucher = null;
            foreach (var hand in BrickHand.Active)
            {
                if (hand.Input == null || !hand.Input.IsTracked || hand.IsHolding)
                    continue;
                if (Contains(hand.Input.PokePoint))
                {
                    toucher = hand;
                    break;
                }
            }

            var nowTouched = toucher != null;
            if (nowTouched && !touched)
            {
                OnPressed();
                toucher.Input.Pulse(0.4f, 0.05f);
            }
            touched = nowTouched;
        }

        bool Contains(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint) - pressZoneCentre;
            return Mathf.Abs(local.x) <= pressZone.x * 0.5f
                && Mathf.Abs(local.y) <= pressZone.y * 0.5f
                && Mathf.Abs(local.z) <= pressZone.z * 0.5f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(pressZoneCentre, pressZone);
        }
    }
}
