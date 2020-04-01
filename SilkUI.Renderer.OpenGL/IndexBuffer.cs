using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using IndexType = UInt32;

    internal class IndexBuffer : BufferObject<IndexType>
    {
        private uint _index = 0;
        private bool _disposed = false;
        private readonly object _bufferLock = new object();
        private IndexType[] _buffer = null;
        private bool _changedSinceLastCreation = true;
        private int _size = 0;

        public override int Size => _size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedInt;

        public override int Dimension { get; }

        public IndexBuffer(int dimension)
        {
            _index = State.Gl.GenBuffer();
            Dimension = dimension;
        }

        public override void Bind()
        {
            if (_disposed)
                throw new Exception("Tried to bind a disposed buffer.");

            State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, _index);

            Recreate(); // ensure that the data is up to date
        }

        public void Unbind()
        {
            if (_disposed)
                return;

            State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        }

        void Recreate() // is only called when the buffer is bound (see Bind())
        {
            if (!_changedSinceLastCreation || _buffer == null)
                return;

            lock (_bufferLock)
            {
                unsafe
                {
                    fixed (IndexType* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ElementArrayBuffer, (uint)(Size * sizeof(IndexType)),
                            ptr, GLEnum.StaticDraw);
                    }
                }
            }

            _changedSinceLastCreation = false;
        }

        internal override bool RecreateUnbound()
        {
            if (!_changedSinceLastCreation || _buffer == null)
                return false;

            if (_disposed)
                throw new InvalidOperationException("Tried to recreate a disposed buffer.");

            State.Gl.BindBuffer(GLEnum.ArrayBuffer, _index);

            lock (_bufferLock)
            {
                unsafe
                {
                    fixed (IndexType* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(IndexType)),
                            ptr, GLEnum.StaticDraw);
                    }
                }
            }

            _changedSinceLastCreation = false;

            return true;
        }

        public void InsertPrimitive(int offset, IndexType restartIndex)
        {
            int numIndices = Dimension;
            int numVertices = numIndices - 1; // 1 for the restart index
            int vertexOffset = (offset / numIndices) * numVertices;

            if (offset > int.MaxValue - numIndices)
                throw new Exceptions.InsufficientResourcesException("Too many polygons to render.");

            if (_size < offset + numIndices)
            {
                _buffer = EnsureBufferSize(_buffer, (int)offset + numIndices, out _);
                _size = _buffer.Length;
                _changedSinceLastCreation = true;
            }

            for (int i = 0; i < numVertices; ++i)
                _buffer[offset++] = (IndexType)vertexOffset++;
            _buffer[offset++] = restartIndex;
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

                    if (_index != 0)
                    {
                        State.Gl.DeleteBuffer(_index);

                        if (_buffer != null)
                        {
                            lock (_bufferLock)
                            {
                                _buffer = null;
                            }
                        }

                        _size = 0;
                        _index = 0;
                    }

                    _disposed = true;
                }
            }
        }
    }
}
