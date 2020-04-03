using System.Collections.Generic;
using System.Drawing;
using Silk.NET.OpenGL;
using Silk.NET.Windowing.Common;

namespace SilkUI.Renderer.OpenGL
{
    internal class ControlRenderer : IControlRenderer
    {
        internal RenderLayer SpriteRenderLayer { get; } // for now this is based on an atlas texture
        internal RenderLayer BlurRectRenderLayer { get; }
        internal RenderLayer BlurEllipseRenderLayer { get; }
        internal RenderLayer OpaqueEllipseRenderLayer { get; }
        internal RenderLayer TransparentEllipseRenderLayer { get; }
        private readonly Dictionary<int, RenderLayer> _opaquePolygonRenderLayers = new Dictionary<int, RenderLayer>();
        private readonly Dictionary<int, RenderLayer> _transparentPolygonRenderLayers = new Dictionary<int, RenderLayer>();
        private readonly Dictionary<int, IRenderNode> _renderNodes = new Dictionary<int, IRenderNode>();
        private readonly IndexPool _renderNodeIndexPool = new IndexPool();
        private readonly Context _context;
        private readonly RenderDimensionReference _renderDimensionReference;
        private readonly TextureAtlas _textureAtlas = new TextureAtlas();
        private uint _displayLayer = 0;

        public ControlRenderer(RenderDimensionReference renderDimensionReference)
        {
            _renderDimensionReference = renderDimensionReference;
            _context = new Context(renderDimensionReference);

            SpriteRenderLayer = new RenderLayer(_textureAtlas.AtlasTexture, PrimitiveRenderer.CreateSpriteRenderer(), renderDimensionReference); // TODO: colorkey? color overlay?
            BlurRectRenderLayer = new RenderLayer(null, PrimitiveRenderer.CreateBlurRectRenderer(), renderDimensionReference);
            BlurEllipseRenderLayer = new RenderLayer(null, PrimitiveRenderer.CreateBlurEllipseRenderer(), renderDimensionReference);
            OpaqueEllipseRenderLayer = new RenderLayer(null, PrimitiveRenderer.CreateEllipseRenderer(false), renderDimensionReference);
            TransparentEllipseRenderLayer = new RenderLayer(null, PrimitiveRenderer.CreateEllipseRenderer(true), renderDimensionReference);
        }

        private RenderLayer GetPolygonRenderLayer(Dictionary<int, RenderLayer> polygonRenderLayers, int numVertices, bool supportTransparency)
        {
            if (!polygonRenderLayers.ContainsKey(numVertices))
            {
                var renderLayer = new RenderLayer(null, PrimitiveRenderer.CreatePolygonRenderer(numVertices, supportTransparency), _renderDimensionReference);
                polygonRenderLayers.Add(numVertices, renderLayer);
                return renderLayer;
            }
            else
            {
                return polygonRenderLayers[numVertices];
            }
        }

        internal RenderLayer GetPolygonRenderLayer(int numVertices, bool supportTransparency)
        {
            if (supportTransparency)
                return GetPolygonRenderLayer(_transparentPolygonRenderLayers, numVertices, true);
            else
                return GetPolygonRenderLayer(_opaquePolygonRenderLayers, numVertices, false);         
        }

        public void StartRenderCycle()
        {
            _context.SetRotation(Rotation.None); // TODO: can be used later for different devices
            _displayLayer = 0;

            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
        }

        public void EndRenderCycle()
        {
            // 1. Draw all opaque objects.
            State.Gl.DepthMask(true);
            State.Gl.Disable(EnableCap.Blend);

            OpaqueEllipseRenderLayer.Render();

            foreach (var renderLayer in _opaquePolygonRenderLayers)
                renderLayer.Value.Render();

            // 2. Draw all objects with transparency.
            State.Gl.DepthMask(false);
            State.Gl.Enable(EnableCap.Blend);

            BlurRectRenderLayer.Render();
            BlurEllipseRenderLayer.Render();
            SpriteRenderLayer.Render();
            TransparentEllipseRenderLayer.Render();

            foreach (var renderLayer in _transparentPolygonRenderLayers)
                renderLayer.Value.Render();
        }

        public void RemoveRenderObject(int renderObjectIndex)
        {
            if (_renderNodes.ContainsKey(renderObjectIndex))
            {
                _renderNodes[renderObjectIndex].Delete();
                _renderNodes.Remove(renderObjectIndex);
                _renderNodeIndexPool.UnassignIndex(renderObjectIndex);
            }
        }

