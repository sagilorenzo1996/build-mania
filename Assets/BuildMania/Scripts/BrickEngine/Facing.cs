using System;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>The direction a connector points, in grid axes.</summary>
    public enum Facing
    {
        Up,      // +Y
        Down,    // -Y
        Right,   // +X
        Left,    // -X
        Forward, // +Z
        Back,    // -Z
    }

    public static class FacingExtensions
    {
        public static Facing Opposite(this Facing facing) => facing switch
        {
            Facing.Up => Facing.Down,
            Facing.Down => Facing.Up,
            Facing.Right => Facing.Left,
            Facing.Left => Facing.Right,
            Facing.Forward => Facing.Back,
            Facing.Back => Facing.Forward,
            _ => throw new ArgumentOutOfRangeException(nameof(facing)),
        };

        public static Vector3Int ToVector(this Facing facing) => facing switch
        {
            Facing.Up => Vector3Int.up,
            Facing.Down => Vector3Int.down,
            Facing.Right => Vector3Int.right,
            Facing.Left => Vector3Int.left,
            Facing.Forward => new Vector3Int(0, 0, 1),
            Facing.Back => new Vector3Int(0, 0, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(facing)),
        };

        public static Facing FromVector(Vector3Int v)
        {
            if (v == Vector3Int.up) return Facing.Up;
            if (v == Vector3Int.down) return Facing.Down;
            if (v == Vector3Int.right) return Facing.Right;
            if (v == Vector3Int.left) return Facing.Left;
            if (v == new Vector3Int(0, 0, 1)) return Facing.Forward;
            if (v == new Vector3Int(0, 0, -1)) return Facing.Back;
            throw new ArgumentException($"{v} is not a unit grid axis.", nameof(v));
        }

        public static Facing Rotate(this Facing facing, int quarterTurns) =>
            FromVector(GridRotation.Rotate(facing.ToVector(), quarterTurns));
    }
}
