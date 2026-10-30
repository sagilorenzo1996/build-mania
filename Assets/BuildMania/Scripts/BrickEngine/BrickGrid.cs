using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>
    /// The brick dimension standard. Every brick position is a whole grid cell:
    /// X and Z count studs, Y counts plates. A full-height brick is three plates tall.
    /// </summary>
    /// <remarks>
    /// A cell's position is the centre of its bottom face. Connector positions use
    /// half-cell units so side connectors can sit between stud centres.
    /// The sizes are an initial standard chosen for controller handling in VR;
    /// revisit them after the first headset test. Everything else derives from them.
    /// </remarks>
    public static class BrickGrid
    {
        /// <summary>Distance between stud centres, in metres.</summary>
        public const float Pitch = 0.04f;

        /// <summary>Height of one plate, in metres.</summary>
        public const float PlateHeight = 0.016f;

        public const int PlatesPerBrick = 3;

        public const float StudRadius = Pitch * 0.3f;
        public const float StudHeight = PlateHeight * 0.5f;

        /// <summary>Inset on each side of a brick body so neighbouring bricks show a seam.</summary>
        public const float SeamGap = 0.0005f;

        public static Vector3 CellToLocal(Vector3Int cell) =>
            new(cell.x * Pitch, cell.y * PlateHeight, cell.z * Pitch);

        public static Vector3 LocalToCell(Vector3 local) =>
            new(local.x / Pitch, local.y / PlateHeight, local.z / Pitch);

        public static Vector3 HalfToLocal(Vector3Int half) =>
            new(half.x * Pitch * 0.5f, half.y * PlateHeight * 0.5f, half.z * Pitch * 0.5f);
    }
}
