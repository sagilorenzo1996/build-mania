using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>
    /// The colours available to bricks, one shared material per colour so the
    /// SRP Batcher can batch them. Each visual theme has its own palette with
    /// the same colour order, so swapping themes keeps every brick's colour index.
    /// </summary>
    [CreateAssetMenu(fileName = "BrickPalette", menuName = "Build Mania/Brick Palette")]
    public sealed class BrickPalette : ScriptableObject
    {
        [SerializeField] List<Material> materials = new();

        public int Count => materials.Count;

        public Material Get(int colorIndex) =>
            materials.Count == 0 ? null : materials[Mathf.Clamp(colorIndex, 0, materials.Count - 1)];

        public void SetMaterials(IEnumerable<Material> palette) => materials = new List<Material>(palette);
    }
}
