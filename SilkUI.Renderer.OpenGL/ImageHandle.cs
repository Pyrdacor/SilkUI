using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SilkUI.Renderer.OpenGL
{
    /// <summary>
    /// Identifies an image of different types.
    /// </summary>
    internal class ImageHandle : IEquatable<ImageHandle>
    {
        public uint Index { get; }
        public Bitmap _image;
        public FreeType.Glyph? _glyph;

        internal ImageHandle(uint index, Bitmap image)
        {
            _image = image;
            Index = index;
        }

        internal ImageHandle(FreeType.Glyph glyph)
        {
            _glyph = glyph;
            Index = glyph.CharCode;
        }

        public int Width
        {
            get
            {
                if (_image != null)
                    return _image.Width;
                if (_glyph != null)
                    return _glyph.Value.Width;
                return 0;
            }
        }

        public int Height
        {
            get
            {
                if (_image != null)
                    return _image.Height;
                if (_glyph != null)
                    return _glyph.Value.Height;
                return 0;
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {
                if (_image != null)
                    return PixelFormat.Format32bppArgb; // We always read data as 32 bit ARGB
                if (_glyph != null)
                    return PixelFormat.Alpha;
                return PixelFormat.DontCare;
            }
        }

        public int BytesPerPixel
        {
            get
            {
                if (_image != null)
                    return 4;
                if (_glyph != null)
                    return 1;
                return 0;
            }
        }

        public byte[] Data
        {
            get
            {
                if (_image != null)
                    return _image.GetData();
                if (_glyph != null)
                    return _glyph.Value.ImageData;
                return null;
            }
        }

        public bool Equals(ImageHandle other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (_image != null)
                return _image == other._image;
            if (_glyph != null)
                return other._glyph != null && _glyph.Value.CharCode == other._glyph.Value.CharCode;

            return other._image == null && other._glyph == null;
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            return Equals((ImageHandle)obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;

            if (_image != null)
                hash = hash * 23 + _image.GetHashCode();
            if (_glyph != null)
                hash = hash * 23 + _glyph.Value.CharCode.GetHashCode();

            return hash;
        }

        public static bool operator ==(ImageHandle lhs, ImageHandle rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ImageHandle lhs, ImageHandle rhs)
        {
            return !(lhs == rhs);
        }
    }

    public static class ImageExtensions
    {
        public static byte[] GetData(this Bitmap image)
        {
            var data = image.LockBits(new Rectangle(Point.Empty, image.Size),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                byte[] buffer = new byte[data.Width * data.Height * 4];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                return buffer;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
    }
}
