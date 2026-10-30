using System.Collections.Generic;
using System.Linq;
using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Building
{
    /// <summary>Converts between live assemblies and stored model definitions.</summary>
    public static class ModelConverter
    {
        /// <summary>Captures an assembly as a model, one brick per step in placement order.</summary>
        public static ModelDefinition FromAssembly(BrickAssembly assembly, string id, string name)
        {
            var model = new ModelDefinition
            {
                Id = id,
                Name = name,
                BaseplateWidth = assembly.BaseplateSize.x,
                BaseplateDepth = assembly.BaseplateSize.y,
            };

            var step = 1;
            foreach (var brick in assembly.Bricks)
            {
                model.Bricks.Add(new ModelBrick
                {
                    BrickId = brick.Definition.Id,
                    X = brick.Position.x,
                    Y = brick.Position.y,
                    Z = brick.Position.z,
                    Rotation = brick.Rotation,
                    Color = brick.ColorIndex,
                    Step = step++,
                });
            }
            return model;
        }

        /// <summary>An empty assembly with the model's baseplate.</summary>
        public static BrickAssembly CreateAssembly(ModelDefinition model) =>
            new(new Vector2Int(model.BaseplateWidth, model.BaseplateDepth));

        /// <summary>
        /// Places the model's bricks into <paramref name="assembly"/> in step order, using the
        /// same connection rules as the player. Returns a description of each brick that could
        /// not be placed; an empty list means the whole model loaded.
        /// </summary>
        public static IReadOnlyList<string> LoadInto(ModelDefinition model, BrickAssembly assembly, BrickCatalog catalog)
        {
            var problems = new List<string>();
            var ordered = model.Bricks
                .Select((brick, index) => (brick, index))
                .OrderBy(entry => entry.brick.Step);

            foreach (var (brick, index) in ordered)
            {
                var label = $"Brick {index + 1} ({brick.BrickId} at {brick.X},{brick.Y},{brick.Z}, step {brick.Step})";
                if (!catalog.TryGet(brick.BrickId, out var definition))
                {
                    problems.Add($"{label}: unknown brick type.");
                    continue;
                }

                var placement = new BrickPlacement(definition, new Vector3Int(brick.X, brick.Y, brick.Z), brick.Rotation, brick.Color);
                var result = assembly.TryPlace(placement, out _);
                if (result != PlacementResult.Ok)
                    problems.Add($"{label}: {result}.");
            }
            return problems;
        }
    }
}
