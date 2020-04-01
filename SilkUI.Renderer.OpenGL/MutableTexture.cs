using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SilkUI.Renderer.OpenGL
{
    internal class MutableTexture : Texture
    {
        private int _width = 0;
        private int _height = 0;
        private byte[] _data = null;

        public MutableTexture(int width, int height)
            : base(width, height)
        {
            _width = width;
            _height = height;
            _data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }

        public override int Width => _width;
        public override int Height => _height;

        public void AddSprite(Point position, byte[] data, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                Buffer.BlockCopy(data, y * width * 4, _data, (position.X + (position.Y + y) * Width) * 4, width * 4);
            }
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
        {
            int index = y * Width + x;

            _data[index * 4 + 0] = r;
            _data[index * 4 + 1] = g;
            _data[index * 4 + 2] = b;
            _data[index * 4 + 3] = a;
        }

        public void SetPixels(byte[] pixelData)
        {
            if (pixelData == null)
                throw new ArgumentNullException("Pixel data was null.");

            if (pixelData.Length != _data.Length)
                throw new ArgumentOutOfRangeException("Pixel data size does not match texture data size.");

            Buffer.BlockCopy(pixelData, 0, _data, 0, pixelData.Length);
        }

        public void Finish(int numMipMapLevels)
        {
            Create(PixelFormat.BGRA8, _data, numMipMapLevels);

            _data = null;
        }

        public void Resize(int width, int height)
        {
            if (_data != null && _width == width && _height == height)
                return;

            _width = width;
            _height = height;
            _data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }
    }
}
