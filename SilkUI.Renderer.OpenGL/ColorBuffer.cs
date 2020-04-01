using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class ColorBuffer : BufferObject<byte>
    {
        private uint _index = 0;
        private bool _disposed = false;
        private byte[] _buffer = null;
        private readonly object _bufferLock = new object();
        private int _size; // count of values
        private readonly IndexPool _indices = new IndexPool();
        private bool _changedSinceLastCreation = true;
        private readonly GLEnum _usageHint = GLEnum.DynamicDraw;

        public override int Size => _size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedByte;

        public override int Dimension => 4;

        public ColorBuffer(bool staticData)
        {
            _index = State.Gl.GenBuffer();

            if (staticData)
                _usageHint = GLEnum.StaticDraw;
        }

        public int Add(Color color, int index = -1)
        {
            bool reused;

            if (index == -1)
                index = _indices.AssignNextFreeIndex(out reused);
            else
                reused = _indices.AssignIndex(index);

            if (_buffer == null)
            {
                _buffer = new byte[128];
                _buffer[0] = color.R;
                _buffer[1] = color.G;
                _buffer[2] = color.B;
                _buffer[3] = color.A;
                _size = 4;
                _changedSinceLastCreation = true;
            }
            else
            {
                _buffer = EnsureBufferSize(_buffer, index * 4, out bool changed);

                if (!reused)
                    _size += 4;

                int bufferIndex = index * 4;

                if (_buffer[bufferIndex + 0] != color.R ||
                    _buffer[bufferIndex + 1] != color.G ||
                    _buffer[bufferIndex + 2] != color.B ||
                    _buffer[bufferIndex + 3] != color.A)
                {
                    _buffer[bufferIndex + 0] = color.R;
                    _buffer[bufferIndex + 1] = color.G;
                    _buffer[bufferIndex + 2] = color.B;
                    _buffer[bufferIndex + 3] = color.A;

                    _changedSinceLastCreation = true;
                }
                else if (changed)
                {
                    _changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, Color color)
        {
            int bufferIndex = index * 4;

            if (_buffer[bufferIndex + 0] != color.R ||
                _buffer[bufferIndex + 1] != color.G ||
                _buffer[bufferIndex + 2] != color.B ||
                _buffer[bufferIndex + 3] != color.A)
            {
                _buffer[bufferIndex + 0] = color.R;
                _buffer[bufferIndex + 1] = color.G;
                _buffer[bufferIndex + 2] = color.B;
                _buffer[bufferIndex + 3] = color.A;

                _changedSinceLastCreation = true;
            }
        }

        public void Remove(int index)
        {
            _indices.UnassignIndex(index);
        }

        public void ReduceSizeTo(int size)
        {
            _size = size;
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
