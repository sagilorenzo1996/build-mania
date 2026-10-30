using BuildMania.Bricks;
using NUnit.Framework;
using UnityEngine;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    public class BuildHistoryTests : BrickTestFixture
    {
        BrickAssembly assembly;
        BuildHistory history;

        [SetUp]
        public void CreateAssembly()
        {
            assembly = new BrickAssembly(new Vector2Int(8, 8));
            history = new BuildHistory(assembly);
        }

        [Test]
        public void Undo_Place_RemovesBrick_AndRedo_RestoresSameId()
        {
            var place = new PlaceBrickCommand(At(Brick2x2, 0, 0, 0));
            Assert.That(history.Execute(place), Is.True);
            var id = place.PlacedId;

            Assert.That(history.Undo(), Is.True);
            Assert.That(assembly.Count, Is.Zero);

            Assert.That(history.Redo(), Is.True);
            Assert.That(assembly.TryGetBrick(id, out _), Is.True);
        }

        [Test]
        public void Undo_Remove_RestoresBrick()
        {
            var place = new PlaceBrickCommand(At(Brick2x4, 0, 0, 0, color: 3));
            history.Execute(place);
            history.Execute(new RemoveBrickCommand(place.PlacedId));
            Assert.That(assembly.Count, Is.Zero);

            history.Undo();

            Assert.That(assembly.TryGetBrick(place.PlacedId, out var restored), Is.True);
            Assert.That(restored.ColorIndex, Is.EqualTo(3));
        }

        [Test]
        public void UndoRedo_ChainOfCommands_KeepsIdsConsistent()
        {
            var bottom = new PlaceBrickCommand(At(Brick2x2, 0, 0, 0));
            var top = new PlaceBrickCommand(At(Brick2x2, 0, 3, 0));
            history.Execute(bottom);
            history.Execute(top);
            history.Execute(new RemoveBrickCommand(top.PlacedId));

            history.Undo();
            history.Undo();
            history.Undo();
            Assert.That(assembly.Count, Is.Zero);

            history.Redo();
            history.Redo();
            history.Redo();
            Assert.That(assembly.Count, Is.EqualTo(1));
            Assert.That(assembly.TryGetBrick(bottom.PlacedId, out _), Is.True);
        }

        [Test]
        public void Execute_AfterUndo_ClearsRedo()
        {
            history.Execute(new PlaceBrickCommand(At(Brick2x2, 0, 0, 0)));
            history.Undo();

            history.Execute(new PlaceBrickCommand(At(Brick2x2, 4, 0, 4)));

            Assert.That(history.CanRedo, Is.False);
        }

        [Test]
        public void FailedCommand_IsNotRecorded()
        {
            var floating = new PlaceBrickCommand(At(Brick2x2, 0, 5, 0));

            Assert.That(history.Execute(floating), Is.False);
            Assert.That(floating.LastResult, Is.EqualTo(PlacementResult.NotConnected));
            Assert.That(history.CanUndo, Is.False);
        }

        [Test]
        public void BlockedRemoval_IsNotRecorded()
        {
            var bottom = new PlaceBrickCommand(At(Brick2x2, 0, 0, 0));
            history.Execute(bottom);
            history.Execute(new PlaceBrickCommand(At(Brick2x2, 0, 3, 0)));

            var remove = new RemoveBrickCommand(bottom.PlacedId);

            Assert.That(history.Execute(remove), Is.False);
            Assert.That(remove.LastResult, Is.EqualTo(RemovalResult.WouldDisconnect));
            Assert.That(assembly.Count, Is.EqualTo(2));
        }
    }
}
