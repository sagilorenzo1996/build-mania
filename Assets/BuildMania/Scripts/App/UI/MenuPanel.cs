using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>A menu choice: the label shown and what happens when it is pressed.</summary>
    public readonly struct MenuChoice
    {
        public MenuChoice(string label, Action action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; }
        public Action Action { get; }
    }

    /// <summary>
    /// The build area's menu: a title sign behind the baseplate, a fixed Menu button, and a
    /// grid of buttons that takes over the tray surface while a menu is open. Everything is
    /// within reach and pressed by touch, so it works with hands, controllers and VR Glasses.
    /// </summary>
    public sealed class MenuPanel : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] MenuButton menuButton;
        [SerializeField] MenuButton buttonPrefab;
        [SerializeField] Transform buttonGrid;
        [Tooltip("Distance between button centres: X across, Y towards the player, in metres.")]
        [SerializeField] Vector2 cellSize = new(0.32f, 0.08f);
        [SerializeField, Min(1)] int columns = 2;
        [SerializeField, Min(1)] int rows = 3;

        readonly List<MenuButton> buttons = new();
        Action menuAction;
        IReadOnlyList<MenuChoice> items;
        int page;

        public string Title => title != null ? title.text : string.Empty;
        public IReadOnlyList<MenuButton> Buttons => buttons;
        public bool IsOpen => buttons.Count > 0;

        public void SetTitle(string text)
        {
            if (title != null)
                title.text = text;
        }

        /// <summary>Shows the fixed Menu button bound to <paramref name="action"/>, or hides it when null.</summary>
        public void SetMenuButton(Action action)
        {
            menuAction = action;
            UpdateMenuButton();
        }

        /// <summary>Lays the items out on the tray, with a "More" button when they do not fit.</summary>
        public void ShowButtons(IReadOnlyList<MenuChoice> menuItems)
        {
            items = menuItems;
            page = 0;
            BuildPage();
        }

        public void ClearButtons()
        {
            foreach (var button in buttons)
                if (button != null)
                    Destroy(button.gameObject);
            buttons.Clear();
            items = null;
            UpdateMenuButton();
        }

        void BuildPage()
        {
            foreach (var button in buttons)
                Destroy(button.gameObject);
            buttons.Clear();

            var capacity = columns * rows;
            var paged = items.Count > capacity;
            var perPage = paged ? capacity - 1 : capacity;
            var start = page * perPage;
            var count = Mathf.Min(perPage, items.Count - start);

            for (var i = 0; i < count; i++)
                Add(i, items[start + i].Label, items[start + i].Action);
            if (paged)
            {
                var lastPage = start + perPage >= items.Count;
                Add(count, lastPage ? "Back to first" : "More", () =>
                {
                    page = lastPage ? 0 : page + 1;
                    BuildPage();
                });
            }
            UpdateMenuButton();
        }

        void Add(int index, string label, Action action)
        {
            var column = index % columns;
            var row = index / columns;
            var button = Instantiate(buttonPrefab, buttonGrid);
            button.name = $"Menu: {label}";
            button.transform.localPosition = new Vector3(
                (column - (columns - 1) * 0.5f) * cellSize.x, 0f, -row * cellSize.y);
            button.Bind(label, action);
            buttons.Add(button);
        }

        void UpdateMenuButton()
        {
            if (menuButton == null)
                return;
            var visible = menuAction != null && !IsOpen;
            menuButton.gameObject.SetActive(visible);
            if (visible)
                menuButton.Bind("Menu", menuAction);
        }
    }
}
