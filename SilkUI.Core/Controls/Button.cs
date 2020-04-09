using System.Drawing;
using Silk.NET.Input.Common;

namespace SilkUI.Controls
{
    /// <summary>
    /// A clickable button.
    /// </summary>
    public class Button : Label
    {
        public new static readonly int DefaultWidth = 100;
        public new static readonly int DefaultHeight = 60;

        private BoolProperty _pressed = new BoolProperty(nameof(Pressed), false);

        public bool Pressed
        {
            get => Enabled && Visible && _pressed.HasValue && _pressed.Value.Value;
            internal set => _pressed.Value = value && Enabled && Visible;
        }

        public Button(string id = null)
            : base(id)
        {
            Width = DefaultWidth;
            Height = DefaultHeight;

            MouseDown += (_, e) =>
            {
                if (e.Button == MouseButton.Left)
                    Pressed = true;
            };
            MouseUp += (_, e) =>
            {
                if (e.Button == MouseButton.Left)
                    Pressed = false;
            };
            MouseUpOutside += (_, e) =>
            {
                if (e.Button == MouseButton.Left)
                    Pressed = false;
            };
            _pressed.InternalValueChanged += Invalidate;
        }

        protected override void OnRender(RenderEventArgs args)
        {
            // A button is just a label which sets some default styles
            // when different states are active.
            ColorValue backgroundColor;
            AllDirectionStyleValue<ColorValue> borderColor;

            if (Enabled)
            {
                backgroundColor = Color.Gray;

                if (Hovered)
                    backgroundColor = backgroundColor.Lighten(0.25f);
            }
            else
            {
                backgroundColor = Color.DarkGray;
            }

            borderColor = backgroundColor;

            OverrideStyleIfUndefined("border.size", 4);
            OverrideStyleIfUndefined("border.color", borderColor);
            OverrideStyleIfUndefined("border.linestyle", Pressed ? BorderLineStyle.Inset : BorderLineStyle.Outset);
            OverrideStyleIfUndefined("background.color", backgroundColor);

            // Draw with set styles.
            base.OnRender(args);
        }
    }
}