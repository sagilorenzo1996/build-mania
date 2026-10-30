using System;
using UnityEngine;

namespace BuildMania.Bricks
{
    public enum ConnectorKind
    {
        /// <summary>Raised stud on a top surface.</summary>
        Stud,
        /// <summary>Receptacle under a brick that a stud pushes into.</summary>
        Socket,
        /// <summary>Side axle pin, for example on a wheel holder.</summary>
        Pin,
        /// <summary>Hub hole that slides onto a pin, for example on a wheel.</summary>
        Hole,
    }

    /// <summary>
    /// One connection point on a brick. Two connectors mate when they sit at the same
    /// position, face each other, and their kinds fit (stud/socket or pin/hole).
    /// </summary>
    [Serializable]
    public struct BrickConnector
    {
        [SerializeField] ConnectorKind kind;
        [Tooltip("Position relative to the brick's anchor cell, in half-cell units (X/Z: half studs, Y: half plates).")]
        [SerializeField] Vector3Int halfPosition;
        [SerializeField] Facing facing;

        public BrickConnector(ConnectorKind kind, Vector3Int halfPosition, Facing facing)
        {
            this.kind = kind;
            this.halfPosition = halfPosition;
            this.facing = facing;
        }

        public ConnectorKind Kind => kind;
        public Vector3Int HalfPosition => halfPosition;
        public Facing Facing => facing;

        public static bool KindsMate(ConnectorKind a, ConnectorKind b) => (a, b) switch
        {
            (ConnectorKind.Stud, ConnectorKind.Socket) => true,
            (ConnectorKind.Socket, ConnectorKind.Stud) => true,
            (ConnectorKind.Pin, ConnectorKind.Hole) => true,
            (ConnectorKind.Hole, ConnectorKind.Pin) => true,
            _ => false,
        };
    }
}
