using System.Collections.Generic;

namespace BuildMania.App
{
    /// <summary>
    /// Home: choose a model to build (or continue one), start a free build, or reopen a saved
    /// creation. Phase 4 grows this into the full garage and gallery with 3D previews.
    /// </summary>
    public sealed class GarageStep : AppStep
    {
        public override void Enter()
        {
            var area = Context.Area;
            area.PlacementFilter = null;
            area.ClearHints();
            area.HideTray();
            area.SetSwatchesVisible(false);
            area.SetHistoryButtonsVisible(false);
            area.View.ResetAssembly(FreeBuildStep.Baseplate);

            Context.Menu.SetMenuButton(null);
            Context.Menu.SetTitle("Garage: what shall we build?");
            Context.Menu.ShowButtons(BuildItems());
        }

        public override void Exit() => Context.Menu.ClearButtons();

        List<MenuChoice> BuildItems()
        {
            var items = new List<MenuChoice>();
            foreach (var model in Context.Models.Models)
            {
                var progress = Context.Saves.LoadProgress(model.Id);
                var started = progress != null && (progress.CompletedSteps > 0 || progress.Finished);
                var label = !started
                    ? $"Build {model.DisplayName}"
                    : progress.Finished
                        ? $"{model.DisplayName} (built)"
                        : $"Continue {model.DisplayName}";
                var id = model.Id;
                items.Add(new MenuChoice(label, () =>
                {
                    Context.Session.ModelId = id;
                    Context.Flow.GoTo<GuidedBuildStep>();
                }));
            }

            items.Add(new MenuChoice("Free build", () =>
            {
                Context.Session.CreationId = null;
                Context.Session.KeepCurrentBuild = false;
                Context.Flow.GoTo<FreeBuildStep>();
            }));

            foreach (var creationId in Context.Saves.CreationIds())
            {
                var creation = Context.Saves.LoadCreation(creationId);
                var id = creationId;
                items.Add(new MenuChoice($"Open {creation?.Name ?? creationId}", () =>
                {
                    Context.Session.CreationId = id;
                    Context.Session.KeepCurrentBuild = false;
                    Context.Flow.GoTo<FreeBuildStep>();
                }));
            }
            return items;
        }
    }
}
