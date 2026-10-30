using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>
    /// The prototype brick set: five basic bricks plus the wheel component
    /// (an axle plate and a wheel that clips onto its side pins).
    /// Sizes read as X by Z in studs; slopes descend towards -Z.
    /// </summary>
    public static class StarterBrickSet
    {
        public const string Brick1x2 = "brick_1x2";
        public const string Brick2x2 = "brick_2x2";
        public const string Brick2x4 = "brick_2x4";
        public const string Plate2x4 = "plate_2x4";
        public const string Slope2x2 = "slope_2x2";
        public const string AxlePlate2x2 = "axle_plate_2x2";
        public const string Wheel = "wheel";

        public static IReadOnlyList<BrickSpec> All { get; } = new[]
        {
            Basic(Brick1x2, "Brick 1x2", new Vector3Int(1, BrickGrid.PlatesPerBrick, 2)),
            Basic(Brick2x2, "Brick 2x2", new Vector3Int(2, BrickGrid.PlatesPerBrick, 2)),
            Basic(Brick2x4, "Brick 2x4", new Vector3Int(2, BrickGrid.PlatesPerBrick, 4)),
            Basic(Plate2x4, "Plate 2x4", new Vector3Int(2, 1, 4)),
            SlopeSpec(),
            AxlePlateSpec(),
            WheelSpec(),
        };

        public static BrickSpec Get(string id) => All.First(spec => spec.Id == id);

        static BrickSpec Basic(string id, string displayName, Vector3Int size) =>
            new(id, displayName, size, BrickShape.Box,
                StandardConnectors.TopStuds(size).Concat(StandardConnectors.BottomSockets(size)));

        static BrickSpec SlopeSpec()
        {
            var size = new Vector3Int(2, BrickGrid.PlatesPerBrick, 2);
            return new BrickSpec(Slope2x2, "Slope 2x2", size, BrickShape.Slope,
                StandardConnectors.TopStuds(size, (_, z) => z == size.z - 1)
                    .Concat(StandardConnectors.BottomSockets(size)));
        }

        static BrickSpec AxlePlateSpec()
        {
            var size = new Vector3Int(2, 1, 2);
            // Pins sit at mid-plate height, centred along Z, on both outer side faces.
            var pins = new[]
            {
                new BrickConnector(ConnectorKind.Pin, new Vector3Int(2 * size.x - 1, 1, size.z - 1), Facing.Right),
                new BrickConnector(ConnectorKind.Pin, new Vector3Int(-1, 1, size.z - 1), Facing.Left),
            };
            return new BrickSpec(AxlePlate2x2, "Axle Plate 2x2", size, BrickShape.Box,
                StandardConnectors.TopStuds(size).Concat(StandardConnectors.BottomSockets(size)).Concat(pins));
        }

        static BrickSpec WheelSpec()
        {
            // Two studs across, so it fits a 1 x 5-plate x 2 box; the hub is open on both sides.
            var size = new Vector3Int(1, 5, 2);
            var hubY = size.y;      // half-plate units: centre of a 5-plate wheel
            var hubZ = size.z - 1;  // half-stud units: centre of a 2-stud wheel
            return new BrickSpec(Wheel, "Wheel", size, BrickShape.Wheel, new[]
            {
                new BrickConnector(ConnectorKind.Hole, new Vector3Int(-1, hubY, hubZ), Facing.Left),
                new BrickConnector(ConnectorKind.Hole, new Vector3Int(1, hubY, hubZ), Facing.Right),
            });
        }
    }
}
