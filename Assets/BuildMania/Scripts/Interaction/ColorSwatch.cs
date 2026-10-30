using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>Selects the colour for new bricks; the selected swatch is raised.</summary>
    public sealed class ColorSwatch : PokeButton
    {
        const float RaisedOffset = 0.006f;

        [SerializeField] BuildArea area;
        [SerializeField] int colorIndex;
        [SerializeField] Transform tile;

        Vector3 restPosition;

        public int ColorIndex => colorIndex;

        public void Configure(BuildArea owner, int index, Transform swatchTile)
        {
            area = owner;
            colorIndex = index;
            tile = swatchTile;
        }

        void Start()
        {
            if (tile != null)
                restPosition = tile.localPosition;
            area.SelectedColorChanged += ShowSelection;
            ShowSelection(area.SelectedColor);
        }

        void OnDestroy()
        {
            if (area != null)
                area.SelectedColorChanged -= ShowSelection;
        }

        protected override void OnPressed() => area.SelectedColor = colorIndex;

        void ShowSelection(int selected)
        {
            if (tile != null)
                tile.localPosition = restPosition + (selected == colorIndex ? Vector3.up * RaisedOffset : Vector3.zero);
        }
    }
}
