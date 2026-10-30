using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>Where and how a brick goes on the grid.</summary>
    public readonly struct BrickPlacement
    {
        public BrickPlacement(BrickDefinition definition, Vector3Int position, int rotation, int colorIndex = 0)
        {
            Definition = definition;
            Position = position;
            Rotation = GridRotation.Normalize(rotation);
            ColorIndex = colorIndex;
        }

        public BrickDefinition Definition { get; }
        /// <summary>Grid cell of the brick's anchor (its local cell 0,0,0).</summary>
        public Vector3Int Position { get; }
        /// <summary>Quarter turns about the vertical axis, 0-3.</summary>
        public int Rotation { get; }
        public int ColorIndex { get; }

        public IEnumerable<Vector3Int> Cells()
        {
            foreach (var cell in Definition.LocalCells())
                yield return Position + GridRotation.Rotate(cell, Rotation);
        }

        public IEnumerable<WorldConnector> Connectors()
        {
            var origin = Position * 2;
            foreach (var connector in Definition.Connectors)
                yield return new WorldConnector(
                    connector.Kind,
                    origin + GridRotation.Rotate(connector.HalfPosition, Rotation),
                    connector.Facing.Rotate(Rotation));
        }

        /// <summary>
        /// True when both placements put the same brick type in the same space with the same
        /// connectors, even if their anchor and rotation differ (a 2x2 brick turned half a turn).
        /// Colour is ignored.
        /// </summary>
        public bool IsEquivalentTo(in BrickPlacement other)
        {
            if (Definition == null || other.Definition == null || Definition.Id != other.Definition.Id)
                return false;
            if (Position == other.Position && Rotation == other.Rotation)
                return true;
            return new HashSet<Vector3Int>(Cells()).SetEquals(other.Cells())
                && new HashSet<WorldConnector>(Connectors()).SetEquals(other.Connectors());
        }
    }

    /// <summary>A connector in assembly space, in half-cell units.</summary>
    public readonly struct WorldConnector : System.IEquatable<WorldConnector>
    {
        public WorldConnector(ConnectorKind kind, Vector3Int halfPosition, Facing facing)
        {
            Kind = kind;
            HalfPosition = halfPosition;
            Facing = facing;
        }

        public ConnectorKind Kind { get; }
        public Vector3Int HalfPosition { get; }
        public Facing Facing { get; }

        public bool Mates(in WorldConnector other) =>
            other.HalfPosition == HalfPosition &&
            other.Facing == Facing.Opposite() &&
            BrickConnector.KindsMate(Kind, other.Kind);

        public bool Equals(WorldConnector other) =>
            Kind == other.Kind && HalfPosition == other.HalfPosition && Facing == other.Facing;

        public override bool Equals(object obj) => obj is WorldConnector other && Equals(other);

        public override int GetHashCode() => System.HashCode.Combine((int)Kind, HalfPosition, (int)Facing);
    }

    /// <summary>A brick that is part of an assembly.</summary>
    public sealed class PlacedBrick
    {
        internal PlacedBrick(int id, BrickPlacement placement)
        {
            Id = id;
            Placement = placement;
        }

        /// <summary>Unique within its assembly. Ids increase in placement order.</summary>
        public int Id { get; }
        public BrickPlacement Placement { get; }
        public BrickDefinition Definition => Placement.Definition;
        public Vector3Int Position => Placement.Position;
        public int Rotation => Placement.Rotation;
        public int ColorIndex => Placement.ColorIndex;
    }
}
