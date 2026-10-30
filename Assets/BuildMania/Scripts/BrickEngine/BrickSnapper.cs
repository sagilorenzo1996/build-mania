using UnityEngine;

namespace BuildMania.Bricks
{
    public readonly struct SnapResult
    {
        public SnapResult(Vector3Int position, int rotation, float distance)
        {
            Position = position;
            Rotation = rotation;
            Distance = distance;
        }

        public Vector3Int Position { get; }
        public int Rotation { get; }
        /// <summary>How far the brick moves to snap, in metres.</summary>
        public float Distance { get; }
    }

    /// <summary>Finds the nearest valid grid placement for a brick the player is holding.</summary>
    public static class BrickSnapper
    {
        /// <param name="localPosition">Brick pivot in assembly space, in metres.</param>
        /// <param name="rotation">Quarter turns about the vertical axis.</param>
        /// <param name="maxDistance">Furthest the brick may jump to snap, in metres.</param>
        public static bool TryFindPlacement(
            BrickAssembly assembly,
            BrickDefinition definition,
            Vector3 localPosition,
            int rotation,
            out SnapResult result,
            float maxDistance = BrickGrid.Pitch,
            int horizontalRadius = 1,
            int verticalRadius = BrickGrid.PlatesPerBrick)
        {
            result = default;
            var cell = BrickGrid.LocalToCell(localPosition);
            var centre = new Vector3Int(Mathf.RoundToInt(cell.x), Mathf.RoundToInt(cell.y), Mathf.RoundToInt(cell.z));
            var bestDistance = float.MaxValue;
            var found = false;

            for (var dx = -horizontalRadius; dx <= horizontalRadius; dx++)
            for (var dy = -verticalRadius; dy <= verticalRadius; dy++)
            for (var dz = -horizontalRadius; dz <= horizontalRadius; dz++)
            {
                var candidate = centre + new Vector3Int(dx, dy, dz);
                var distance = Vector3.Distance(BrickGrid.CellToLocal(candidate), localPosition);
                if (distance > maxDistance || distance >= bestDistance)
                    continue;
                if (assembly.CheckPlacement(new BrickPlacement(definition, candidate, rotation)) != PlacementResult.Ok)
                    continue;

                bestDistance = distance;
                result = new SnapResult(candidate, GridRotation.Normalize(rotation), distance);
                found = true;
            }

            return found;
        }
    }
}
