using System.Drawing;

namespace SilkUI.Controls
{
    /// <summary>
    /// A panel with text.
    /// </summary>
    public class Label : Panel
    {
        public new static readonly int DefaultWidth = 100;
        public new static readonly int DefaultHeight = 40;

        private int? _textRef;
        private StringProperty _text = new StringProperty(nameof(Text), "");

        public string Text
        {
            get => _text.Value ?? "";
            set => _text.Value = value;
        }

        public Label(string id = null)
            : base(id)
        {
            Width = DefaultWidth;
            Height = DefaultHeight;

            _text.InternalValueChanged += Invalidate;
        }

        protected override void OnRender(RenderEventArgs args)
        {
            base.OnRender(args);

            // Draw text after the rest.
            if (!string.IsNullOrWhiteSpace(Text)) // TODO: text styles
                _textRef = args.Renderer.DrawText(this, _textRef, AbsoluteContentRectangle, Text, new Font()
                {
                    // TODO: Test what happens if not all is initialized here
                    Name = "arial.ttf",
                    Size = 18,
                    FallbackNames = new string[] { },
                    Style = FontStyle.None
                }, Color.Red, HorizontalAlignment.Right, VertictalAlignment.Bottom, false, TextOverflow.Clip);
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
