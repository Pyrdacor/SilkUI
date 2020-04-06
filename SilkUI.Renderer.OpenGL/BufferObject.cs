using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal struct FreeBufferChunk
    {
        public int Index;
        public int Size;
    }

    internal abstract class BufferObject<T> : IDisposable
    {
        protected T[] _buffer = null;
        protected bool _changedSinceLastCreation = true;
        public abstract int Dimension { get; }
        protected abstract int BytesPerValue { get; }
        public bool Normalized { get; protected set; } = false;
        public abstract int Size { get; }
        public abstract VertexAttribPointerType Type { get; }

        public abstract void Dispose();

        public abstract void Bind();

        internal abstract bool RecreateUnbound();

        protected void EnsureBufferSize(int size)
        {
            if (_buffer == null)
            {
                // first we just use a 256B buffer
                _buffer = new T[256];
            }
            else if (_buffer.Length <= size) // we need to recreate the buffer
            {
                if (_buffer.Length < 0xffff) // double size up to 64K
                    Array.Resize(ref _buffer, _buffer.Length << 1);
                else // increase by 1K after 64K reached
                    Array.Resize(ref _buffer, _buffer.Length + 1024);
            }
        }

        public void Defragment(int newSize, List<FreeBufferChunk> freeBufferChunks)
        {
            int bytesPerEntry = Dimension * BytesPerValue;
            var defragmentedBuffer = new T[newSize];
            int targetOffset = 0;
            int sourceOffset = 0;

            for (int i = 0; i < freeBufferChunks.Count; ++i)
            {
                int dataSize = freeBufferChunks[i].Index * bytesPerEntry - sourceOffset;
                System.Buffer.BlockCopy(_buffer, sourceOffset, defragmentedBuffer, targetOffset, dataSize);
                sourceOffset += dataSize + freeBufferChunks[i].Size * bytesPerEntry;
                targetOffset += dataSize;
            }

            int end = (freeBufferChunks[^1].Index + freeBufferChunks[^1].Size) * bytesPerEntry;

            if (end < _buffer.Length)
                System.Buffer.BlockCopy(_buffer, sourceOffset, defragmentedBuffer, targetOffset, _buffer.Length - end);

            _buffer = defragmentedBuffer;
            _changedSinceLastCreation = true;
        }
    }
}
