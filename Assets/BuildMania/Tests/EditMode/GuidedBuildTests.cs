using System.IO;
using System.Linq;
using BuildMania.Bricks;
using BuildMania.Building;
using NUnit.Framework;
using UnityEngine;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    public class GuidedBuildTests : BrickTestFixture
    {
        const string StarterCarPath = "Assets/BuildMania/Data/Models/starter_car.json";

        static ModelDefinition StarterCar() => ModelSerializer.FromJson(File.ReadAllText(StarterCarPath));

        GuidedBuild Guide(ModelDefinition model) => new(new InstructionPlan(model), Catalog());

        BrickPlacement Target(ModelBrick brick) =>
            new(Def(brick.BrickId), new Vector3Int(brick.X, brick.Y, brick.Z), brick.Rotation, brick.Color);

        [Test]
        public void InstructionPlan_GroupsBricksIntoOrderedSteps()
        {
            var model = new ModelDefinition { Id = "test" };
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Step = 3 });
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x2, Step = 1 });
            model.Bricks.Add(new ModelBrick { BrickId = Brick2x4, Step = 3 });

            var plan = new InstructionPlan(model);

            Assert.That(plan.Steps.Select(s => s.Number), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(plan.Steps[1].Bricks, Has.Count.EqualTo(2));
        }

        [Test]
        public void StarterCar_HasFiveStepsAndTenBricks()
        {
            var plan = new InstructionPlan(StarterCar());

            Assert.That(plan.Steps, Has.Count.EqualTo(5));
            Assert.That(plan.BrickCount, Is.EqualTo(10));
            Assert.That(plan.Steps[2].Bricks.All(b => b.BrickId == Wheel), Is.True);
        }

        [Test]
        public void OnlyTheCurrentStepsBricksAreAccepted()
        {
            var model = StarterCar();
            var guided = Guide(model);
            guided.Attach(new BrickAssembly());

            Assert.That(guided.StepIndex, Is.Zero);
            Assert.That(guided.Accepts(Target(model.Bricks[0])), Is.True, "chassis");
            Assert.That(guided.Accepts(Target(model.Bricks[3])), Is.False, "a wheel belongs to step 3");
            Assert.That(guided.Accepts(At(Plate2x4, 0, 3, 6)), Is.False, "chassis in the wrong place");
        }

        [Test]
        public void PlacingAStepsBricks_MovesToTheNextStep()
        {
            var model = StarterCar();
            var guided = Guide(model);
            var assembly = new BrickAssembly();
            guided.Attach(assembly);
            var changes = 0;
            guided.StepChanged += _ => changes++;

            assembly.TryPlace(Target(model.Bricks[0]), out _);

            Assert.That(guided.StepIndex, Is.EqualTo(1));
            Assert.That(guided.RemainingTargets, Has.Count.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void SymmetricBrickTurnedHalfway_StillCounts_ButASlopeDoesNot()
        {
            var guided = Guide(StarterCar());
            guided.Attach(new BrickAssembly(), completedSteps: 3);
            Assert.That(guided.StepIndex, Is.EqualTo(3));

            // Target: brick_2x2 at (7,4,8) and slope_2x2 at (7,4,6), both unturned.
            Assert.That(guided.Accepts(At(Brick2x2, 8, 4, 9, rotation: 2)), Is.True);
            Assert.That(guided.Accepts(At(Slope2x2, 8, 4, 7, rotation: 2)), Is.False, "same space, but the slope faces backwards");
        }

        [Test]
        public void BuildingEveryStep_Completes_AndUndoReturnsToTheLastStep()
        {
            var model = StarterCar();
            var guided = Guide(model);
            var assembly = new BrickAssembly();
            var history = new BuildHistory(assembly);
            guided.Attach(assembly);
            var completed = false;
            guided.Completed += () => completed = true;

            foreach (var brick in model.Bricks.OrderBy(b => b.Step))
            {
                Assert.That(guided.Accepts(Target(brick)), Is.True, $"{brick.BrickId} in step {brick.Step}");
                Assert.That(history.Execute(new PlaceBrickCommand(Target(brick))), Is.True);
            }

            Assert.That(completed, Is.True);
            Assert.That(guided.IsComplete, Is.True);

            history.Undo();
            Assert.That(guided.IsComplete, Is.False);
            Assert.That(guided.StepIndex, Is.EqualTo(4));
        }

        [Test]
        public void Resuming_RebuildsTheCompletedSteps()
        {
            var guided = Guide(StarterCar());
            var assembly = new BrickAssembly();

            guided.Attach(assembly, completedSteps: 2);

            Assert.That(assembly.Count, Is.EqualTo(3));
            Assert.That(guided.StepIndex, Is.EqualTo(2));
            Assert.That(guided.Problems, Is.Empty);
        }

        [Test]
        public void UnknownBrickTypes_AreReported()
        {
            var model = new ModelDefinition { Id = "test" };
            model.Bricks.Add(new ModelBrick { BrickId = "brick_9x9", Step = 1 });

            Assert.That(Guide(model).Problems, Has.Count.EqualTo(1));
        }
    }
}
