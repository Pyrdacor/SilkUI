using System.Collections.Generic;
using System.Linq;

namespace SilkUI
{
    public class FontGlyphs
    {
        public IReadOnlyDictionary<uint, FreeType.Glyph> Glyphs { get; }

        internal FontGlyphs(FreeType.Glyph[] glyphs)
        {
            Glyphs = glyphs.ToDictionary(g => g.CharCode, g => g);
        }
    }
}
