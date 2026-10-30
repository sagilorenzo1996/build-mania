using System;
using BuildMania.Interaction;
using TMPro;
using UnityEngine;

namespace BuildMania.App
{
    /// <summary>A labelled button lying on the tray, pressed by touch like the other tray buttons.</summary>
    public sealed class MenuButton : PokeButton
    {
        [SerializeField] TMP_Text label;

        Action action;

        public string Label => label != null ? label.text : string.Empty;

        public void Bind(string text, Action onPressed)
        {
            if (label != null)
                label.text = text;
            action = onPressed;
        }

        /// <summary>Presses the button from code, as a touch would.</summary>
        public void Press() => action?.Invoke();

        protected override void OnPressed() => Press();
    }
}
