using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>
    /// Shows a <see cref="BrickAssembly"/> in the scene. The GameObject's transform is the
    /// grid origin: cell (0, 0, 0) sits at its position. Brick GameObjects follow the
    /// assembly, so undo and redo update the scene automatically.
    /// </summary>
    public sealed class BrickAssemblyView : MonoBehaviour
    {
        [Tooltip("Baseplate studs in X and Z. Zero for a free-standing build.")]
        [SerializeField] Vector2Int baseplateSize = new(32, 32);
        [SerializeField] BrickPalette palette;
        [SerializeField, Min(1)] int undoLimit = 200;
        [Tooltip("Furthest a released brick may jump to snap, in metres.")]
        [SerializeField, Min(0f)] float snapDistance = BrickGrid.Pitch;

        readonly Dictionary<int, GameObject> instances = new();

        public BrickAssembly Assembly { get; private set; }
        public BuildHistory History { get; private set; }

        /// <summary>Extra rule for snapping, for example only the targets of a guided step. Null allows any valid spot.</summary>
        public Func<BrickPlacement, bool> PlacementFilter { get; set; }

        /// <summary>Raised after <see cref="ResetAssembly"/> replaces the assembly and history.</summary>
        public event Action AssemblyReset;

        void Awake() => CreateAssembly(baseplateSize);

        void OnDestroy() => ReleaseAssembly();

        /// <summary>Empties the build and starts a new one. A zero baseplate makes a free-standing build.</summary>
        public void ResetAssembly(Vector2Int baseplate)
        {
            ReleaseAssembly();
            foreach (var instance in instances.Values)
                if (instance != null)
                    Destroy(instance);
            instances.Clear();
            CreateAssembly(baseplate);
            AssemblyReset?.Invoke();
        }

        /// <summary>Nearest valid placement for a brick held at this world pose.</summary>
        public bool TrySnap(BrickDefinition definition, Vector3 worldPosition, Quaternion worldRotation, out SnapResult snap)
        {
            var localPosition = transform.InverseTransformPoint(worldPosition);
            var rotation = GridRotation.FromRotation(Quaternion.Inverse(transform.rotation) * worldRotation);
            return BrickSnapper.TryFindPlacement(Assembly, definition, localPosition, rotation, out snap, snapDistance,
                accept: PlacementFilter);
        }

        /// <summary>Snaps and places a brick released at this world pose. Undoable.</summary>
        public bool TryPlace(BrickDefinition definition, Vector3 worldPosition, Quaternion worldRotation, int colorIndex)
        {
            if (!TrySnap(definition, worldPosition, worldRotation, out var snap))
                return false;
            var placement = new BrickPlacement(definition, snap.Position, snap.Rotation, colorIndex);
            return History.Execute(new PlaceBrickCommand(placement));
        }

        /// <summary>Detaches a placed brick. Undoable.</summary>
        public bool TryRemove(int brickId) => History.Execute(new RemoveBrickCommand(brickId));

        public Pose GridToWorld(Vector3Int position, int rotation) => new(
            transform.TransformPoint(BrickGrid.CellToLocal(position)),
            transform.rotation * GridRotation.ToQuaternion(rotation));

        public bool TryGetInstance(int brickId, out GameObject instance) => instances.TryGetValue(brickId, out instance);

        void CreateAssembly(Vector2Int baseplate)
        {
            baseplateSize = baseplate;
            Assembly = new BrickAssembly(baseplate);
            History = new BuildHistory(Assembly, undoLimit);
            Assembly.BrickAdded += Spawn;
            Assembly.BrickRemoved += Despawn;
        }

        void ReleaseAssembly()
        {
            if (Assembly == null)
                return;
            Assembly.BrickAdded -= Spawn;
            Assembly.BrickRemoved -= Despawn;
        }

        void Spawn(PlacedBrick brick)
        {
            var prefab = brick.Definition.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"Brick '{brick.Definition.Id}' has no prefab; it is in the assembly but not shown.", this);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = $"{brick.Definition.Id} #{brick.Id}";
            instance.transform.SetLocalPositionAndRotation(
                BrickGrid.CellToLocal(brick.Position),
                GridRotation.ToQuaternion(brick.Rotation));

            if (instance.TryGetComponent<BrickInstance>(out var marker))
                marker.PlacedId = brick.Id;

            var material = palette != null ? palette.Get(brick.ColorIndex) : null;
            if (material != null)
                foreach (var meshRenderer in instance.GetComponentsInChildren<MeshRenderer>())
                    meshRenderer.sharedMaterial = material;

            instances[brick.Id] = instance;
        }

        void Despawn(PlacedBrick brick)
        {
            if (!instances.Remove(brick.Id, out var instance) || instance == null)
                return;
            Destroy(instance);
        }
    }
}
