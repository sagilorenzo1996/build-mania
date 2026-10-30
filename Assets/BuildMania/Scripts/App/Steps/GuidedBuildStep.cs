using System.Collections.Generic;
using System.Linq;
using BuildMania.Bricks;
using BuildMania.Building;
using BuildMania.Content;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>
    /// Step-by-step assembly of a catalog model. The tray offers only the current step's
    /// bricks in their model colours, hint ghosts show where they go, and only those spots
    /// accept a brick. Progress is saved after every completed step.
    /// </summary>
    public sealed class GuidedBuildStep : AppStep
    {
        ModelEntry entry;
        BuildProgress progress;
        BrickAssembly assembly;
        float startTime;
        bool firstSnapRecorded;

        /// <summary>The guided build in progress, while this step is active.</summary>
        public GuidedBuild Guided { get; private set; }

        public override void Enter()
        {
            if (!Context.Models.TryGet(Context.Session.ModelId, out entry))
            {
                Debug.LogError($"Model '{Context.Session.ModelId}' is not in the catalog.");
                Context.Flow.GoTo<GarageStep>();
                return;
            }

            var model = entry.Load();
            var area = Context.Area;
            area.View.ResetAssembly(new Vector2Int(model.BaseplateWidth, model.BaseplateDepth));
            assembly = area.View.Assembly;

            Guided = new GuidedBuild(new InstructionPlan(model), Context.Bricks);
            progress = Context.Saves.LoadProgress(entry.Id) ?? new BuildProgress { ModelId = entry.Id };
            Guided.Attach(assembly, progress.Finished ? Guided.StepCount : progress.CompletedSteps);
            foreach (var problem in Guided.Problems)
                Debug.LogWarning($"{entry.DisplayName}: {problem}");

            Guided.StepChanged += OnStepChanged;
            Guided.Completed += OnCompleted;
            assembly.BrickAdded += OnBrickAdded;

            area.PlacementFilter = Guided.Accepts;
            area.SetSwatchesVisible(false);
            startTime = Time.realtimeSinceStartup;
            firstSnapRecorded = false;
            Context.Log.Record("guided_start", entry.Id);
            ShowCurrentStep();
        }

        public override void Exit()
        {
            if (Guided != null)
            {
                Guided.StepChanged -= OnStepChanged;
                Guided.Completed -= OnCompleted;
                Guided.Detach();
                Guided = null;
            }
            if (assembly != null)
                assembly.BrickAdded -= OnBrickAdded;
            assembly = null;

            var area = Context.Area;
            area.PlacementFilter = null;
            area.ClearHints();
            Context.Menu.ClearButtons();
            Context.Menu.SetMenuButton(null);
        }

        void ShowCurrentStep()
        {
            var area = Context.Area;
            var menu = Context.Menu;
            menu.ClearButtons();

            if (Guided.IsComplete)
            {
                area.HideTray();
                area.ClearHints();
                area.SetHistoryButtonsVisible(false);
                menu.SetMenuButton(null);
                menu.SetTitle($"{entry.DisplayName} is complete!");
                menu.ShowButtons(new[]
                {
                    new MenuChoice("Customise it", () =>
                    {
                        Context.Session.KeepCurrentBuild = true;
                        Context.Flow.GoTo<FreeBuildStep>();
                    }),
                    new MenuChoice("Play", () =>
                    {
                        Context.Session.KeepCurrentBuild = true;
                        Context.Flow.GoTo<PlayStep>();
                    }),
                    new MenuChoice("Build again", StartOver),
                    new MenuChoice("Garage", () => Context.Flow.GoTo<GarageStep>()),
                });
                return;
            }

            var remaining = Guided.RemainingTargets;
            var wanted = new Dictionary<string, int>();
            foreach (var target in remaining)
                if (!wanted.ContainsKey(target.Definition.Id))
                    wanted[target.Definition.Id] = target.ColorIndex;

            area.ShowTray(wanted);
            area.ShowHints(remaining);
            area.SetHistoryButtonsVisible(true);
            menu.SetMenuButton(OpenMenu);
            var count = remaining.Count;
            menu.SetTitle($"{entry.DisplayName}: step {Guided.StepIndex + 1} of {Guided.StepCount}, add {count} brick{(count == 1 ? "" : "s")}");
        }

        void OpenMenu()
        {
            var area = Context.Area;
            area.HideTray();
            area.ClearHints();
            area.SetHistoryButtonsVisible(false);
            Context.Menu.ShowButtons(new[]
            {
                new MenuChoice("Keep building", ShowCurrentStep),
                new MenuChoice("Start over", StartOver),
                new MenuChoice("Garage", () => Context.Flow.GoTo<GarageStep>()),
            });
        }

        void StartOver()
        {
            Context.Saves.ClearProgress(entry.Id);
            Context.Flow.GoTo<GuidedBuildStep>();
        }

        void OnStepChanged(int stepIndex)
        {
            progress.CompletedSteps = stepIndex;
            progress.Finished = Guided.IsComplete;
            Context.Saves.SaveProgress(progress);
            Context.Log.Record("guided_step", $"{entry.Id}:{stepIndex}", Time.realtimeSinceStartup - startTime);
            if (!Context.Menu.IsOpen || Guided.IsComplete)
                ShowCurrentStep();
        }

        void OnCompleted()
        {
            Context.Log.Record("guided_complete", entry.Id, Time.realtimeSinceStartup - startTime);
        }

        void OnBrickAdded(PlacedBrick _)
        {
            if (firstSnapRecorded)
                return;
            firstSnapRecorded = true;
            Context.Log.Record("first_snap", entry.Id, Time.realtimeSinceStartup - startTime);
        }

        /// <summary>Bricks the current step still needs; empty once the model is complete.</summary>
        public IReadOnlyList<BrickPlacement> RemainingTargets =>
            Guided != null ? Guided.RemainingTargets : (IReadOnlyList<BrickPlacement>)new List<BrickPlacement>();

        /// <summary>Brick ids the tray currently offers.</summary>
        public IEnumerable<string> OfferedBricks =>
            Context.Area.TraySlots.Where(slot => slot.gameObject.activeSelf).Select(slot => slot.Definition.Id);
    }
}
