using System;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    /// <summary>
    /// A colored polygon.
    /// </summary>
    internal class Polygon : RenderNode
    {
        protected uint? _blurRadius = null;
        protected ControlRenderer _controlRenderer;

        protected Polygon(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            params Point[] vertexPositions)
            : base(renderDimensionReference, vertexPositions)
        {
            _controlRenderer = controlRenderer;
            // Default color has no transparency so start with the opaque layer
            Layer = controlRenderer.GetPolygonRenderLayer(vertexPositions.Length, false);
        }

        protected Polygon(ControlRenderer controlRenderer, RenderLayer layer,
            RenderDimensionReference renderDimensionReference,
            params Point[] vertexPositions)
            : base(renderDimensionReference, vertexPositions)
        {
            _controlRenderer = controlRenderer;
            Layer = layer;
        }

        public static Polygon CreateTriangle(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            Point p1, Point p2, Point p3)
        {
            return new Polygon(controlRenderer, renderDimensionReference, p1, p2, p3);
        }

        public static Polygon CreateRect(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height, uint? blurRadius = null)
        {
            var layer = blurRadius == null ? controlRenderer.GetPolygonRenderLayer(4, false) : controlRenderer.BlurRectRenderLayer;
            var rect = new Polygon(controlRenderer, layer, renderDimensionReference,
                new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }
            );

            rect._blurRadius = blurRadius;

            return rect;
        }

        public static Polygon CreatePolygon(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference, params Point[] points)
        {
            if (points.Length < 3)
                throw new ArgumentException("A polygon must consist of at least 3 points.");

            if (points.Length == 3)
            {
                return CreateTriangle(controlRenderer, renderDimensionReference,
                    points[0], points[1], points[2]);
            }

            return new Polygon(controlRenderer, renderDimensionReference, points);
        }

        public uint? BlurRadius
        {
            get => _blurRadius;
            set
            {
                // TODO: This should never be set for non-rects.

                if (_blurRadius == value)
                    return;

                // Changed between blur and not blur?
                bool blurChanged = (_blurRadius == null) != (value == null);

                _blurRadius = value;

                UpdateBlurRadius(blurChanged);
            }
        }

        protected virtual void UpdateBlurRadius(bool blurChanged)
        {
            if (blurChanged)
            {
                bool wasAdded = _drawIndex != null;
                RemoveFromLayer();

                if (_blurRadius == null) // Not blurred anymore
                    Layer = _controlRenderer.GetPolygonRenderLayer(VertexPositions.Length, Color.A > 0 && Color.A < 255);
                else // Now blurred
                    Layer = _controlRenderer.BlurRectRenderLayer;

                if (wasAdded)
                    AddToLayer();

                return;
            }

            if (_drawIndex.HasValue)
                Layer.UpdateBlurRadius(_drawIndex.Value, _blurRadius ?? 0u);
        }

        protected override void UpdateColor()
        {
            if (_blurRadius == null)
            {
                // If opaque changes to transparent or vice versa we have to change layers.
                bool transparency = Color.A > 0 && Color.A < 255;

                if (Layer.SupportTransparency != transparency)
                {
                    bool wasAdded = _drawIndex != null;
                    RemoveFromLayer();
                    Layer = _controlRenderer.GetPolygonRenderLayer(VertexPositions.Length, transparency);
                    if (wasAdded)
                        AddToLayer();
                    return;
                }
            }

            base.UpdateColor();
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            for (int i = 0; i < VertexPositions.Length; ++i)
            {
                int newX = VertexPositions[i].X;
                int newY = VertexPositions[i].Y;

                if (VertexPositions[i].X != X)
                {
                    float ratio = (float)VertexPositions[i].X / Width;
                    newX = X + Util.Round(ratio * width);
                }
                if (VertexPositions[i].Y != Y)
                {
                    float ratio = (float)VertexPositions[i].Y / Height;
                    newY = Y + Util.Round(ratio * height);
                }

                VertexPositions[i] = new Point(newX, newY);
            }

            base.Resize(width, height);

            UpdatePosition();
        }
    }
}
