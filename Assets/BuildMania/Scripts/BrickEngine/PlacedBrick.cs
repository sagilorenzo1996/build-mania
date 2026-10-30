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
    }

    /// <summary>A connector in assembly space, in half-cell units.</summary>
    public readonly struct WorldConnector
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
