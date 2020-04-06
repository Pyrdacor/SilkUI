using System;
using System.IO;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class Texture : IDisposable
    {
        public enum PixelFormat
        {
            RGBA8 = 0,
            BGRA8,
            RGB8,
            BGR8,
            Alpha,
            RGB5A1,
            R5G6B5,
            BGR5A1,
            B5G6R5
        }

        protected static readonly uint[] BytesPerPixel = new uint[9]
        {
            4,
            4,
            3,
            3,
            1,
            2,
            2,
            2,
            2
        };

        public static Texture ActiveTexture { get; private set; } = null;

        private bool _disposed = false;

        public virtual uint Index { get; private set; } = 0u;
        public virtual int Width { get; } = 0;
        public virtual int Height { get; } = 0;

        protected Texture(int width, int height)
        {
            Index = State.Gl.GenTexture();
            Width = width;
            Height = height;
        }

        public Texture(int width, int height, PixelFormat format, Stream pixelDataStream, int numMipMapLevels = 0)
        {
            int size = width * height * (int)BytesPerPixel[(int)format];

            if ((pixelDataStream.Length - pixelDataStream.Position) < size)
                throw new IOException("Pixel data stream does not contain enough data.");

            if (!pixelDataStream.CanRead)
                throw new IOException("Pixel data stream does not support reading.");

            byte[] pixelData = new byte[size];

            pixelDataStream.Read(pixelData, 0, size);

            Index = State.Gl.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);
        }

        public Texture(int width, int height, PixelFormat format, byte[] pixelData, int numMipMapLevels = 0)
        {
            if (width * height * BytesPerPixel[(int)format] != pixelData.Length)
                throw new ArgumentOutOfRangeException("Invalid texture data size.");

            Index = State.Gl.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);
        }

        private static GLEnum ToOpenGLPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGBA8:
                    return GLEnum.Rgba;
                case PixelFormat.BGRA8:
                    return GLEnum.Bgra;
                case PixelFormat.RGB8:
                    return GLEnum.Rgb;
                case PixelFormat.BGR8:
                    return GLEnum.Bgr;
                case PixelFormat.Alpha:
                    // Note: for the supported image format GL_RED means one channel data, GL_ALPHA is only used for texture storage on the gpu, so we don't use it
                    // We always use RGBA8 as texture storage on the gpu
                    return GLEnum.Red;
                default:
                    throw new FormatException("Invalid pixel format.");
            }
        }

        protected void Create(PixelFormat format, byte[] pixelData, int numMipMapLevels)
        {
            if (format >= PixelFormat.RGB5A1)
            {
                pixelData = ConvertPixelData(pixelData, ref format);
            }

            Bind();

            var minMode = (numMipMapLevels > 0) ? TextureMinFilter.NearestMipmapNearest : TextureMinFilter.Nearest;

            State.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minMode);
            State.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            State.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            State.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            State.Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            unsafe
            {
                fixed (byte* ptr = &pixelData[0])
                {
                    State.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)Width, (uint)Height, 0, ToOpenGLPixelFormat(format), GLEnum.UnsignedByte, ptr);
                }
            }

            if (numMipMapLevels > 0)
                State.Gl.GenerateMipmap(GLEnum.Texture2D);
        }

        public virtual void Bind()
        {
            if (_disposed)
                throw new InvalidOperationException("Tried to bind a disposed texture.");

            if (ActiveTexture == this)
                return;

            State.Gl.BindTexture(TextureTarget.Texture2D, Index);
            ActiveTexture = this;
        }

        public static void Unbind()
        {
            State.Gl.BindTexture(TextureTarget.Texture2D, 0);
            ActiveTexture = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ActiveTexture == this)
                        Unbind();

                    if (Index != 0)
                    {
                        State.Gl.DeleteTexture(Index);
                        Index = 0;
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

        }

        protected static byte[] Convert16BitColorWithAlpha(byte[] pixelData)
        {
            int numPixels = pixelData.Length / 2; // 16 bits (2 bytes) per pixel
            var buffer = new byte[numPixels * 4]; // new format has 4 components (RGBA) one byte each

            // Note: RGBA can also be BGRA. The comments below are for RGBA.
            // The order is the same in source and destination so the same
            // code can be used. Only the meaning of the bytes is different.

            for (int i = 0; i < numPixels; ++i)
            {
                var b1 = pixelData[i * 2 + 0];
                var b2 = pixelData[i * 2 + 1];

                // Byte1     Byte2
                // RRRRRGGG  GGBBBBBA
                buffer[i * 4 + 0] = (byte)((b1 >> 3) * 8 + 4); // R
                buffer[i * 4 + 1] = (byte)((((b1 & 0x07) << 2) | (b2 >> 6)) * 8 + 4); // G
                buffer[i * 4 + 2] = (byte)(((b2 >> 1) & 0x1f) * 8 + 4); // B
                buffer[i * 4 + 3] = (byte)((b2 & 0x1) * 255); // A
            }

            return buffer;
        }

        protected static byte[] Convert16BitColorWithoutAlpha(byte[] pixelData)
        {
            int numPixels = pixelData.Length / 2; // 16 bits (2 bytes) per pixel
            var buffer = new byte[numPixels * 3]; // new format has 3 components (RGB) one byte each

            // Note: RGB can also be BGR. The comments below are for RGB.
            // The order is the same in source and destination so the same
            // code can be used. Only the meaning of the bytes is different.

            for (int i = 0; i < numPixels; ++i)
            {
                var b1 = pixelData[i * 2 + 0];
                var b2 = pixelData[i * 2 + 1];

                // Byte1     Byte2
                // RRRRRGGG  GGGBBBBB
                buffer[i * 4 + 0] = (byte)((b1 >> 3) * 8 + 4); // R
                buffer[i * 4 + 1] = (byte)((((b1 & 0x07) << 3) | (b2 >> 5)) * 4 + 2); // G
                buffer[i * 4 + 2] = (byte)((b2 & 0x1f) * 8 + 4); // B
            }

            return buffer;
        }

        protected static byte[] ConvertPixelData(byte[] pixelData, ref PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGB5A1:
                    format = PixelFormat.RGBA8;
                    return Convert16BitColorWithAlpha(pixelData);
                case PixelFormat.R5G6B5:
                    format = PixelFormat.RGB8;
                    return Convert16BitColorWithoutAlpha(pixelData);
                case PixelFormat.BGR5A1:
                    format = PixelFormat.BGRA8;
                    return Convert16BitColorWithAlpha(pixelData);
                case PixelFormat.B5G6R5:
                    format = PixelFormat.BGR8;
                    return Convert16BitColorWithoutAlpha(pixelData);
                default:
                    return pixelData;
            }
        }
    }
}
