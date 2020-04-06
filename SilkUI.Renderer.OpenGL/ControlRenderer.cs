using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Silk.NET.OpenGL;
using Silk.NET.Windowing.Common;

namespace SilkUI.Renderer.OpenGL
{
    internal class ControlRenderer : IControlRenderer
    {
        private readonly Dictionary<int, IEnumerable<DrawCommand>> _drawCommands = new Dictionary<int, IEnumerable<DrawCommand>>();
        private readonly List<int> _removedRenderObjects = new List<int>();
        private readonly IndexPool _renderNodeIndexPool = new IndexPool();
        private readonly Context _context;
        private readonly RenderDimensionReference _renderDimensionReference;
        private readonly TextureAtlas _textureAtlas = new TextureAtlas();
        private readonly TextureAtlas _fontTextureAtlas = new TextureAtlas();
        private uint _displayLayer = 0;
        private readonly DrawCommandBatch _drawCommandBatch = new DrawCommandBatch();
        private int _replaceRenderObjectIndex = -1;

        public ControlRenderer(RenderDimensionReference renderDimensionReference)
        {
            _renderDimensionReference = renderDimensionReference;
            _context = new Context(renderDimensionReference);
        }

        public void StartRenderCycle()
        {
            _context.SetRotation(Rotation.None); // TODO: can be used later for different devices
            _displayLayer = 0;

            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
        }

        public void EndRenderCycle()
        {
            _drawCommandBatch.UpdateBatch(_drawCommands.SelectMany(c => c.Value));

            // Remove deleted render objects.
            foreach (var renderObject in _removedRenderObjects)
            {
                _drawCommands.Remove(renderObject);
                _renderNodeIndexPool.UnassignIndex(renderObject);
            }

            _drawCommandBatch.Render(_renderDimensionReference);
        }

        public void RemoveRenderObject(int renderObjectIndex)
        {
            if (_drawCommands.ContainsKey(renderObjectIndex))
            {
                foreach (var drawCommand in _drawCommands[renderObjectIndex])
                    drawCommand.State = DrawCommandState.Removed;
            }

            _removedRenderObjects.Add(renderObjectIndex);
        }

        public void ReplaceRenderObjectWithFollowingDrawCall(int renderObjectIndex)
        {
            _replaceRenderObjectIndex = renderObjectIndex;
        }

        private static bool CheckDrawCommandReplacement(DrawCommand prevDrawCommand, DrawCommand drawCommand)
        {
            return prevDrawCommand.Texture == drawCommand.Texture &&
                prevDrawCommand.Shader == drawCommand.Shader &&
                prevDrawCommand.VertexPositions.Length == drawCommand.VertexPositions.Length;
        }

        private static bool CheckDrawCommandReplacement(DrawCommand[] prevDrawCommands, DrawCommand[] drawCommands)
        {
            if (prevDrawCommands.Length != drawCommands.Length)
                return false;

            for (int i = 0; i < drawCommands.Length; ++i)
            {
                if (!CheckDrawCommandReplacement(prevDrawCommands[i], drawCommands[i]))
                    return false;
            }

            return true;
        }

        private int AddDrawCommands(params DrawCommand[] drawCommands)
        {
            int renderObjectIndex = GetRenderObjectIndex();

            if (_drawCommands.ContainsKey(renderObjectIndex)) // Replacement
            {
                // Check if we can replace.
                var prevDrawCommands = _drawCommands[renderObjectIndex].ToArray();

                if (CheckDrawCommandReplacement(prevDrawCommands, drawCommands))
                {
                    // Replace it.
                    for (int i = 0; i < drawCommands.Length; ++i)
                    {
                        drawCommands[i].State = DrawCommandState.Replaced;
                        drawCommands[i].BufferIndex = prevDrawCommands[i].BufferIndex;
                    }

                    _drawCommands[renderObjectIndex] = drawCommands;
                }
                else
                {
                    // Remove the old one and add this as new.
                    RemoveRenderObject(renderObjectIndex);
                    // Use a new render object index.
                    renderObjectIndex = _renderNodeIndexPool.AssignNextFreeIndex();
                    _drawCommands.Add(renderObjectIndex, drawCommands);
                }
            }
            else
            {
                _drawCommands.Add(renderObjectIndex, drawCommands);
            }

            return renderObjectIndex;
        }

        private int GetRenderObjectIndex()
        {
            if (_replaceRenderObjectIndex != -1)
            {
                int index = _replaceRenderObjectIndex;
                _replaceRenderObjectIndex = -1;
                return index;
            }

            return _renderNodeIndexPool.AssignNextFreeIndex();
        }

        private int DenyDrawing()
        {
            // If we won't draw but there was a request for replacement
            // we have to remove the draw calls that should have been replaced.
            if (_replaceRenderObjectIndex != -1)
            {
                RemoveRenderObject(_replaceRenderObjectIndex);
                _replaceRenderObjectIndex = -1;
            }

            return -1;
        }

        public int DrawRectangle(int x, int y, int width, int height, Color color, int lineSize)
        {
            if (width == 0 || height == 0 || lineSize == 0 || color.A == 0)
                return DenyDrawing();

            if (width <= 2 * lineSize || height <= 2 * lineSize)
            {
                // it's just a filled rect with the border color
                return FillRectangle(x, y, width, height, color);
            }

            int renderObjectIndex = GetRenderObjectIndex();
            var topLine = new DrawCommandRect(x, y, width, lineSize, _displayLayer, color);
            var leftLine = new DrawCommandRect(x, y + lineSize, lineSize, height - 2 * lineSize, _displayLayer, color);
            var rightLine = new DrawCommandRect(x + width - lineSize, y + lineSize, lineSize, height - 2 * lineSize, _displayLayer, color);
            var bottomLine = new DrawCommandRect(x, y + height - lineSize, width, lineSize, _displayLayer, color);

            ++_displayLayer;

            return AddDrawCommands(topLine, leftLine, rightLine, bottomLine);
        }

