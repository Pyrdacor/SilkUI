using System;
using System.Collections.Generic;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    internal class TextureAtlas
    {
        public MutableTexture AtlasTexture { get; } = new MutableTexture(0, 0);
        private readonly Dictionary<ImageHandle, Point> _images = new Dictionary<ImageHandle, Point>();

        public Point AddTexture(ImageHandle imageHandle)
        {
            if (!_images.ContainsKey(imageHandle))
            {
                // TODO: place the image so the atlas size uses minimal space
                var position = new Point(AtlasTexture.Width, 0);
                AtlasTexture.Resize(AtlasTexture.Width + imageHandle.Width, Math.Max(AtlasTexture.Height, imageHandle.Height));
                AtlasTexture.AddSprite(position, imageHandle.Data, imageHandle.Width, imageHandle.Height, imageHandle.BytesPerPixel == 1);
                return position;
            }
            else
            {
                return _images[imageHandle];
            }
        }
    }
}
