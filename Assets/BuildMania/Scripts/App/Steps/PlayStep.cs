namespace BuildMania.App
{
    /// <summary>
    /// Phase 3 slot: "Build It. Play It." This step will hand the finished
    /// <see cref="Bricks.BrickAssembly"/> to the Play Engine, which converts it into one optimised
    /// drivable vehicle on a miniature track. For now it keeps the build and offers a way back.
    /// </summary>
    public sealed class PlayStep : AppStep
    {
        public override void Enter()
        {
            var area = Context.Area;
            area.PlacementFilter = null;
            area.ClearHints();
            area.HideTray();
            area.SetSwatchesVisible(false);
            area.SetHistoryButtonsVisible(false);

            Context.Menu.SetMenuButton(null);
            Context.Menu.SetTitle("Driving arrives in Phase 3. Your build is safe.");
            Context.Menu.ShowButtons(new[]
            {
                new MenuChoice("Back to building", () =>
                {
                    Context.Session.KeepCurrentBuild = true;
                    Context.Flow.GoTo<FreeBuildStep>();
                }),
                new MenuChoice("Garage", () => Context.Flow.GoTo<GarageStep>()),
            });
        }

        public override void Exit() => Context.Menu.ClearButtons();
    }
}