        public int FillRectangle(int x, int y, int width, int height, Color color)
        {
            if (width == 0 || height == 0 || color.A == 0)
                return DenyDrawing();

            return AddDrawCommands(new DrawCommandRect(x, y, width, height, _displayLayer++, color));
        }

        public int DrawRectangleLine(int x, int y, int width, int height, Color color, LineStyle lineStyle)
        {
            switch (lineStyle)
            {
                case LineStyle.Solid:
                    return FillRectangle(x, y, width, height, color);
                case LineStyle.Dotted:
                    // TODO
                    return DenyDrawing();
                case LineStyle.Dashed:
                    // TODO
                    return DenyDrawing();
                default:
                    return DenyDrawing();
            }
        }

        public int DrawImage(int x, int y, Bitmap image, Color? colorOverlay = null)
        {
            if (image.Width == 0 || image.Height == 0 || (colorOverlay.HasValue && colorOverlay.Value.A == 0))
                return DenyDrawing();

            var textureAtlasOffset = _textureAtlas.AddTexture(new ImageHandle(image));

            return AddDrawCommands(new DrawCommandSprite(x, y, image.Width, image.Height, _displayLayer++,
                colorOverlay ?? Color.White, _textureAtlas.AtlasTexture, textureAtlasOffset));
        }

        public int FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            if (color.A == 0)
                return DenyDrawing();

            var p1 = new Point(x1, y1);
            var p2 = new Point(x2, y2);
            var p3 = new Point(x3, y3);

            if (p1 == p2 || p1 == p3 || p2 == p3)
                return DenyDrawing();

            return AddDrawCommands(new DrawCommandPolygon(new Point[3] { p1, p2, p3 }, _displayLayer++, color));
        }

        public int FillPolygon(Color color, params Point[] points)
        {
            if (points.Length < 3 || color.A == 0)
                return DenyDrawing();

            return AddDrawCommands(new DrawCommandPolygon(points, _displayLayer++, color));
        }

        public int DrawShadow(int x, int y, int width, int height, Color color, int blurRadius, bool inset)
        {
            if (color.A == 0)
                return DenyDrawing();

            if (inset)
            {
                // TODO
                return DenyDrawing();
            }
            else
            {
                if (blurRadius <= 0)
                {
                    return FillRectangle(x, y, width, height, color);
                }
                else
                {
                    return AddDrawCommands(new DrawCommandBlurredRect(x, y, width, height, _displayLayer++, color, (uint)blurRadius));
                }
            }
        }

        public int DrawText(int x, int y, string text, Font font, Color color)
        {
            if (color.A == 0 || font.Size == 0 || string.IsNullOrWhiteSpace(text))
                return DenyDrawing();

            FontGlyphs glyphs;

            try
            {
                glyphs = FontManager.Instance.GetFont(font);
            }
            catch (Exception ex)
            {
                var exception = new KeyNotFoundException("The given font is not available.", ex);
                exception.Data.Add("Font", font);
                throw exception;
            }

            List<DrawCommand> drawCommands = new List<DrawCommand>(text.Length);

            foreach (char ch in text)
            {
                var glyph = DrawGlyph(ref x, y, ch, color, glyphs);

                if (glyph != null)
                    drawCommands.Add(glyph);
            }

            if (drawCommands.Count == 0)
                return DenyDrawing(); // Nothing was drawn.

            ++_displayLayer;
            _fontTextureAtlas.AtlasTexture.Finish(0); // TODO: this has to change

            return AddDrawCommands(drawCommands.ToArray());
        }

        private DrawCommand DrawGlyph(ref int x, int y, char character, Color color, FontGlyphs glyphs)
        {
            var bytes = Encoding.Unicode.GetBytes(new char[] { character });
            uint code = bytes.Length switch
            {
                1 => bytes[0],
                2 => BitConverter.ToUInt16(bytes),
                3 => BitConverter.ToUInt32(new byte[4] { 0, bytes[0], bytes[1], bytes[2] }), // TODO: is order correct?
                4 => BitConverter.ToUInt32(bytes),
                _ => throw new ArgumentOutOfRangeException("Invalid character code.")
            };

            if (!glyphs.Glyphs.ContainsKey(code))
                return null;

            var glyph = glyphs.Glyphs[code];

            if (code <= 32) // Don't draw control characters and spaces but advance.
            {
                if (code == '\t' && glyphs.Glyphs.ContainsKey(' ')) // TODO: make tab size configurable or a constant
                    x += 4 * glyphs.Glyphs[' '].Advance;
                else
                    x += glyph.Advance;

                return null;
            }
            
            var textureAtlasOffset = _fontTextureAtlas.AddTexture(new ImageHandle(glyph));
            var glyphCommand = new DrawCommandSprite(x + glyph.BearingX, y + glyph.BearingY, glyph.Width, glyph.Height,
                _displayLayer, color, _fontTextureAtlas.AtlasTexture, textureAtlasOffset);

            x += glyphs.Glyphs[code].Advance;

            return glyphCommand;
        }
    }

    public class ControlRendererFactory : IControlRendererFactory
    {
        public IControlRenderer CreateControlRenderer(IView view)
        {
            var dimensions = new RenderDimensionReference();
            dimensions.SetDimensions(view.Size.Width, view.Size.Height);
            return  new ControlRenderer(dimensions);
        }
    }
}
