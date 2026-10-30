using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>Plain description of a brick type, used to create <see cref="BrickDefinition"/> assets.</summary>
    public sealed class BrickSpec
    {
        public BrickSpec(string id, string displayName, Vector3Int size, BrickShape shape, IEnumerable<BrickConnector> connectors)
        {
            Id = id;
            DisplayName = displayName;
            Size = size;
            Shape = shape;
            Connectors = new List<BrickConnector>(connectors);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public Vector3Int Size { get; }
        public BrickShape Shape { get; }
        public IReadOnlyList<BrickConnector> Connectors { get; }
    }

    /// <summary>Builders for the standard stud and socket grids.</summary>
    public static class StandardConnectors
    {
        /// <summary>One stud on top of each column, optionally filtered by (x, z).</summary>
        public static IEnumerable<BrickConnector> TopStuds(Vector3Int size, Func<int, int, bool> include = null)
        {
            for (var x = 0; x < size.x; x++)
            for (var z = 0; z < size.z; z++)
                if (include == null || include(x, z))
                    yield return new BrickConnector(ConnectorKind.Stud, new Vector3Int(2 * x, 2 * size.y, 2 * z), Facing.Up);
        }

        /// <summary>One socket under each column.</summary>
        public static IEnumerable<BrickConnector> BottomSockets(Vector3Int size)
        {
            for (var x = 0; x < size.x; x++)
            for (var z = 0; z < size.z; z++)
                yield return new BrickConnector(ConnectorKind.Socket, new Vector3Int(2 * x, 0, 2 * z), Facing.Down);
        }
    }
}
