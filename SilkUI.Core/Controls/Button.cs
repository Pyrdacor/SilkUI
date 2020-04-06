using System.Drawing;
using Silk.NET.Input.Common;

namespace SilkUI.Controls
{
    /// <summary>
    /// A clickable button.
    /// </summary>
    public class Button : Panel
    {
        private int? _textRef;
        private BoolProperty _pressed = new BoolProperty(nameof(Pressed), false);
        private StringProperty _text = new StringProperty(nameof(Text), "");

        public bool Pressed
        {
            get => Enabled && Visible && _pressed.HasValue && _pressed.Value.Value;
            internal set => _pressed.Value = value && Enabled && Visible;
        }

        public string Text
        {
            get => _text.Value ?? "";
            set => _text.Value = value;
        }

        public Button(string id = null)
            : base(id)
        {
            // for now set some base dimensions
            Width = 100;
            Height = 60;

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
            _text.InternalValueChanged += Invalidate;
        }

        protected override void OnRender(RenderEventArgs args)
        {
            // A button is just a panel which sets some default styles
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

            // Draw text.
            if (!string.IsNullOrWhiteSpace(Text)) // TODO: text styles
                _textRef = args.Renderer.DrawText(this, _textRef, X, Y, Text, new Font()
                {
                    // TODO: Test what happens if not all is initialized here
                    Name = "arial.ttf",
                    Size = 18,
                    FallbackNames = new string[] { },
                    Options = FontOptions.None
                }, Color.Red);
        }

        internal override void DestroyView()
        {
            if (_textRef.HasValue)
            {
                ControlRenderer.RemoveRenderObject(_textRef.Value);
                _textRef = null;
            }

            base.DestroyView();
        }
    }
}