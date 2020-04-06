using System;

namespace SilkUI
{
    [Flags]
    public enum FontOptions
    {
        None = 0x00,
        Bold = 0x01,
        Italic = 0x02,
        Underlined = 0x04,
        Overlined = 0x08,
        LineThrough = 0x10
    }

    public struct Font
    {
        public string Name;
        public int Size;
        public FontOptions Options;
        public string[] FallbackNames;
    }
}
