namespace BuildMania.App
{
    /// <summary>
    /// One stage of the player journey (Garage, Guided Build, Free Build, Play...).
    /// A step sets up the build area and menu when entered and tidies up when left.
    /// To add a stage: subclass this and register it in <see cref="AppFlow"/>.
    /// </summary>
    public abstract class AppStep
    {
        protected AppContext Context { get; private set; }

        /// <summary>Name used in the playtest log.</summary>
        public virtual string Name => GetType().Name;

        internal void Bind(AppContext context) => Context = context;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
    }
}
