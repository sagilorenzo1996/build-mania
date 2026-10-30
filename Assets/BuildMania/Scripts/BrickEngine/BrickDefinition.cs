using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>How a brick's mesh is shaped. Occupancy is always the full size box.</summary>
    public enum BrickShape
    {
        Box,
        Slope,
        Wheel,
    }

    /// <summary>
    /// A reusable brick type: the cells it occupies, its connectors and its prefab.
    /// Models refer to bricks by <see cref="Id"/>, so ids must stay stable once published.
    /// </summary>
    [CreateAssetMenu(fileName = "Brick", menuName = "Build Mania/Brick Definition")]
    public sealed class BrickDefinition : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [Tooltip("Occupied cells: X and Z in studs, Y in plates.")]
        [SerializeField] Vector3Int size = new(2, BrickGrid.PlatesPerBrick, 2);
        [SerializeField] BrickShape shape;
        [SerializeField] List<BrickConnector> connectors = new();
        [SerializeField] GameObject prefab;

        public string Id => id;
        public string DisplayName => displayName;
        public Vector3Int Size => size;
        public BrickShape Shape => shape;
        public IReadOnlyList<BrickConnector> Connectors => connectors;
        public GameObject Prefab => prefab;

        public static BrickDefinition Create(BrickSpec spec, GameObject prefab = null)
        {
            var definition = CreateInstance<BrickDefinition>();
            definition.Configure(spec, prefab);
            return definition;
        }

        public void Configure(BrickSpec spec, GameObject prefab)
        {
            name = spec.Id;
            id = spec.Id;
            displayName = spec.DisplayName;
            size = spec.Size;
            shape = spec.Shape;
            connectors = new List<BrickConnector>(spec.Connectors);
            this.prefab = prefab;
        }

        /// <summary>Occupied cells relative to the anchor cell, before rotation.</summary>
        public IEnumerable<Vector3Int> LocalCells()
        {
            for (var x = 0; x < size.x; x++)
            for (var y = 0; y < size.y; y++)
            for (var z = 0; z < size.z; z++)
                yield return new Vector3Int(x, y, z);
        }
    }
}
