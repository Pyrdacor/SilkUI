using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SilkUI
{
    public class FontManager
    {
        private static readonly string FontPath = @"fonts";
        private static FontManager _instance;
        private readonly FreeType.FreeType ft = new FreeType.FreeType();
        private readonly Dictionary<string, Dictionary<int, Dictionary<FontOptions, FontGlyphs>>> cachedFonts =
            new Dictionary<string, Dictionary<int, Dictionary<FontOptions, FontGlyphs>>>();

        private FontManager()
        {

        }

        public static FontManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FontManager();

                return _instance;
            }
        }

        private static Font? NextFont(Font font)
        {
            if (font.FallbackNames == null || font.FallbackNames.Length == 0)
                return null;

            return new Font()
            {
                Name = font.FallbackNames[0],
                Size = font.Size,
                Options = font.Options,
                FallbackNames = font.FallbackNames.Skip(1).ToArray()
            };
        }

        /// <summary>
        /// Gets glyphs for the given font if is available.
        /// </summary>
        /// <param name="font">Font to retrieve glyphs for</param>
        public FontGlyphs GetFont(Font font)
        {
            return GetFont(FontPath, font);
        }

        /// <summary>
        /// Gets glyphs for the given font if is available.
        /// </summary>
        /// <param name="fontPath">Path to the font directory</param>
        /// <param name="font">Font to retrieve glyphs for</param>
        public FontGlyphs GetFont(string fontPath, Font font)
        {
            var fontOptions = font.Options & (FontOptions.Bold | FontOptions.Italic); // only those are stored in the font itself

            if (cachedFonts.ContainsKey(font.Name))
            {
                if (cachedFonts[font.Name].ContainsKey(font.Size))
                {
                    if (!cachedFonts[font.Name][font.Size].ContainsKey(fontOptions))
                    {
                        // If the style is not present, it is not available at all.
                        // Try next fallback font or return null.
                        var nextFont = NextFont(font);
                        return nextFont != null ? GetFont(fontPath, nextFont.Value) : null;
                    }
                    else
                    {
                        return cachedFonts[font.Name][font.Size][fontOptions];
                    }
                }
                else
                {
                    FreeType.Font fontData;

                    try
                    {
                        fontData = ft.LoadFont(Path.Combine(fontPath, font.Name), font.Size);
                    }
                    catch
                    {
                        // In error case first try fallback fonts.
                        var nextFont = NextFont(font);

                        if (nextFont == null)
                            throw;

                        return GetFont(fontPath, nextFont.Value);
                    }

                    var glyphs = new Dictionary<FontOptions, FontGlyphs>();

                    foreach (var face in fontData.Faces)
                    {
                        var fontGlyphs = new FontGlyphs(face.Glyphs);
                        var options = FontOptions.None;

                        if (face.Bold)
                            options |= FontOptions.Bold;
                        if (face.Italic)
                            options |= FontOptions.Italic;

                        glyphs.Add(options, fontGlyphs);
                    }

                    cachedFonts[font.Name].Add(font.Size, glyphs);

                    if (glyphs.ContainsKey(fontOptions))
                        return glyphs[fontOptions];
                    else
                    {
                        var nextFont = NextFont(font);
                        return nextFont != null ? GetFont(fontPath, nextFont.Value) : null;
                    }
                }
            }
            else
            {
                cachedFonts.Add(font.Name, new Dictionary<int, Dictionary<FontOptions, FontGlyphs>>());
                return GetFont(font);
            }
        }
    }
}
