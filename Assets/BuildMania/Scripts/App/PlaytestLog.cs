using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>
    /// Local playtest metrics for the plan's validation round (time to first snap, time to
    /// finish a model, completion). One JSON line per event, kept on the device only.
    /// </summary>
    public sealed class PlaytestLog
    {
        [Serializable]
        struct Entry
        {
            public string utc;
            public float sessionSeconds;
            public string name;
            public string detail;
            public float seconds;
        }

        readonly string path;
        readonly List<string> recent = new();

        /// <param name="filePath">Where to append events, or null to keep them in memory only.</param>
        public PlaytestLog(string filePath)
        {
            path = filePath;
        }

        public IReadOnlyList<string> Recent => recent;

        public void Record(string name, string detail = null, float seconds = -1f)
        {
            var line = JsonUtility.ToJson(new Entry
            {
                utc = DateTime.UtcNow.ToString("o"),
                sessionSeconds = Time.realtimeSinceStartup,
                name = name,
                detail = detail,
                seconds = seconds,
            });
            recent.Add(line);
            if (recent.Count > 200)
                recent.RemoveAt(0);

            if (path == null)
                return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Playtest log not written: {e.Message}");
            }
        }
    }
}
