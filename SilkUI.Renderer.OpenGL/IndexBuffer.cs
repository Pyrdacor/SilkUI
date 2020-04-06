using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using IndexType = UInt32;

    internal class IndexBuffer : BufferObject<IndexType>
    {
        public const IndexType PrimitiveRestartIndex = IndexType.MaxValue;

        private uint _index = 0;
        private bool _disposed = false;
        private readonly object _bufferLock = new object();
        private int _size = 0;

        public override int Size => _size;
        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedInt;
        public override int Dimension => 1;
        protected override int BytesPerValue => 4;

        public IndexBuffer()
        {
            _index = State.Gl.GenBuffer();
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

        public void AddPrimitive(int numVertices, int firstIndex)
        {
            EnsureBufferSize(_size + numVertices + 1);

            for (int i = 0; i < numVertices; ++i)
                _buffer[_size++] = (IndexType)firstIndex++;
            _buffer[_size++] = PrimitiveRestartIndex;

            _changedSinceLastCreation = true;
        }

        public void Clear()
        {
            _buffer = null;
            _size = 0;
        }

        public override void Dispose()
        {
            if (!_disposed)
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
