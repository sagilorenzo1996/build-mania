using BuildMania.Bricks;
using NUnit.Framework;
using UnityEngine;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    public class BrickSnapperTests : BrickTestFixture
    {
        static readonly Vector3 SmallOffset = new(0.012f, 0.005f, -0.009f);

        [Test]
        public void HeldNearBaseplateCell_SnapsToThatCell()
        {
            var assembly = new BrickAssembly(new Vector2Int(8, 8));
            var held = BrickGrid.CellToLocal(new Vector3Int(2, 0, 3)) + SmallOffset;

            Assert.That(BrickSnapper.TryFindPlacement(assembly, Def(Brick2x2), held, 0, out var snap), Is.True);
            Assert.That(snap.Position, Is.EqualTo(new Vector3Int(2, 0, 3)));
        }

        [Test]
        public void HeldJustAboveBrick_SnapsOntoItsStuds()
        {
            var assembly = new BrickAssembly(new Vector2Int(8, 8));
            assembly.TryPlace(At(Brick2x4, 0, 0, 0), out _);
            // Slightly low, as if pressing down onto the studs.
            var held = BrickGrid.CellToLocal(new Vector3Int(0, 3, 1)) + new Vector3(0.004f, -0.006f, 0.003f);

            Assert.That(BrickSnapper.TryFindPlacement(assembly, Def(Brick2x2), held, 0, out var snap), Is.True);
            Assert.That(snap.Position, Is.EqualTo(new Vector3Int(0, 3, 1)));
        }

        [Test]
        public void HeldFarFromAnyValidSpot_DoesNotSnap()
        {
            var assembly = new BrickAssembly(new Vector2Int(8, 8));
            var held = BrickGrid.CellToLocal(new Vector3Int(2, 30, 2));

            Assert.That(BrickSnapper.TryFindPlacement(assembly, Def(Brick2x2), held, 0, out _), Is.False);
        }

        [Test]
        public void AcceptFilter_OnlySnapsToAllowedPlacements()
        {
            var assembly = new BrickAssembly(new Vector2Int(8, 8));
            var held = BrickGrid.CellToLocal(new Vector3Int(2, 0, 3)) + SmallOffset;
            var allowed = new Vector3Int(3, 0, 3);

            Assert.That(BrickSnapper.TryFindPlacement(assembly, Def(Brick2x2), held, 0, out var snap,
                accept: p => p.Position == allowed), Is.True);
            Assert.That(snap.Position, Is.EqualTo(allowed));
            Assert.That(BrickSnapper.TryFindPlacement(assembly, Def(Brick2x2), held, 0, out _,
                accept: p => p.Position == new Vector3Int(6, 0, 6)), Is.False, "allowed spot is out of reach");
        }

        [TestCase(10f, 80f, -5f, 1)]
        [TestCase(0f, -100f, 0f, 3)]
        [TestCase(0f, 179f, 0f, 2)]
        [TestCase(30f, 20f, 15f, 0)]
        public void HeldRotation_RoundsToNearestQuarterTurn(float x, float y, float z, int expected)
        {
            Assert.That(GridRotation.FromRotation(Quaternion.Euler(x, y, z)), Is.EqualTo(expected));
        }
    }
}
