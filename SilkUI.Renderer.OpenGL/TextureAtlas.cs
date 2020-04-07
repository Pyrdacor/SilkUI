using System;
using System.Collections.Generic;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    internal class TextureAtlas
    {
        public Texture AtlasTexture { get; }
        private readonly Dictionary<uint, Point> _atlasPositions = new Dictionary<uint, Point>();

        internal TextureAtlas(Texture texture, Dictionary<uint, Point> imagePositions)
        {
            AtlasTexture = texture;
            _atlasPositions = imagePositions;
        }

        public static TextureAtlas Create(params FreeType.Glyph[] glyphs)
        {
            var builder = new TextureAtlasBuilder();

            foreach (var glyph in glyphs)
                builder.AddImage(new ImageHandle(glyph));

            return builder.Create();
        }

        public static TextureAtlas Create(Dictionary<uint, Bitmap> images)
        {
            var builder = new TextureAtlasBuilder();

            foreach (var image in images)
                builder.AddImage(new ImageHandle(image.Key, image.Value));

            return builder.Create();
        }

        public Point GetAtlasPosition(uint key)
        {
            return _atlasPositions[key];
        }
    }
}
