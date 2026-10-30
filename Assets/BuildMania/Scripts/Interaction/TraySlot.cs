using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// A tray compartment showing one brick type. Grabbing it hands the player a new brick
    /// of that type, in the selected colour or in the colour a guided step asks for.
    /// </summary>
    public sealed class TraySlot : MonoBehaviour
    {
        const float ColliderMargin = 0.01f;

        [SerializeField] BrickDefinition definition;
        [SerializeField] BuildArea area;

        GameObject display;
        int? colourOverride;

        public BrickDefinition Definition => definition;

        /// <summary>Colour of the brick this slot hands out.</summary>
        public int ColorIndex => colourOverride ?? (area != null ? area.SelectedColor : 0);

        /// <summary>World pose of the displayed brick's pivot, where a new brick appears when taken.</summary>
        public Pose DisplayPose => display != null
            ? new Pose(display.transform.position, display.transform.rotation)
            : new Pose(transform.position, transform.rotation);

        public void Configure(BrickDefinition brick, BuildArea owner)
        {
            definition = brick;
            area = owner;
        }

        /// <summary>Fixes the colour (guided build), or null to follow the selected colour.</summary>
        public void SetColourOverride(int? colorIndex)
        {
            colourOverride = colorIndex;
            Recolour();
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
                area.SelectedColorChanged += OnSelectedColourChanged;
            Recolour();
        }

        void OnDestroy()
        {
            if (area != null)
                area.SelectedColorChanged -= OnSelectedColourChanged;
        }

        void OnSelectedColourChanged(int _) => Recolour();

        void Recolour()
        {
            var material = area != null ? area.MaterialFor(ColorIndex) : null;
            if (display == null || material == null)
                return;
            foreach (var displayRenderer in display.GetComponentsInChildren<MeshRenderer>())
                displayRenderer.sharedMaterial = material;
        }
    }
}
