using System;
using BuildMania.Building;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>
    /// Build anything with the whole tray and any colour, on an empty baseplate, a saved
    /// creation, or a finished guided model ("customise it").
    /// </summary>
    public sealed class FreeBuildStep : AppStep
    {
        public static readonly Vector2Int Baseplate = new(16, 16);

        public override void Enter()
        {
            var session = Context.Session;
            var view = Context.Area.View;
            if (!session.KeepCurrentBuild)
            {
                var creation = session.CreationId != null ? Context.Saves.LoadCreation(session.CreationId) : null;
                if (creation != null)
                {
                    view.ResetAssembly(new Vector2Int(creation.BaseplateWidth, creation.BaseplateDepth));
                    foreach (var problem in ModelConverter.LoadInto(creation, view.Assembly, Context.Bricks))
                        Debug.LogWarning($"{creation.Name}: {problem}");
                }
                else
                {
                    view.ResetAssembly(Baseplate);
                }
            }
            session.KeepCurrentBuild = false;
            session.CreationId = null;

            Context.Area.PlacementFilter = null;
            Context.Area.ClearHints();
            ShowTools();
            Context.Menu.SetTitle("Free build: anything goes");
            Context.Log.Record("free_build_start");
        }

        public override void Exit()
        {
            Context.Menu.ClearButtons();
            Context.Menu.SetMenuButton(null);
        }

        void ShowTools()
        {
            var area = Context.Area;
            Context.Menu.ClearButtons();
            area.ShowTray();
            area.SetSwatchesVisible(true);
            area.SetHistoryButtonsVisible(true);
            Context.Menu.SetMenuButton(OpenMenu);
        }

        void OpenMenu()
        {
            var area = Context.Area;
            area.HideTray();
            area.SetSwatchesVisible(false);
            area.SetHistoryButtonsVisible(false);
            Context.Menu.ShowButtons(new[]
            {
                new MenuChoice("Keep building", ShowTools),
                new MenuChoice("Save creation", SaveCreation),
                new MenuChoice("Play", () =>
                {
                    Context.Session.KeepCurrentBuild = true;
                    Context.Flow.GoTo<PlayStep>();
                }),
                new MenuChoice("Garage", () => Context.Flow.GoTo<GarageStep>()),
            });
        }

        void SaveCreation()
        {
            var stamp = DateTime.Now;
            var creation = ModelConverter.FromAssembly(Context.Area.View.Assembly,
                $"creation-{stamp:yyyyMMdd-HHmmss}", $"Creation {stamp:d MMM HH:mm}");
            Context.Saves.SaveCreation(creation);
            Context.Log.Record("creation_saved", creation.Id);
            Context.Menu.SetTitle($"Saved as \"{creation.Name}\"");
            ShowTools();
        }
    }
}