        public int DrawRectangle(int x, int y, int width, int height, Color color, int lineSize)
        {
            if (width == 0 || height == 0 || lineSize == 0)
                return -1;

            if (width <= 2 * lineSize || height <= 2 * lineSize)
            {
                // it's just a filled rect with the border color
                return FillRectangle(x, y, width, height, color);
            }

            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var topLine = Polygon.CreateRect(this, _renderDimensionReference,
                x, y, width, lineSize);
            var leftLine = Polygon.CreateRect(this, _renderDimensionReference,
                x, y + lineSize, lineSize, height - 2 * lineSize);
            var rightLine = Polygon.CreateRect(this, _renderDimensionReference,
                x + width - lineSize, y + lineSize, lineSize, height - 2 * lineSize);
            var bottomLine = Polygon.CreateRect(this, _renderDimensionReference,
                x, y + height - lineSize, width, lineSize);

            topLine.Color = color;
            topLine.DisplayLayer = _displayLayer;
            topLine.Visible = true;

            leftLine.Color = color;
            leftLine.DisplayLayer = _displayLayer;
            leftLine.Visible = true;

            rightLine.Color = color;
            rightLine.DisplayLayer = _displayLayer;
            rightLine.Visible = true;

            bottomLine.Color = color;
            bottomLine.DisplayLayer = _displayLayer;
            bottomLine.Visible = true;

            ++_displayLayer;

            var container = new RenderNodeContainer();

            container.AddChild(topLine);
            container.AddChild(leftLine);
            container.AddChild(rightLine);
            container.AddChild(bottomLine);

            _renderNodes.Add(renderObjectIndex, container);

            return renderObjectIndex;
        }

        public int FillRectangle(int x, int y, int width, int height, Color color)
        {
            if (width == 0 || height == 0)
                return -1;

            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var rectShape = Polygon.CreateRect(this, _renderDimensionReference,
                x, y, width, height);

            rectShape.Color = color;
            rectShape.DisplayLayer = _displayLayer++;
            rectShape.Visible = true;

            _renderNodes.Add(renderObjectIndex, rectShape);

            return renderObjectIndex;
        }

        public int DrawRectangleLine(int x, int y, int width, int height, Color color, LineStyle lineStyle)
        {
            switch (lineStyle)
            {
                case LineStyle.Solid:
                    return FillRectangle(x, y, width, height, color);
                case LineStyle.Dotted:
                    // TODO
                    return -1;
                case LineStyle.Dashed:
                    // TODO
                    return -1;
                default:
                    return -1;
            }
        }

        public int DrawImage(int x, int y, Image image, Color? colorOverlay = null)
        {
            if (image.Width == 0 || image.Height == 0)
                return -1;

            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var textureAtlasOffset = _textureAtlas.AddTexture(image);
            var sprite = Sprite.Create(this, _renderDimensionReference,
                x, y, image.Width, image.Height, textureAtlasOffset.X, textureAtlasOffset.Y);

            sprite.Color = colorOverlay ?? Color.White;
            sprite.DisplayLayer = _displayLayer++;
            sprite.Visible = true;

            _renderNodes.Add(renderObjectIndex, sprite);

            return renderObjectIndex;
        }

        public int FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            var p1 = new Point(x1, y1);
            var p2 = new Point(x2, y2);
            var p3 = new Point(x3, y3);

            if (p1 == p2 || p1 == p3 || p2 == p3)
                return -1;

            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var shape = Polygon.CreateTriangle(this, _renderDimensionReference, p1, p2, p3);

            shape.Color = color;
            shape.DisplayLayer = _displayLayer++;
            shape.Visible = true;

            _renderNodes.Add(renderObjectIndex, shape);

            return renderObjectIndex;
        }

        public int FillPolygon(Color color, params Point[] points)
        {
            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var shape = Polygon.CreatePolygon(this, _renderDimensionReference, points);

            shape.Color = color;
            shape.DisplayLayer = _displayLayer++;
            shape.Visible = true;

            _renderNodes.Add(renderObjectIndex, shape);

            return renderObjectIndex;
        }

        public int DrawShadow(int x, int y, int width, int height, Color color, int blurRadius, bool inset)
        {
            if (inset)
            {
                // TODO
                return -1;
            }
            else
            {
                if (blurRadius <= 0)
                {
                    return FillRectangle(x, y, width, height, color);
                }
                else
                {
                    int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
                    var shadow = Polygon.CreateRect(this, _renderDimensionReference, x, y, width, height, blurRadius == 0 ? (uint?)null : (uint)blurRadius);

                    shadow.Color = color;
                    shadow.DisplayLayer = _displayLayer++;
                    shadow.Visible = true;

                    _renderNodes.Add(renderObjectIndex, shadow);

                    return renderObjectIndex;
                }
            }
        }

        public int DrawText(int x, int y, string text, Font font, Color color)
        {
            // TODO
            return -1;
        }
    }

    public class ControlRendererFactory : IControlRendererFactory
    {
        public IControlRenderer CreateControlRenderer(IView view)
        {
            var dimensions = new SilkUI.Renderer.OpenGL.RenderDimensionReference();
            dimensions.SetDimensions(view.Size.Width, view.Size.Height);
            return  new SilkUI.Renderer.OpenGL.ControlRenderer(dimensions);
        }
    }
}
