using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>Undoes or redoes the last build step.</summary>
    public sealed class HistoryButton : PokeButton
    {
        public enum Kind
        {
            Undo,
            Redo,
        }

        [SerializeField] BuildArea area;
        [SerializeField] Kind kind;

        public void Configure(BuildArea owner, Kind buttonKind)
        {
            area = owner;
            kind = buttonKind;
        }

        protected override void OnPressed()
        {
            if (kind == Kind.Undo)
                area.Undo();
            else
                area.Redo();
        }
    }
}
