using System;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    /// <summary>
    /// A colored ellipse (or round rect).
    /// </summary>
    internal class Ellipse : Polygon
    {
        public enum EllipseRoundness : uint
        {
            Full = 2,
            High = 4,
            Medium = 8,
            Low = 16
        }

        private uint _roundness = 2u;

        private Ellipse(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height)
            : base(controlRenderer, controlRenderer.OpaqueEllipseRenderLayer, renderDimensionReference, new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                })
        {

        }

        protected Ellipse(ControlRenderer controlRenderer, RenderLayer layer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height)
            : base(controlRenderer, layer, renderDimensionReference, new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                })
        {

        }

        public static Ellipse CreateEllipse(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height, EllipseRoundness roundness, uint? blurRadius = null)
        {
            if (roundness != EllipseRoundness.Full &&
                roundness != EllipseRoundness.High &&
                roundness != EllipseRoundness.Medium &&
                roundness != EllipseRoundness.Low)
            throw new ArgumentOutOfRangeException($"Invalid ellipse roundness value {roundness}.");

            var ellipse = new Ellipse(controlRenderer, renderDimensionReference, x, y, width, height);

            ellipse._roundness = (uint)roundness;
            ellipse._blurRadius = blurRadius;

            return ellipse;
        }

        public EllipseRoundness Roundness
        {
            get => (EllipseRoundness)_roundness;
            set
            {
                if (_roundness == (uint)value)
                    return;

                if (value != EllipseRoundness.Full &&
                    value != EllipseRoundness.High &&
                    value != EllipseRoundness.Medium &&
                    value != EllipseRoundness.Low)
                    throw new ArgumentOutOfRangeException($"Invalid ellipse roundness value {value}.");

                _roundness = (uint)value;

                UpdateRoundness();
            }
        }

        protected void UpdateRoundness()
        {
            if (_drawIndex.HasValue)
                Layer.UpdateRoundness(_drawIndex.Value, _roundness);
        }

        protected override void UpdateBlurRadius(bool blurChanged)
        {
            if (blurChanged)
            {
                bool wasAdded = _drawIndex != null;
                RemoveFromLayer();

                if (_blurRadius == null) // Not blurred anymore
                    Layer = Color.A > 0 && Color.A < 255 ? _controlRenderer.TransparentEllipseRenderLayer: _controlRenderer.OpaqueEllipseRenderLayer;
                else // Now blurred
                    Layer = _controlRenderer.BlurEllipseRenderLayer;

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
                    Layer = transparency ? _controlRenderer.TransparentEllipseRenderLayer : _controlRenderer.OpaqueEllipseRenderLayer;
                    if (wasAdded)
                        AddToLayer();
                    return;
                }
            }

            base.UpdateColor();
        }
    }
}
