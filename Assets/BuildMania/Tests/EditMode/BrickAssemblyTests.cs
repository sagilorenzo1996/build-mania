using System.Linq;
using BuildMania.Bricks;
using NUnit.Framework;
using UnityEngine;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    public class BrickAssemblyTests : BrickTestFixture
    {
        static BrickAssembly WithBaseplate(int size = 8) => new(new Vector2Int(size, size));

        [Test]
        public void Brick_OnBaseplate_IsPlaced()
        {
            var assembly = WithBaseplate();

            Assert.That(assembly.TryPlace(At(Brick2x4, 1, 0, 1), out var placed), Is.EqualTo(PlacementResult.Ok));
            Assert.That(placed.Id, Is.EqualTo(1));
            Assert.That(assembly.TryGetBrickAt(new Vector3Int(2, 2, 4), out var occupant), Is.True);
            Assert.That(occupant.Id, Is.EqualTo(1));
        }

        [Test]
        public void Brick_FloatingAboveBaseplate_IsNotConnected()
        {
            var assembly = WithBaseplate();

            Assert.That(assembly.TryPlace(At(Brick2x2, 0, 1, 0), out _), Is.EqualTo(PlacementResult.NotConnected));
            Assert.That(assembly.Count, Is.Zero);
        }

        [Test]
        public void Brick_BelowBaseplate_IsRejected()
        {
            Assert.That(WithBaseplate().CheckPlacement(At(Brick2x2, 0, -1, 0)), Is.EqualTo(PlacementResult.BelowBaseplate));
        }

        [Test]
        public void Brick_OffTheBaseplateEdge_IsNotConnected()
        {
            Assert.That(WithBaseplate(4).CheckPlacement(At(Brick2x2, 10, 0, 10)), Is.EqualTo(PlacementResult.NotConnected));
        }

        [Test]
        public void Brick_OverlappingAnother_IsRejected()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Brick2x4, 0, 0, 0), out _);

            Assert.That(assembly.CheckPlacement(At(Brick2x2, 1, 0, 2)), Is.EqualTo(PlacementResult.Overlaps));
        }

        [Test]
        public void Brick_OnTopOfAnother_ConnectsThroughStuds()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Brick2x4, 0, 0, 0), out _);

            Assert.That(assembly.CheckPlacement(At(Brick2x2, 0, 3, 0)), Is.EqualTo(PlacementResult.Ok));
            // Overhanging the corner, held by a single stud.
            Assert.That(assembly.CheckPlacement(At(Brick2x2, 1, 3, 3)), Is.EqualTo(PlacementResult.Ok));
        }

        [Test]
        public void Brick_OnSlopedFace_IsNotConnected_ButOnSlopeStudsIs()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Slope2x2, 2, 0, 2), out _);

            // Front row (z = 2) is the sloped face with no studs.
            Assert.That(assembly.CheckPlacement(At(Brick1x2, 2, 3, 1)), Is.EqualTo(PlacementResult.NotConnected));
            // Back row (z = 3) has studs.
            Assert.That(assembly.CheckPlacement(At(Brick1x2, 2, 3, 3)), Is.EqualTo(PlacementResult.Ok));
        }

        [Test]
        public void RotatedBrick_OccupiesRotatedCells()
        {
            var assembly = WithBaseplate();

            // A quarter turn maps local (x, z) to (z, -x): the 2x4 now runs along X.
            Assert.That(assembly.TryPlace(At(Brick2x4, 0, 0, 5, rotation: 1), out _), Is.EqualTo(PlacementResult.Ok));
            Assert.That(assembly.TryGetBrickAt(new Vector3Int(3, 0, 4), out _), Is.True);
            Assert.That(assembly.TryGetBrickAt(new Vector3Int(0, 0, 6), out _), Is.False);
        }

        [Test]
        public void GridRotation_MatchesUnityRotation()
        {
            var vectors = new[] { new Vector3Int(1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(3, 2, -1) };
            for (var turns = 0; turns < 4; turns++)
            foreach (var v in vectors)
            {
                var expected = GridRotation.ToQuaternion(turns) * (Vector3)v;
                var actual = (Vector3)GridRotation.Rotate(v, turns);
                Assert.That(Vector3.Distance(expected, actual), Is.LessThan(1e-4f), $"{v} rotated {turns} quarter turns");
            }
        }

        [Test]
        public void FreeBuild_FirstBrickGoesAnywhere_NextMustConnect()
        {
            var assembly = new BrickAssembly();

            Assert.That(assembly.TryPlace(At(Plate2x4, 5, -2, 3), out _), Is.EqualTo(PlacementResult.Ok));
            Assert.That(assembly.CheckPlacement(At(Brick2x2, 20, 0, 0)), Is.EqualTo(PlacementResult.NotConnected));
            Assert.That(assembly.CheckPlacement(At(Brick2x2, 5, -1, 3)), Is.EqualTo(PlacementResult.Ok));
        }

        [Test]
        public void Removing_TopBrick_Succeeds()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Brick2x2, 0, 0, 0), out _);
            assembly.TryPlace(At(Brick2x2, 0, 3, 0), out var top);

            Assert.That(assembly.TryRemove(top.Id), Is.EqualTo(RemovalResult.Ok));
            Assert.That(assembly.Count, Is.EqualTo(1));
        }

        [Test]
        public void Removing_SupportingBrick_WouldDisconnect()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Brick2x2, 0, 0, 0), out var bottom);
            assembly.TryPlace(At(Brick2x2, 0, 3, 0), out _);

            Assert.That(assembly.TryRemove(bottom.Id), Is.EqualTo(RemovalResult.WouldDisconnect));
            Assert.That(assembly.Count, Is.EqualTo(2));
        }

        [Test]
        public void Removing_OneOfTwoPillars_KeepsBridgeConnected()
        {
            var assembly = WithBaseplate();
            assembly.TryPlace(At(Brick1x2, 0, 0, 0), out var left);
            assembly.TryPlace(At(Brick1x2, 3, 0, 0), out var right);
            // Plate turned to run along X, spanning both pillars.
            Assert.That(assembly.TryPlace(At(Plate2x4, 0, 3, 1, rotation: 1), out _), Is.EqualTo(PlacementResult.Ok));

            Assert.That(assembly.TryRemove(left.Id), Is.EqualTo(RemovalResult.Ok));
            Assert.That(assembly.TryRemove(right.Id), Is.EqualTo(RemovalResult.WouldDisconnect));
        }

        [Test]
        public void Wheels_ClipOntoBothAxlePlatePins()
        {
            var assembly = new BrickAssembly();
            assembly.TryPlace(At(AxlePlate2x2, 0, 0, 0), out var axle);

            Assert.That(assembly.TryPlace(At(Wheel, 2, -2, 0), out var rightWheel), Is.EqualTo(PlacementResult.Ok));
            Assert.That(assembly.TryPlace(At(Wheel, -1, -2, 0), out var leftWheel), Is.EqualTo(PlacementResult.Ok));
            Assert.That(assembly.GetNeighbours(axle.Id).ToArray(), Is.EquivalentTo(new[] { rightWheel.Id, leftWheel.Id }));
        }

        [Test]
        public void Wheel_AwayFromPin_IsNotConnected()
        {
            var assembly = new BrickAssembly();
            assembly.TryPlace(At(AxlePlate2x2, 0, 0, 0), out _);

            Assert.That(assembly.CheckPlacement(At(Wheel, 3, -2, 0)), Is.EqualTo(PlacementResult.NotConnected));
            Assert.That(assembly.CheckPlacement(At(Wheel, 2, -1, 0)), Is.EqualTo(PlacementResult.NotConnected));
        }
    }
}
