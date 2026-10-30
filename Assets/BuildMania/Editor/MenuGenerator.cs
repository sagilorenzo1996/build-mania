using BuildMania.App;
using BuildMania.Bricks;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Builds the build area's menu: the title sign behind the baseplate, the fixed Menu
    /// button, and the prefab for the menu buttons that appear on the tray.
    /// </summary>
    public static class MenuGenerator
    {
        const string ButtonPrefabPath = "Assets/BuildMania/Prefabs/MenuButton.prefab";
        const float P = BrickGrid.Pitch;
        const float H = BrickGrid.PlateHeight;
        const float BaseHeight = 0.6f * H;

        // Realistic palette indices.
        const int Black = 2, LightGrey = 5, DarkGrey = 6;

        public static MenuPanel AddMenu(Transform areaRoot, BrickPalette palette, Vector3 titlePosition, Vector2 titleSize,
            Vector3 menuButtonPosition, Vector3 gridOrigin, Vector2 cellSize)
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                throw new System.InvalidOperationException("TextMesh Pro resources are missing. Run Build Mania > Import Text Resources first.");

            var buttonPrefab = CreateButtonPrefab(palette, new Vector2(cellSize.x - 0.4f * P, cellSize.y - 0.3f * P));

            var menuRoot = BuildAreaGenerator.Child(areaRoot, "Menu", Vector3.zero);
            var panel = menuRoot.gameObject.AddComponent<MenuPanel>();

            // Title on an upright sign behind the baseplate, leaning back slightly: in the
            // player's forward view (narrow-FOV friendly) and readable against any room.
            var sign = BuildAreaGenerator.Child(menuRoot, "Title Sign", titlePosition);
            sign.localRotation = Quaternion.Euler(15f, 0f, 0f);
            var board = new MeshBuilder();
            board.AddBox(new Vector3(-titleSize.x * 0.5f - 0.01f, -titleSize.y * 0.5f - 0.008f, 0.001f),
                new Vector3(titleSize.x * 0.5f + 0.01f, titleSize.y * 0.5f + 0.008f, 0.006f));
            BuildAreaGenerator.AddVisual(sign, "Board", Vector3.zero, BuildAreaGenerator.SaveMesh(board.ToMesh("title_sign"), "title_sign"), palette.Get(DarkGrey));
            var title = CreateLabel(sign, "Title", Vector3.zero, titleSize, Color.white, 0.45f, Quaternion.identity);
            title.text = "Build Mania";

            var grid = BuildAreaGenerator.Child(menuRoot, "Buttons", gridOrigin);
            var menuButton = BuildButton(menuRoot, "Menu Button", menuButtonPosition, new Vector2(2f * P, 2f * P), palette, "Menu");

            BuildAreaGenerator.SetFields(panel,
                ("title", title), ("menuButton", menuButton), ("buttonPrefab", buttonPrefab), ("buttonGrid", grid),
                ("cellSize", cellSize), ("columns", 2), ("rows", 3));
            return panel;
        }

        static MenuButton CreateButtonPrefab(BrickPalette palette, Vector2 size)
        {
            var root = new GameObject("MenuButton");
            try
            {
                BuildButton(root.transform, null, Vector3.zero, size, palette, "Option");
                return PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath).GetComponent<MenuButton>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>A flat button with its label lying on top, readable from the player's side.</summary>
        static MenuButton BuildButton(Transform parent, string name, Vector3 position, Vector2 size, BrickPalette palette, string text)
        {
            var button = name == null ? parent : BuildAreaGenerator.Child(parent, name, position);

            var baseMesh = new MeshBuilder();
            baseMesh.AddBox(new Vector3(-size.x * 0.5f, 0f, -size.y * 0.5f), new Vector3(size.x * 0.5f, BaseHeight, size.y * 0.5f));
            var meshName = $"menu_button_{Mathf.RoundToInt(size.x * 1000)}x{Mathf.RoundToInt(size.y * 1000)}";
            BuildAreaGenerator.AddVisual(button, "Base", Vector3.zero, BuildAreaGenerator.SaveMesh(baseMesh.ToMesh(meshName), meshName), palette.Get(LightGrey));

            var label = CreateLabel(button, "Label", new Vector3(0f, BaseHeight + 0.0005f, 0f), size * 0.88f, palette.Get(Black).GetColor("_BaseColor"), 0.3f);
            label.text = text;

            var menuButton = button.gameObject.AddComponent<MenuButton>();
            BuildAreaGenerator.SetFields(menuButton, ("label", label));
            menuButton.ConfigureZone(new Vector3(size.x, 0.04f, size.y), new Vector3(0f, BaseHeight + 0.02f, 0f));
            return menuButton;
        }

        /// <summary>
        /// Text facing the player on the -Z side; by default lying flat (facing up), as on the tray buttons.
        /// </summary>
        static TextMeshPro CreateLabel(Transform parent, string name, Vector3 position, Vector2 size, Color colour, float maxFontSize,
            Quaternion? rotation = null)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.SetLocalPositionAndRotation(position, rotation ?? Quaternion.Euler(90f, 0f, 0f));

            var label = labelObject.AddComponent<TextMeshPro>();
            label.font = TMP_Settings.defaultFontAsset;
            label.rectTransform.sizeDelta = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.05f;
            label.fontSizeMax = maxFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = colour;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
