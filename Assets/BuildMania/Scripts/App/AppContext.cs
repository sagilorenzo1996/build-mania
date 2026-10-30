using BuildMania.Bricks;
using BuildMania.Building;
using BuildMania.Content;
using BuildMania.Interaction;

namespace BuildMania.App
{
    /// <summary>Choices carried from one step to the next.</summary>
    public sealed class Session
    {
        /// <summary>Catalog id of the model to build in the guided step.</summary>
        public string ModelId { get; set; }

        /// <summary>Saved creation to open in free build, or null for an empty baseplate.</summary>
        public string CreationId { get; set; }

        /// <summary>True to keep the bricks already on the build area when the next step starts.</summary>
        public bool KeepCurrentBuild { get; set; }
    }

    /// <summary>Everything a step needs: the build area, content, saves and navigation.</summary>
    public sealed class AppContext
    {
        public AppContext(AppFlow flow, BuildArea area, ModelCatalog models, BrickCatalog bricks, MenuPanel menu, ISaveStore saves, PlaytestLog log)
        {
            Flow = flow;
            Area = area;
            Models = models;
            Bricks = bricks;
            Menu = menu;
            Saves = saves;
            Log = log;
        }

        public AppFlow Flow { get; }
        public BuildArea Area { get; }
        public ModelCatalog Models { get; }
        public BrickCatalog Bricks { get; }
        public MenuPanel Menu { get; }
        public ISaveStore Saves { get; }
        public PlaytestLog Log { get; }
        public Session Session { get; } = new();
    }
}
