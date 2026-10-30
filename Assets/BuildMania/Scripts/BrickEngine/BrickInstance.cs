using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>Marks a GameObject as a brick and links it to its definition.</summary>
    [DisallowMultipleComponent]
    public sealed class BrickInstance : MonoBehaviour
    {
        [SerializeField] BrickDefinition definition;

        public BrickDefinition Definition => definition;

        /// <summary>Id in the owning assembly, or 0 while the brick is loose.</summary>
        public int PlacedId { get; internal set; }
    }
}
