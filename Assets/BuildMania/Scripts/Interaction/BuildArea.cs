using System;
using System.Collections;
using System.Collections.Generic;
using BuildMania.Bricks;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuildMania.Interaction
{
    /// <summary>
    /// A portable building station: the baseplate with its assembly, the brick tray and the
    /// colour selection. The whole station can be carried to any table by its handles.
    /// App steps decide what it offers: which bricks the tray shows, which spots accept a
    /// brick, and which hint ghosts show where bricks go.
    /// </summary>
    public sealed class BuildArea : MonoBehaviour
    {
        [SerializeField] BrickAssemblyView assemblyView;
        [SerializeField] BrickPalette palette;
        [SerializeField] Material ghostMaterial;
        [SerializeField] Material hintMaterial;
        [SerializeField] int selectedColor = 1;

        [Header("Start position")]
        [Tooltip("Move in front of the viewer once head tracking starts. Off when the area sits on a fixed table.")]
        [SerializeField] bool placeInFrontOfViewer = true;
        [Tooltip("Horizontal distance from the eyes to the baseplate centre, in metres. The tray sits nearer.")]
        [SerializeField] float viewerDistance = 0.85f;
        [Tooltip("How far below eye level the area sits, in metres.")]
        [SerializeField] float viewerDrop = 0.45f;

        /// <summary>Whether the area moves in front of the viewer when the scene starts.</summary>
        public bool PlaceInFrontOfViewer
        {
            get => placeInFrontOfViewer;
            set => placeInFrontOfViewer = value;
        }

        public BrickAssemblyView View => assemblyView;
        public BrickPalette Palette => palette;
        public Material GhostMaterial => ghostMaterial;

        public int SelectedColor
        {
            get => selectedColor;
            set
            {
                var clamped = palette != null ? Mathf.Clamp(value, 0, Mathf.Max(0, palette.Count - 1)) : value;
                if (clamped == selectedColor)
                    return;
                selectedColor = clamped;
                SelectedColorChanged?.Invoke(selectedColor);
            }
        }

        public event Action<int> SelectedColorChanged;

        readonly List<GameObject> hints = new();
        TraySlot[] traySlots;
        ColorSwatch[] swatches;
        HistoryButton[] historyButtons;

        public IReadOnlyList<TraySlot> TraySlots => traySlots;

        /// <summary>Extra rule for where bricks may snap, for example a guided step's targets. Null allows any valid spot.</summary>
        public Func<BrickPlacement, bool> PlacementFilter
        {
            get => assemblyView.PlacementFilter;
            set => assemblyView.PlacementFilter = value;
        }

        void Awake()
        {
            traySlots = GetComponentsInChildren<TraySlot>(true);
            swatches = GetComponentsInChildren<ColorSwatch>(true);
            historyButtons = GetComponentsInChildren<HistoryButton>(true);
        }

        /// <summary>
        /// Shows every tray brick in the selected colour, or, given <paramref name="onlyBricks"/>
        /// (brick id to colour), just those bricks in those colours.
        /// </summary>
        public void ShowTray(IReadOnlyDictionary<string, int> onlyBricks = null)
        {
            foreach (var slot in traySlots)
            {
                if (onlyBricks == null)
                {
                    slot.gameObject.SetActive(true);
                    slot.SetColourOverride(null);
                }
                else
                {
                    var wanted = onlyBricks.TryGetValue(slot.Definition.Id, out var colour);
                    slot.gameObject.SetActive(wanted);
                    slot.SetColourOverride(wanted ? colour : null);
                }
            }
        }

        public void HideTray()
        {
            foreach (var slot in traySlots)
                slot.gameObject.SetActive(false);
        }

        public void SetSwatchesVisible(bool visible)
        {
            foreach (var swatch in swatches)
                swatch.gameObject.SetActive(visible);
        }

        public void SetHistoryButtonsVisible(bool visible)
        {
            foreach (var button in historyButtons)
                button.gameObject.SetActive(visible);
        }

        /// <summary>Shows see-through hint bricks where the given placements go.</summary>
        public void ShowHints(IEnumerable<BrickPlacement> placements)
        {
            ClearHints();
            foreach (var placement in placements)
            {
                var source = placement.Definition.Prefab != null ? placement.Definition.Prefab.GetComponent<MeshFilter>() : null;
                if (source == null)
                    continue;
                var hint = new GameObject($"Hint {placement.Definition.Id}");
                hint.transform.SetParent(assemblyView.transform, false);
                hint.transform.SetLocalPositionAndRotation(
                    BrickGrid.CellToLocal(placement.Position), GridRotation.ToQuaternion(placement.Rotation));
                hint.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var hintRenderer = hint.AddComponent<MeshRenderer>();
                hintRenderer.sharedMaterial = hintMaterial != null ? hintMaterial : ghostMaterial;
                hintRenderer.shadowCastingMode = ShadowCastingMode.Off;
                hints.Add(hint);
            }
        }

        public void ClearHints()
        {
            foreach (var hint in hints)
                if (hint != null)
                    Destroy(hint);
            hints.Clear();
        }

        IEnumerator Start()
        {
            if (!placeInFrontOfViewer)
                yield break;

            // Head tracking needs a few frames before the camera pose is meaningful.
            for (var i = 0; i < 10; i++)
                yield return null;
            var viewer = Camera.main;
            if (viewer != null)
                PlaceInFrontOf(viewer.transform);
        }

        /// <summary>Puts the area level, in front of and below the viewer, facing them.</summary>
        public void PlaceInFrontOf(Transform viewer)
        {
            var forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(viewer.up, Vector3.up);
            forward.Normalize();
            transform.SetPositionAndRotation(
                viewer.position + forward * viewerDistance + Vector3.down * viewerDrop,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        public Material MaterialFor(int colorIndex) => palette != null ? palette.Get(colorIndex) : null;

        public bool TrySnap(BrickDefinition definition, Pose held, out SnapResult snap) =>
            assemblyView.TrySnap(definition, held.position, held.rotation, out snap);

        public Pose SnapPose(SnapResult snap) => assemblyView.GridToWorld(snap.Position, snap.Rotation);

        /// <summary>Adds a brick at a snapped position. Undoable.</summary>
        public bool Place(BrickDefinition definition, SnapResult snap, int colorIndex) =>
            assemblyView.History.Execute(new PlaceBrickCommand(
                new BrickPlacement(definition, snap.Position, snap.Rotation, colorIndex)));

        /// <summary>Takes a brick off the build if nothing else depends on it. Undoable.</summary>
        public bool TryDetach(int brickId, out PlacedBrick brick) =>
            assemblyView.Assembly.TryGetBrick(brickId, out brick) && assemblyView.TryRemove(brickId);

        public bool Owns(BrickInstance instance) =>
            instance != null && instance.PlacedId != 0 && instance.transform.IsChildOf(assemblyView.transform);

        public bool Undo() => assemblyView.History.Undo();
        public bool Redo() => assemblyView.History.Redo();
    }
}
