using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal abstract class BufferObject<T> : IDisposable
    {
        public abstract int Dimension { get; }
        public bool Normalized { get; protected set; } = false;
        public abstract int Size { get; }
        public abstract VertexAttribPointerType Type { get; }

        public abstract void Dispose();

        public abstract void Bind();

        internal abstract bool RecreateUnbound();

        protected static T[] EnsureBufferSize(T[] buffer, int size, out bool changed)
        {
            changed = false;

            if (buffer == null)
            {
                changed = true;

                // first we just use a 256B buffer
                return new T[256];
            }
            else if (buffer.Length <= size) // we need to recreate the buffer
            {
                changed = true;

                if (buffer.Length < 0xffff) // double size up to 64K
                    Array.Resize(ref buffer, buffer.Length << 1);
                else // increase by 1K after 64K reached
                    Array.Resize(ref buffer, buffer.Length + 1024);
            }

            return buffer;
        }
    }
}
