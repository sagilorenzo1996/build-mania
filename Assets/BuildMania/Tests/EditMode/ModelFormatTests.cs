using System;
using System.Linq;
using BuildMania.Bricks;
using BuildMania.Building;
using NUnit.Framework;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    public class ModelFormatTests : BrickTestFixture
    {
        const int Red = 1;
        const int Black = 2;

        /// <summary>A free-standing chassis: plate, two axle plates and four wheels.</summary>
        BrickAssembly BuildMiniCar()
        {
            var assembly = new BrickAssembly();
            Place(assembly, At(Plate2x4, 0, 0, 0, color: Red));
            Place(assembly, At(AxlePlate2x2, 0, -1, 0));
            Place(assembly, At(AxlePlate2x2, 0, -1, 2));
            Place(assembly, At(Wheel, 2, -3, 0, color: Black));
            Place(assembly, At(Wheel, -1, -3, 0, color: Black));
            Place(assembly, At(Wheel, 2, -3, 2, color: Black));
            Place(assembly, At(Wheel, -1, -3, 2, color: Black));
            return assembly;
        }

        static void Place(BrickAssembly assembly, BrickPlacement placement) =>
            Assert.That(assembly.TryPlace(placement, out _), Is.EqualTo(PlacementResult.Ok),
                $"{placement.Definition.Id} at {placement.Position}");

        static string[] Describe(BrickAssembly assembly) => assembly.Bricks
            .Select(b => $"{b.Definition.Id} {b.Position} r{b.Rotation} c{b.ColorIndex}")
            .ToArray();

        [Test]
        public void ExportedCar_SurvivesJson_AndRebuildsIdentically()
        {
            var original = BuildMiniCar();

            var json = ModelSerializer.ToJson(ModelConverter.FromAssembly(original, "mini_car", "Mini Car"));
            var model = ModelSerializer.FromJson(json);
            var rebuilt = ModelConverter.CreateAssembly(model);
            var problems = ModelConverter.LoadInto(model, rebuilt, Catalog());

            Assert.That(problems, Is.Empty);
            Assert.That(Describe(rebuilt), Is.EqualTo(Describe(original)));
            Assert.That(model.Id, Is.EqualTo("mini_car"));
            Assert.That(model.Bricks.Select(b => b.Step), Is.EqualTo(Enumerable.Range(1, 7)));
        }

        [Test]
        public void Json_UsesReadableFieldNames()
        {
            var json = ModelSerializer.ToJson(ModelConverter.FromAssembly(BuildMiniCar(), "mini_car", "Mini Car"));

            StringAssert.Contains("\"formatVersion\": 1", json);
            StringAssert.Contains("\"brick\": \"wheel\"", json);
            StringAssert.Contains("\"step\": 7", json);
        }

        [Test]
        public void Load_ReportsUnknownBricks_AndPlacesTheRest()
        {
            var model = new ModelDefinition { Id = "test", BaseplateWidth = 8, BaseplateDepth = 8 };
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Step = 1 });
            model.Bricks.Add(new ModelBrick { BrickId = "brick_9x9", X = 4, Step = 2 });

            var assembly = ModelConverter.CreateAssembly(model);
            var problems = ModelConverter.LoadInto(model, assembly, Catalog());

            Assert.That(problems, Has.Count.EqualTo(1));
            StringAssert.Contains("brick_9x9", problems[0]);
            Assert.That(assembly.Count, Is.EqualTo(1));
        }

        [Test]
        public void Load_ReportsBricksThatBreakTheRules()
        {
            var model = new ModelDefinition { Id = "test", BaseplateWidth = 8, BaseplateDepth = 8 };
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Y = 6, Step = 1 });

            var problems = ModelConverter.LoadInto(model, ModelConverter.CreateAssembly(model), Catalog());

            Assert.That(problems, Has.Count.EqualTo(1));
            StringAssert.Contains(nameof(PlacementResult.NotConnected), problems[0]);
        }

        [Test]
        public void Load_PlacesBricksInStepOrder()
        {
            // Listed top-first, but the steps put the supporting brick down first.
            var model = new ModelDefinition { Id = "test", BaseplateWidth = 8, BaseplateDepth = 8 };
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Y = 3, Step = 2 });
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Y = 0, Step = 1 });

            var problems = ModelConverter.LoadInto(model, ModelConverter.CreateAssembly(model), Catalog());

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void NewerFormatVersion_IsRejected()
        {
            Assert.Throws<FormatException>(() => ModelSerializer.FromJson("{\"formatVersion\": 99, \"bricks\": []}"));
        }

        [Test]
        public void InvalidJson_IsRejected()
        {
            Assert.Throws<FormatException>(() => ModelSerializer.FromJson("{ not json"));
        }
    }
}
