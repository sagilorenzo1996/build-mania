using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// A tray compartment showing one brick type in the selected colour.
    /// Grabbing it hands the player a new brick of that type.
    /// </summary>
    public sealed class TraySlot : MonoBehaviour
    {
        const float ColliderMargin = 0.01f;

        [SerializeField] BrickDefinition definition;
        [SerializeField] BuildArea area;

        GameObject display;

        public BrickDefinition Definition => definition;

        /// <summary>World pose of the displayed brick's pivot, where a new brick appears when taken.</summary>
        public Pose DisplayPose => display != null
            ? new Pose(display.transform.position, display.transform.rotation)
            : new Pose(transform.position, transform.rotation);

        public void Configure(BrickDefinition brick, BuildArea owner)
        {
            definition = brick;
            area = owner;
        }

        void Start()
        {
            if (definition == null || definition.Prefab == null)
                return;

            // Sit the display brick's bottom centre on the slot origin.
            display = Instantiate(definition.Prefab, transform);
            display.name = $"{definition.Id} (display)";
            var bounds = display.GetComponent<MeshFilter>().sharedMesh.bounds;
            display.transform.SetLocalPositionAndRotation(
                -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), Quaternion.identity);
            foreach (var displayCollider in display.GetComponentsInChildren<Collider>())
                displayCollider.enabled = false;

            var grabCollider = gameObject.GetComponent<BoxCollider>();
            if (grabCollider == null)
                grabCollider = gameObject.AddComponent<BoxCollider>();
            grabCollider.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            grabCollider.size = bounds.size + Vector3.one * ColliderMargin;

            if (area != null)
            {
                area.SelectedColorChanged += Recolour;
                Recolour(area.SelectedColor);
            }
        }

        void OnDestroy()
        {
            if (area != null)
                area.SelectedColorChanged -= Recolour;
        }

        void Recolour(int colorIndex)
        {
            var material = area.MaterialFor(colorIndex);
            if (display == null || material == null)
                return;
            foreach (var displayRenderer in display.GetComponentsInChildren<MeshRenderer>())
                displayRenderer.sharedMaterial = material;
        }
    }
}
