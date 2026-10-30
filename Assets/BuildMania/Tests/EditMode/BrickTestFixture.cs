using System.Collections.Generic;
using BuildMania.Bricks;
using NUnit.Framework;
using UnityEngine;

namespace BuildMania.Tests
{
    /// <summary>Creates starter brick definitions for a test and destroys them afterwards.</summary>
    public abstract class BrickTestFixture
    {
        readonly Dictionary<string, BrickDefinition> definitions = new();
        readonly List<Object> created = new();

        protected BrickDefinition Def(string id)
        {
            if (!definitions.TryGetValue(id, out var definition))
            {
                definition = BrickDefinition.Create(StarterBrickSet.Get(id));
                definitions[id] = definition;
                created.Add(definition);
            }
            return definition;
        }

        protected BrickCatalog Catalog()
        {
            var bricks = new List<BrickDefinition>();
            foreach (var spec in StarterBrickSet.All)
                bricks.Add(Def(spec.Id));
            var catalog = BrickCatalog.Create(bricks);
            created.Add(catalog);
            return catalog;
        }

        protected BrickPlacement At(string id, int x, int y, int z, int rotation = 0, int color = 0) =>
            new(Def(id), new Vector3Int(x, y, z), rotation, color);

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (var obj in created)
                Object.DestroyImmediate(obj);
            created.Clear();
            definitions.Clear();
        }
    }
}
