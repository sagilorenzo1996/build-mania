using System;
using System.Collections.Generic;
using System.IO;
using BuildMania.Bricks;
using BuildMania.Building;
using BuildMania.Content;
using BuildMania.Interaction;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>
    /// Runs the player journey as a sequence of steps inside one scene, so passthrough and
    /// the build area stay in place while the player moves between Garage, building and play.
    /// </summary>
    public sealed class AppFlow : MonoBehaviour
    {
        [SerializeField] BuildArea area;
        [SerializeField] ModelCatalog models;
        [SerializeField] BrickCatalog bricks;
        [SerializeField] MenuPanel menu;
        [Tooltip("Keep progress and creations between sessions. Off for throwaway sessions.")]
        [SerializeField] bool persistSaves = true;

        readonly Dictionary<Type, AppStep> steps = new();
        ISaveStore saves;

        public AppContext Context { get; private set; }
        public AppStep Current { get; private set; }

        public event Action<AppStep> StepChanged;

        /// <summary>Wires the flow up from code; call before the first frame. Saves default to files on the device.</summary>
        public void Configure(BuildArea buildArea, ModelCatalog modelCatalog, BrickCatalog brickCatalog, MenuPanel menuPanel, ISaveStore saveStore = null)
        {
            area = buildArea;
            models = modelCatalog;
            bricks = brickCatalog;
            menu = menuPanel;
            saves = saveStore;
        }

        void Start()
        {
            var root = Path.Combine(Application.persistentDataPath, "BuildMania");
            saves ??= persistSaves ? new FileSaveStore(root) : new MemorySaveStore();
            var log = new PlaytestLog(persistSaves ? Path.Combine(root, "playtest-log.jsonl") : null);
            Context = new AppContext(this, area, models, bricks, menu, saves, log);

            Register(new GarageStep());
            Register(new GuidedBuildStep());
            Register(new FreeBuildStep());
            Register(new PlayStep());
            GoTo<GarageStep>();
        }

        void Update() => Current?.Tick();

        public void Register(AppStep step)
        {
            step.Bind(Context);
            steps[step.GetType()] = step;
        }

        public T Get<T>() where T : AppStep => (T)steps[typeof(T)];

        /// <summary>Leaves the current step and enters <typeparamref name="T"/> (re-entering restarts it).</summary>
        public void GoTo<T>() where T : AppStep
        {
            var next = steps[typeof(T)];
            Current?.Exit();
            Current = next;
            Context.Log.Record("step", next.Name);
            next.Enter();
            StepChanged?.Invoke(next);
        }
    }
}
