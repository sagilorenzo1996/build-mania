using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BuildMania.Building
{
    /// <summary>How far the player has got with a guided model.</summary>
    public sealed class BuildProgress
    {
        [JsonProperty("modelId")] public string ModelId { get; set; }
        /// <summary>Instruction steps fully built; resuming starts at this step index.</summary>
        [JsonProperty("completedSteps")] public int CompletedSteps { get; set; }
        [JsonProperty("finished")] public bool Finished { get; set; }
        [JsonProperty("updatedUtc")] public string UpdatedUtc { get; set; }
    }

    /// <summary>Where build progress and the player's own creations are kept.</summary>
    public interface ISaveStore
    {
        /// <summary>Saved progress for a model, or null if none.</summary>
        BuildProgress LoadProgress(string modelId);
        void SaveProgress(BuildProgress progress);
        void ClearProgress(string modelId);

        IReadOnlyList<string> CreationIds();
        ModelDefinition LoadCreation(string id);
        void SaveCreation(ModelDefinition creation);
    }

    /// <summary>Keeps everything in memory; for tests and sessions that should not persist.</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        readonly Dictionary<string, string> progress = new();
        readonly Dictionary<string, string> creations = new();

        public BuildProgress LoadProgress(string modelId) =>
            progress.TryGetValue(modelId, out var json) ? JsonConvert.DeserializeObject<BuildProgress>(json) : null;

        public void SaveProgress(BuildProgress value) => progress[value.ModelId] = JsonConvert.SerializeObject(value);
        public void ClearProgress(string modelId) => progress.Remove(modelId);
        public IReadOnlyList<string> CreationIds() => creations.Keys.OrderBy(id => id).ToList();
        public ModelDefinition LoadCreation(string id) => creations.TryGetValue(id, out var json) ? ModelSerializer.FromJson(json) : null;
        public void SaveCreation(ModelDefinition creation) => creations[creation.Id] = ModelSerializer.ToJson(creation);
    }

    /// <summary>
    /// JSON files on the device, offline and private to the player:
    /// <c>progress/&lt;model&gt;.json</c> and <c>creations/&lt;id&gt;.json</c> under <see cref="Root"/>.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        public FileSaveStore(string root)
        {
            Root = root;
        }

        public string Root { get; }

        string ProgressPath(string modelId) => Path.Combine(Root, "progress", FileName(modelId));
        string CreationPath(string id) => Path.Combine(Root, "creations", FileName(id));

        public BuildProgress LoadProgress(string modelId)
        {
            var path = ProgressPath(modelId);
            return File.Exists(path) ? JsonConvert.DeserializeObject<BuildProgress>(File.ReadAllText(path)) : null;
        }

        public void SaveProgress(BuildProgress value)
        {
            value.UpdatedUtc = DateTime.UtcNow.ToString("o");
            Write(ProgressPath(value.ModelId), JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        public void ClearProgress(string modelId)
        {
            var path = ProgressPath(modelId);
            if (File.Exists(path))
                File.Delete(path);
        }

        public IReadOnlyList<string> CreationIds()
        {
            var folder = Path.Combine(Root, "creations");
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(id => id).ToList()
                : new List<string>();
        }

        public ModelDefinition LoadCreation(string id)
        {
            var path = CreationPath(id);
            return File.Exists(path) ? ModelSerializer.FromJson(File.ReadAllText(path)) : null;
        }

        public void SaveCreation(ModelDefinition creation) => Write(CreationPath(creation.Id), ModelSerializer.ToJson(creation));

        static void Write(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Write then move, so a crash mid-save never leaves a half-written file.
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, contents);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporary, path);
        }

        static string FileName(string id)
        {
            var safe = new string(id.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
            return safe + ".json";
        }
    }
}
