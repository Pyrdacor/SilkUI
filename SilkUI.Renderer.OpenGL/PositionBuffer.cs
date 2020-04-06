using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class PositionBuffer : BufferObject<short>
    {
        private uint _index = 0;
        private int _size;
        private bool _disposed = false;
        private readonly object _bufferLock = new object();        
        private readonly GLEnum _usageHint = GLEnum.DynamicDraw;

        public override int Size => _size;
        public override VertexAttribPointerType Type => VertexAttribPointerType.Short;
        public override int Dimension => 2;
        protected override int BytesPerValue => 2;

        public PositionBuffer(bool staticData)
        {
            _index = State.Gl.GenBuffer();

            if (staticData)
                _usageHint = GLEnum.StaticDraw;
        }

        public void Add(int index, short x, short y)
        {
            EnsureBufferSize(index * Dimension + Dimension);

            int bufferIndex = index * Dimension;

            if (bufferIndex == _size)
                _size += Dimension;

            _buffer[bufferIndex + 0] = x;
            _buffer[bufferIndex + 1] = y;

            _changedSinceLastCreation = true;
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                State.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

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

        public override void Bind()
        {
            if (_disposed)
                throw new InvalidOperationException("Tried to bind a disposed buffer.");

            State.Gl.BindBuffer(GLEnum.ArrayBuffer, _index);

            Recreate(); // ensure that the data is up to date
        }

        void Recreate() // is only called when the buffer is bound (see Bind())
        {
            if (!_changedSinceLastCreation || _buffer == null)
                return;

            lock (_bufferLock)
            {
                unsafe
                {
                    fixed (short* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(short)),
                            ptr, _usageHint);
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
                    fixed (short* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(short)),
                            ptr, _usageHint);
                    }
                }
            }

            _changedSinceLastCreation = false;

            return true;
        }
    }
}
