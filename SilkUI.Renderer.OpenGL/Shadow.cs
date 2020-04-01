using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    internal class Shadow : Shape
    {
        private uint _blurRadius = 0u;
        private uint _roundness = 0u;

        /// <summary>
        /// Create a shadow render object.
        /// </summary>
        /// <param name="controlRenderer"></param>
        /// <param name="renderDimensionReference"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="blurRadius"></param>
        /// <param name="roundness">Odd numbers are not allowed. 0: Rect, 2: Ellipse, 4 - 16: Round rects</param>
        public Shadow(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height, uint blurRadius, uint roundness)
            : base(controlRenderer, renderDimensionReference,
                controlRenderer.ShadowRenderLayer, CreateMetrics(x, y, width, height, blurRadius))
        {
            _blurRadius = blurRadius;
            _roundness = roundness;
        }

        private static Point[] CreateMetrics(int x, int y, int width, int height, uint blurRadius)
        {
            int r = (int)blurRadius;
            return new Point[4]
            {
                new Point(x - r, y - r),
                new Point(x + width + r, y - r),
                new Point(x + width + r, y + height + r),
                new Point(x - r, y + height + r)
            };
        }

        protected void UpdateBlurRadius()
        {
            if (_drawIndex.HasValue)
                Layer.UpdateBlurRadius(_drawIndex.Value, _blurRadius);
        }

        protected void UpdateRoundness()
        {
            if (_drawIndex.HasValue)
                Layer.UpdateRoundness(_drawIndex.Value, _roundness);
        }

        public uint BlurRadius
        {
            get => _blurRadius;
            set
            {
                if (_blurRadius == value)
                    return;

                _blurRadius = value;

                UpdateBlurRadius();
            }
        }

        public uint Roundness
        {
            get => _roundness;
            set
            {
                if (_roundness == value)
                    return;

                _roundness = value;

                UpdateRoundness();
            }
        }
    }
}