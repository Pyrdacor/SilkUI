namespace SilkUI.Controls
{
    // The panel is the base class for all
    // basic non-component controls like
    // buttons, inputs, labels and so on.

    /// <summary>
    /// Most basic control which is only
    /// a clickable surface with a background.
    /// </summary>
    public class Panel : Control
    {
        private int? _shadowRef;
        private int? _backgroundRef;
        private int?[] _borderRefs = new int?[4];

        public Panel(string id = null)
            : base(id)
        {
            // for now set some base dimensions
            Width = 100;
            Height = 100;
        }

        internal override void DestroyView()
        {
            if (_shadowRef.HasValue)
            {
                ControlRenderer.RemoveRenderObject(_shadowRef.Value);
                _shadowRef = null;
            }

            if (_backgroundRef.HasValue)
            {
                ControlRenderer.RemoveRenderObject(_backgroundRef.Value);
                _backgroundRef = null;
            }

            for (int i = 0; i < _borderRefs.Length; ++i)
            {
                if (_borderRefs[i].HasValue)
                {
                    ControlRenderer.RemoveRenderObject(_borderRefs[i].Value);
                    _borderRefs[i] = null;
                }
            }
        }

        protected override void OnRender(RenderEventArgs args)
        {
            var renderer = args.Renderer;
            var rectangle = ClientRectangle;
            var borderSize = Style.Get<AllDirectionStyleValue<int>>("border.size");
            var borderColor = Style.Get<AllDirectionStyleValue<ColorValue>>("border.color");
            var borderStyle = Style.Get<AllDirectionStyleValue<BorderLineStyle>>("border.linestyle", BorderLineStyle.None);
            var backgroundColor = Style.Get<ColorValue>("background.color", "gray");
            var shadowVisible = Style.Get<bool>("shadow.visible", false);

            if (shadowVisible)
            {
                var shadowColor = Style.Get<ColorValue>("shadow.color");
                var shadowOffsetX = Style.Get<int>("shadow.xoffset");
                var shadowOffsetY = Style.Get<int>("shadow.yoffset");
                var shadowBlurRadius = Style.Get<int>("shadow.blurradius");
                var shadowSpreadRadius = Style.Get<int>("shadow.spreadradius");
                var shadowInset = Style.Get<bool>("shadow.inset", false);

                ControlPainter.DrawShadow
                (
                    this, ref _shadowRef, renderer, rectangle, shadowOffsetX, shadowOffsetY,
                    shadowColor, shadowBlurRadius, shadowSpreadRadius, shadowInset
                );
            }

            _backgroundRef = args.Renderer.FillRectangle(this, _backgroundRef, X, Y, Width, Height, backgroundColor);

            ControlPainter.DrawBorder(this, ref _borderRefs[0], renderer, StlyeDirection.Top, borderStyle.Top,
                borderColor.Top, borderSize.Top, rectangle);
            ControlPainter.DrawBorder(this, ref _borderRefs[1], renderer, StlyeDirection.Right, borderStyle.Right,
                borderColor.Right, borderSize.Right, rectangle);
            ControlPainter.DrawBorder(this, ref _borderRefs[2], renderer, StlyeDirection.Bottom, borderStyle.Bottom,
                borderColor.Bottom, borderSize.Bottom, rectangle);
            ControlPainter.DrawBorder(this, ref _borderRefs[3], renderer, StlyeDirection.Left, borderStyle.Left,
                borderColor.Left, borderSize.Left, rectangle);            

            // render child controls
            base.OnRender(args);
        }

        internal override void CheckStyleChanges()
        {
            Parent?.CheckStyleChanges();
        }
    }
}