using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BuildMania.Building
{
    /// <summary>Reads and writes <see cref="ModelDefinition"/> JSON.</summary>
    public static class ModelSerializer
    {
        static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string ToJson(ModelDefinition model) => JsonConvert.SerializeObject(model, Settings);

        /// <exception cref="FormatException">The JSON is not a model, or was written by a newer version of the game.</exception>
        public static ModelDefinition FromJson(string json)
        {
            ModelDefinition model;
            try
            {
                model = JsonConvert.DeserializeObject<ModelDefinition>(json, Settings);
            }
            catch (JsonException e)
            {
                throw new FormatException($"Model JSON could not be read: {e.Message}", e);
            }

            if (model == null)
                throw new FormatException("Model JSON is empty.");
            if (model.FormatVersion > ModelDefinition.CurrentFormatVersion)
                throw new FormatException(
                    $"Model format {model.FormatVersion} is newer than this game supports ({ModelDefinition.CurrentFormatVersion}).");

            model.Bricks ??= new List<ModelBrick>();
            model.Metadata ??= new Dictionary<string, string>();
            return model;
        }
    }
}
