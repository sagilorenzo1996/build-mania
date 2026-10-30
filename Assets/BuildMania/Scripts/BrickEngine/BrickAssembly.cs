using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BuildMania.Bricks
{
    public enum PlacementResult
    {
        Ok,
        MissingDefinition,
        /// <summary>Overlaps a brick that is already placed.</summary>
        Overlaps,
        /// <summary>Goes below the baseplate surface.</summary>
        BelowBaseplate,
        /// <summary>No connector mates with the baseplate or an existing brick.</summary>
        NotConnected,
    }

    public enum RemovalResult
    {
        Ok,
        NotFound,
        /// <summary>Other bricks would be left with nothing holding them.</summary>
        WouldDisconnect,
    }

    /// <summary>
    /// The bricks in one build and the rules for adding and removing them.
    /// Every brick stays connected: to the baseplate when there is one, otherwise
    /// to the rest of the assembly (the first brick may go anywhere).
    /// </summary>
    public sealed class BrickAssembly
    {
        readonly SortedDictionary<int, PlacedBrick> bricks = new();
        readonly Dictionary<Vector3Int, int> occupancy = new();
        readonly Dictionary<Vector3Int, List<(int brickId, WorldConnector connector)>> connectorIndex = new();
        int nextId = 1;

        /// <param name="baseplateSize">Baseplate studs in X and Z, starting at cell (0, 0). Zero for none.</param>
        public BrickAssembly(Vector2Int baseplateSize = default)
        {
            BaseplateSize = baseplateSize;
        }

        public Vector2Int BaseplateSize { get; }
        public bool HasBaseplate => BaseplateSize.x > 0 && BaseplateSize.y > 0;
        public int Count => bricks.Count;

        /// <summary>Bricks in placement order.</summary>
        public IEnumerable<PlacedBrick> Bricks => bricks.Values;

        public event Action<PlacedBrick> BrickAdded;
        public event Action<PlacedBrick> BrickRemoved;

        public bool TryGetBrick(int id, out PlacedBrick brick) => bricks.TryGetValue(id, out brick);

        public bool TryGetBrickAt(Vector3Int cell, out PlacedBrick brick)
        {
            brick = null;
            return occupancy.TryGetValue(cell, out var id) && bricks.TryGetValue(id, out brick);
        }

        public PlacementResult CheckPlacement(in BrickPlacement placement)
        {
            if (placement.Definition == null)
                return PlacementResult.MissingDefinition;

            foreach (var cell in placement.Cells())
            {
                if (HasBaseplate && cell.y < 0)
                    return PlacementResult.BelowBaseplate;
                if (occupancy.ContainsKey(cell))
                    return PlacementResult.Overlaps;
            }

            if (!HasBaseplate && bricks.Count == 0)
                return PlacementResult.Ok;

            foreach (var connector in placement.Connectors())
                if (MatesBaseplate(connector) || MatingBricks(connector, excludeId: 0).Any())
                    return PlacementResult.Ok;

            return PlacementResult.NotConnected;
        }

        public PlacementResult TryPlace(in BrickPlacement placement, out PlacedBrick placed)
        {
            placed = null;
            var result = CheckPlacement(placement);
            if (result == PlacementResult.Ok)
                placed = Insert(placement, nextId);
            return result;
        }

        public RemovalResult CheckRemoval(int id)
        {
            if (!bricks.ContainsKey(id))
                return RemovalResult.NotFound;
            return CountStranded(removedId: id) == 0 ? RemovalResult.Ok : RemovalResult.WouldDisconnect;
        }

        public RemovalResult TryRemove(int id)
        {
            var result = CheckRemoval(id);
            if (result == RemovalResult.Ok)
                Delete(id);
            return result;
        }

        /// <summary>Ids of the bricks this brick is directly connected to.</summary>
        public IEnumerable<int> GetNeighbours(int id)
        {
            if (!bricks.TryGetValue(id, out var brick))
                return Enumerable.Empty<int>();
            return brick.Placement.Connectors()
                .SelectMany(c => MatingBricks(c, excludeId: id))
                .Distinct();
        }

        public bool IsOnBaseplate(int id) =>
            bricks.TryGetValue(id, out var brick) && brick.Placement.Connectors().Any(MatesBaseplate);

        /// <summary>Adds a brick with a known id, without validation. Used to restore history.</summary>
        internal PlacedBrick Insert(in BrickPlacement placement, int id)
        {
            if (bricks.ContainsKey(id))
                throw new InvalidOperationException($"Brick id {id} is already in use.");

            var brick = new PlacedBrick(id, placement);
            bricks.Add(id, brick);
            nextId = Math.Max(nextId, id + 1);

            foreach (var cell in placement.Cells())
                occupancy[cell] = id;
            foreach (var connector in placement.Connectors())
            {
                if (!connectorIndex.TryGetValue(connector.HalfPosition, out var list))
                    connectorIndex[connector.HalfPosition] = list = new List<(int, WorldConnector)>();
                list.Add((id, connector));
            }

            BrickAdded?.Invoke(brick);
            return brick;
        }

        /// <summary>Removes a brick without validation. Used to restore history.</summary>
        internal void Delete(int id)
        {
            if (!bricks.TryGetValue(id, out var brick))
                return;

            bricks.Remove(id);
            foreach (var cell in brick.Placement.Cells())
                occupancy.Remove(cell);
            foreach (var connector in brick.Placement.Connectors())
            {
                var list = connectorIndex[connector.HalfPosition];
                list.RemoveAll(entry => entry.brickId == id);
                if (list.Count == 0)
                    connectorIndex.Remove(connector.HalfPosition);
            }

            BrickRemoved?.Invoke(brick);
        }

        bool MatesBaseplate(WorldConnector connector)
        {
            if (!HasBaseplate || connector.Kind != ConnectorKind.Socket || connector.Facing != Facing.Down)
                return false;
            var half = connector.HalfPosition;
            if (half.y != 0 || half.x % 2 != 0 || half.z % 2 != 0)
                return false;
            var x = half.x / 2;
            var z = half.z / 2;
            return x >= 0 && z >= 0 && x < BaseplateSize.x && z < BaseplateSize.y;
        }

        IEnumerable<int> MatingBricks(WorldConnector connector, int excludeId)
        {
            if (!connectorIndex.TryGetValue(connector.HalfPosition, out var list))
                yield break;
            foreach (var (brickId, other) in list)
                if (brickId != excludeId && connector.Mates(other))
                    yield return brickId;
        }

        /// <summary>How many bricks would lose their connection if <paramref name="removedId"/> were taken out.</summary>
        int CountStranded(int removedId)
        {
            var remaining = bricks.Count - 1;
            if (remaining == 0)
                return 0;

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            if (HasBaseplate)
            {
                foreach (var brick in bricks.Values)
                    if (brick.Id != removedId && IsOnBaseplate(brick.Id) && visited.Add(brick.Id))
                        queue.Enqueue(brick.Id);
            }
            else
            {
                var start = bricks.Keys.First(id => id != removedId);
                visited.Add(start);
                queue.Enqueue(start);
            }

            while (queue.Count > 0)
            {
                foreach (var neighbour in GetNeighbours(queue.Dequeue()))
                    if (neighbour != removedId && visited.Add(neighbour))
                        queue.Enqueue(neighbour);
            }

            return remaining - visited.Count;
        }
    }
}
