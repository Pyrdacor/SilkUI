using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class ColorBuffer : BufferObject<byte>
    {
        private uint _index = 0;
        private int _size; // count of values
        private bool _disposed = false;
        private readonly object _bufferLock = new object();        
        private readonly GLEnum _usageHint = GLEnum.DynamicDraw;

        public override int Size => _size;
        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedByte;
        public override int Dimension => 4;
        protected override int BytesPerValue => 1;

        public ColorBuffer(bool staticData)
        {
            _index = State.Gl.GenBuffer();

            if (staticData)
                _usageHint = GLEnum.StaticDraw;
        }

        public void Add(int index, Color color)
        {
            EnsureBufferSize(index * Dimension + Dimension);

            int bufferIndex = index * Dimension;

            if (bufferIndex == _size)
                _size += Dimension;

            _buffer[bufferIndex + 0] = color.R;
            _buffer[bufferIndex + 1] = color.G;
            _buffer[bufferIndex + 2] = color.B;
            _buffer[bufferIndex + 3] = color.A;

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
                    fixed (byte* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(byte)),
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
                    fixed (byte* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(byte)),
                            ptr, _usageHint);
                    }
                }
            }

            _changedSinceLastCreation = false;

            return true;
        }
    }
}
