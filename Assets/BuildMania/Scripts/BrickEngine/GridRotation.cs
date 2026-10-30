using UnityEngine;

namespace BuildMania.Bricks
{
    /// <summary>
    /// Bricks rotate about the vertical axis in quarter turns (0-3), matching
    /// <c>Quaternion.Euler(0, 90 * turns, 0)</c>.
    /// </summary>
    public static class GridRotation
    {
        public static int Normalize(int quarterTurns) => ((quarterTurns % 4) + 4) % 4;

        public static Vector3Int Rotate(Vector3Int v, int quarterTurns) => Normalize(quarterTurns) switch
        {
            1 => new Vector3Int(v.z, v.y, -v.x),
            2 => new Vector3Int(-v.x, v.y, -v.z),
            3 => new Vector3Int(-v.z, v.y, v.x),
            _ => v,
        };

        public static Quaternion ToQuaternion(int quarterTurns) =>
            Quaternion.Euler(0f, 90f * Normalize(quarterTurns), 0f);

        public static int FromYaw(float yawDegrees) =>
            Normalize(Mathf.RoundToInt(yawDegrees / 90f));

        /// <summary>
        /// Nearest quarter turn for a freely held rotation, ignoring tilt.
        /// </summary>
        public static int FromRotation(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
            {
                // Pointing straight up or down: use the right axis instead.
                var right = rotation * Vector3.right;
                return FromYaw(Mathf.Atan2(right.x, right.z) * Mathf.Rad2Deg - 90f);
            }
            return FromYaw(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
        }
    }
}
