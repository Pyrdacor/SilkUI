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
        private uint _displayLayer = 0;
        private readonly DrawCommandBatch _drawCommandBatch = new DrawCommandBatch();
        private int _replaceRenderObjectIndex = -1;
        private const int TabSize = 4;

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

            // TODO: we need a texture or texture atlas and cache it!
            Texture texture = null;
            Point textureOffset = Point.Empty;
            bool transparency = false;
            Rectangle? clipRect = null;

            return AddDrawCommands(new DrawCommandSprite(x, y, image.Width, image.Height, _displayLayer++,
                colorOverlay ?? Color.White, texture, textureOffset, transparency, clipRect));
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

            var drawCommands = new List<DrawCommand>(text.Length);
            y += font.Size; // We need origin Y (baseline)

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '\n')
                {
                    y += glyphs.LineHeight;
                    continue;
                }
                else if (text[i] == '\r')
                {
                    if (i == text.Length - 1 || text[i + 1] != '\n')
                        y += glyphs.LineHeight;
                    continue;
                }

                var glyph = DrawGlyph(ref x, y, text[i], color, glyphs, null);

                if (glyph != null)
                    drawCommands.Add(glyph);
            }

            if (drawCommands.Count == 0)
                return DenyDrawing(); // Nothing was drawn.

            ++_displayLayer;

            return AddDrawCommands(drawCommands.ToArray());
        }

        private struct TextLine
        {
            public int StartCommandIndex;
            public int CommandCount;
            public int Width;
            public int CurrentCommandIndex => StartCommandIndex + CommandCount;
        }

        // TODO: LTR/RTL support
        public int DrawText(Rectangle bounds, string text, Font font, Color color,
            HorizontalAlignment horizontalAlignment, VertictalAlignment vertictalAlignment,
            bool wordWrap, TextOverflow textOverflow)
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

            var drawCommands = new List<DrawCommand>(text.Length);
            var lines = new Queue<TextLine>();
            var currentTextLine = new TextLine()
            {
                StartCommandIndex = 0,
                CommandCount = 0,
                Width = 0
            };
            int x = bounds.X;
            int y = bounds.Y + font.Size;
            int lastWhiteSpacePosition = -1;
            int widthToLastWhiteSpace = 0;
            int lastWhiteSpaceWidth = 0;
            Rectangle? clipRect = textOverflow == TextOverflow.Allow ? (Rectangle?)null : bounds;

            void StartNextLine(TextLine? line = null)
            {
                AdjustLineX();
                x = bounds.X;
                y += glyphs.LineHeight;
                lastWhiteSpacePosition = -1;
                lines.Enqueue(currentTextLine);
                currentTextLine = line ?? new TextLine()
                {
                    StartCommandIndex = currentTextLine.CurrentCommandIndex,
                    CommandCount = 0,
                    Width = 0
                };
            }

            void SplitLine()
            {
                var nextLine = new TextLine()
                {
                    // Note: The whitespace is not added as a draw command so the
                    // lastWhiteSpacePosition points to the draw command after
                    // the whitespace character.
                    StartCommandIndex = lastWhiteSpacePosition,
                    CommandCount = currentTextLine.CommandCount - lastWhiteSpacePosition,
                    Width = currentTextLine.Width - widthToLastWhiteSpace - lastWhiteSpaceWidth
                };
                currentTextLine.Width = widthToLastWhiteSpace;
                currentTextLine.CommandCount = lastWhiteSpacePosition - currentTextLine.StartCommandIndex;
                StartNextLine(nextLine);
                x += nextLine.Width;

                // Reposition the glyphs in the line that has moved down.
                for (int i = 0; i < currentTextLine.CommandCount; ++i)
                    drawCommands[currentTextLine.StartCommandIndex + i].Offset(-widthToLastWhiteSpace - lastWhiteSpaceWidth, glyphs.LineHeight);
            }

            void AdjustLineX()
            {
                var lineDrawCommands = drawCommands.Skip(currentTextLine.StartCommandIndex).Take(currentTextLine.CommandCount);

                switch (horizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                    default:
                        break;
                    case HorizontalAlignment.Center:
                        {
                            int offsetX = (bounds.Width - currentTextLine.Width) / 2;
                            if (offsetX != 0)
                            {
                                foreach (var drawCommand in lineDrawCommands)
                                    drawCommand.Offset(offsetX, 0);
                            }
                            break;
                        }
                    case HorizontalAlignment.Right:
                        {
                            int offsetX = bounds.Width - currentTextLine.Width;
                            if (offsetX != 0)
                            {
                                foreach (var drawCommand in lineDrawCommands)
                                    drawCommand.Offset(offsetX, 0);
                            }
                            break;
                        }
                    case HorizontalAlignment.Justify:
                        if (currentTextLine.Width >= bounds.Width)
                        {
                            // If it doesn't fit into the bounds, we just center it.
                            int offsetX = (bounds.Width - currentTextLine.Width) / 2;
                            if (offsetX != 0)
                            {
                                foreach (var drawCommand in lineDrawCommands)
                                    drawCommand.Offset(offsetX, 0);
                            }
                        }
                        else
                        {
                            // TODO: we don't have the whitespace here anymore :(
                            throw new NotImplementedException("Justify is not implemented yet.");
                        }
                        break;
                }
            }

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '\n')
                {
                    StartNextLine();
                    continue;
                }
                else if (text[i] == '\r')
                {
                    if (i == text.Length - 1 || text[i + 1] != '\n')
                        StartNextLine();
                    continue;
                }

                int lastX = x;
                var glyph = DrawGlyph(ref x, y, text[i], color, glyphs, clipRect);
                int glyphWidth = x - lastX;
                bool whiteSpace = text[i] <= 32 || char.IsWhiteSpace(text[i]);


                if (whiteSpace)
                {
                    lastWhiteSpacePosition = currentTextLine.CurrentCommandIndex;
                    widthToLastWhiteSpace = currentTextLine.Width;
                    lastWhiteSpaceWidth = glyphWidth;
                }                

                int newLineWidth = currentTextLine.Width + glyphWidth;

                if (wordWrap && lastWhiteSpacePosition != -1 && newLineWidth > bounds.Width)
                {
                    // If this is a white space character we simply ignore it and break.
                    if (whiteSpace)
                    {
                        StartNextLine();
                        continue;
                    }
                    else
                    {
                        // Bring the current glyph to the right position.
                        glyph.Offset(-widthToLastWhiteSpace - lastWhiteSpaceWidth, glyphs.LineHeight);
                        SplitLine();
                        x += glyphWidth; // x has been reset in SplitLine!
                    }
                }

                if (glyph != null)
                {
                    drawCommands.Add(glyph);
                    ++currentTextLine.CommandCount;
                }

                currentTextLine.Width += glyphWidth;

                if (i == text.Length - 1)
                {
                    AdjustLineX();

                    lines.Enqueue(currentTextLine);

                    // Adjust y of all lines
                    int totalHeight = lines.Count * glyphs.LineHeight;

                    switch (vertictalAlignment)
                    {
                        case VertictalAlignment.Top:
                        default:
                            break;
                        case VertictalAlignment.Center:
                            {
                                int offsetY = (bounds.Height - totalHeight) / 2;
                                if (offsetY != 0)
                                {
                                    foreach (var drawCommand in drawCommands)
                                        drawCommand.Offset(0, offsetY);
                                }
                                break;
                            }
                        case VertictalAlignment.Bottom:
                            {
                                int offsetY = bounds.Height - totalHeight;
                                if (offsetY != 0)
                                {
                                    foreach (var drawCommand in drawCommands)
                                        drawCommand.Offset(0, offsetY);
                                }
                                break;
                            }
                    }
                }
            }

            if (drawCommands.Count == 0)
                return DenyDrawing(); // Nothing was drawn.

            ++_displayLayer;

            return AddDrawCommands(drawCommands.ToArray());
        }

        private DrawCommand DrawGlyph(ref int x, int y, char character, Color color, FontGlyphs glyphs, Rectangle? clipRect)
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
            {
                if (code == '\t' && glyphs.Glyphs.ContainsKey(' ')) // TODO: make tab size configurable or a constant
                    x += TabSize * glyphs.Glyphs[' '].Advance;

                return null;
            }

            var glyph = glyphs.Glyphs[code];

            if (code <= 32 || char.IsWhiteSpace(character)) // Don't draw control characters and spaces, but advance.
            {
                x += glyph.Advance;
                return null;
            }

            var glyphCommand = new DrawCommandSprite(x + glyph.BearingX, y - glyph.BearingY, glyph.Width, glyph.Height,
                _displayLayer, color, glyph.TextureAtlas.AtlasTexture, glyph.TextureAtlasOffset, true, clipRect);

            x += glyph.Advance;

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
