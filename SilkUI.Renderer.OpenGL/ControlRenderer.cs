using System.Collections.Generic;
using System.Drawing;
using Silk.NET.OpenGL;
using Silk.NET.Windowing.Common;

namespace SilkUI.Renderer.OpenGL
{
    internal class ControlRenderer : IControlRenderer
    {
        internal RenderLayer SpriteRenderLayer { get; }
        internal RenderLayer ShadowRenderLayer { get; }
        private readonly Dictionary<int, RenderLayer> _shapeRenderLayers = new Dictionary<int, RenderLayer>();
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

            SpriteRenderLayer = new RenderLayer(_textureAtlas.AtlasTexture, 4, false, renderDimensionReference);
            ShadowRenderLayer = new RenderLayer(null, 4, true, renderDimensionReference);
        }

        internal RenderLayer GetRenderLayer(int numVertices)
        {
            if (!_shapeRenderLayers.ContainsKey(numVertices))
            {
                var renderLayer = new RenderLayer(null, numVertices, false, _renderDimensionReference);
                _shapeRenderLayers.Add(numVertices, renderLayer);
                return renderLayer;
            }
            else
            {
                return _shapeRenderLayers[numVertices];
            }
        }

        public void StartRenderCycle()
        {
            _context.SetRotation(Rotation.None); // TODO: can be used later for different devices

            _displayLayer = 0;
            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
        }

        public void EndRenderCycle()
        {
            ShadowRenderLayer.Render();
            SpriteRenderLayer.Render();            

            foreach (var renderLayer in _shapeRenderLayers)
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
            var topLine = Shape.CreateRect(this, _renderDimensionReference,
                x, y, width, lineSize);
            var leftLine = Shape.CreateRect(this, _renderDimensionReference,
                x, y + lineSize, lineSize, height - 2 * lineSize);
            var rightLine = Shape.CreateRect(this, _renderDimensionReference,
                x + width - lineSize, y + lineSize, lineSize, height - 2 * lineSize);
            var bottomLine = Shape.CreateRect(this, _renderDimensionReference,
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
            var rectShape = Shape.CreateRect(this, _renderDimensionReference,
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
            var shape = Shape.CreateTriangle(this, _renderDimensionReference, p1, p2, p3);

            shape.Color = color;
            shape.DisplayLayer = _displayLayer++;
            shape.Visible = true;

            _renderNodes.Add(renderObjectIndex, shape);

            return renderObjectIndex;
        }

        public int FillPolygon(Color color, params Point[] points)
        {
            int renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex(out _);
            var shape = Shape.CreatePolygon(this, _renderDimensionReference, points);

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
                    var shadow = new Shadow(this, _renderDimensionReference, x, y, width, height, (uint)blurRadius, 0u);

                    shadow.Color = color;
                    shadow.DisplayLayer = _displayLayer++;
                    shadow.Visible = true;

                    _renderNodes.Add(renderObjectIndex, shadow);

                    return renderObjectIndex;
                }
            }
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
