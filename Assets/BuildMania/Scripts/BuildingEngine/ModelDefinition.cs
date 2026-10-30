using System.Collections.Generic;
using Newtonsoft.Json;

namespace BuildMania.Building
{
    /// <summary>
    /// A buildable model as stored on disk: which bricks go where, in what colour and order.
    /// Positions are grid cells (see <see cref="BuildMania.Bricks.BrickGrid"/>), so files are
    /// exact and do not depend on the brick scale.
    /// </summary>
    public sealed class ModelDefinition
    {
        public const int CurrentFormatVersion = 1;

        [JsonProperty("formatVersion")] public int FormatVersion { get; set; } = CurrentFormatVersion;
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }

        /// <summary>Baseplate studs along X; 0 with <see cref="BaseplateDepth"/> 0 for a free-standing model.</summary>
        [JsonProperty("baseplateWidth")] public int BaseplateWidth { get; set; }
        /// <summary>Baseplate studs along Z.</summary>
        [JsonProperty("baseplateDepth")] public int BaseplateDepth { get; set; }

        [JsonProperty("bricks")] public List<ModelBrick> Bricks { get; set; } = new();

        /// <summary>Free-form details such as author, theme or pack.</summary>
        [JsonProperty("metadata")] public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>One brick in a <see cref="ModelDefinition"/>.</summary>
    public sealed class ModelBrick
    {
        /// <summary>The brick type's <see cref="BuildMania.Bricks.BrickDefinition.Id"/>.</summary>
        [JsonProperty("brick")] public string BrickId { get; set; }
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }
        [JsonProperty("z")] public int Z { get; set; }
        /// <summary>Quarter turns about the vertical axis, 0-3.</summary>
        [JsonProperty("rotation")] public int Rotation { get; set; }
        /// <summary>Index into the active theme's brick palette.</summary>
        [JsonProperty("color")] public int Color { get; set; }
        /// <summary>Instruction step, starting at 1. Bricks that share a step are added together.</summary>
        [JsonProperty("step")] public int Step { get; set; }
    }
}
