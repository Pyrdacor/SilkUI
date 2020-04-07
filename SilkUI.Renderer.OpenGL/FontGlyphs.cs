using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SilkUI.Renderer.OpenGL;

namespace SilkUI
{
    internal struct FontGlyph
    {
        public TextureAtlas TextureAtlas;
        public Point TextureAtlasOffset;
        public int Width;
        public int Height;
        public int BearingX;
        public int BearingY;
        public int Advance;
    }

    internal class FontGlyphs
    {
        public IReadOnlyDictionary<uint, FontGlyph> Glyphs { get; }

        internal FontGlyphs(params FreeType.Glyph[] glyphs)
        {
            var fontAtlas = TextureAtlas.Create(glyphs);
            Glyphs = glyphs.ToDictionary
            (
                g => g.CharCode,
                g => new FontGlyph()
                {
                    TextureAtlas = fontAtlas,
                    TextureAtlasOffset = fontAtlas.GetAtlasPosition(g.CharCode),
                    Width = g.Width,
                    Height = g.Height,
                    BearingX = g.BearingX,
                    BearingY = g.BearingY,
                    Advance = g.Advance
                }
            );
        }
    }
}
