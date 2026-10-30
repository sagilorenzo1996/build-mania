using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>Looks up brick definitions by id.</summary>
    [CreateAssetMenu(fileName = "BrickCatalog", menuName = "Build Mania/Brick Catalog")]
    public sealed class BrickCatalog : ScriptableObject
    {
        [SerializeField] List<BrickDefinition> bricks = new();

        Dictionary<string, BrickDefinition> byId;

        public IReadOnlyList<BrickDefinition> Bricks => bricks;

        public static BrickCatalog Create(IEnumerable<BrickDefinition> definitions)
        {
            var catalog = CreateInstance<BrickCatalog>();
            catalog.SetBricks(definitions);
            return catalog;
        }

        public void SetBricks(IEnumerable<BrickDefinition> definitions)
        {
            bricks = new List<BrickDefinition>(definitions);
            byId = null;
        }

        public bool TryGet(string id, out BrickDefinition definition)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, BrickDefinition>();
                foreach (var brick in bricks)
                    if (brick != null && !string.IsNullOrEmpty(brick.Id))
                        byId[brick.Id] = brick;
            }
            return byId.TryGetValue(id ?? string.Empty, out definition);
        }

        void OnValidate() => byId = null;
    }
}
