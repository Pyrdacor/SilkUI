using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class PositionBuffer : BufferObject<short>
    {
        private uint _index = 0;
        private bool _disposed = false;
        private short[] _buffer = null;
        private readonly object _bufferLock = new object();
        private int _size;
        private readonly IndexPool _indices = new IndexPool();
        private bool _changedSinceLastCreation = true;
        private readonly GLEnum _usageHint = GLEnum.DynamicDraw;

        public override int Size => _size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.Short;

        public override int Dimension => 2;

        public PositionBuffer(bool staticData)
        {
            _index = State.Gl.GenBuffer();

            if (staticData)
                _usageHint = GLEnum.StaticDraw;
        }

        public bool IsPositionValid(int index)
        {
            index *= Dimension; // 2 coords each

            if (index < 0 || index >= _buffer.Length)
                return false;

            return _buffer[index] != short.MaxValue;
        }

        public int Add(short x, short y, int index = -1)
        {
            bool reused;

            if (index == -1)
                index = _indices.AssignNextFreeIndex(out reused);
            else
                reused = _indices.AssignIndex(index);

            if (_buffer == null)
            {
                _buffer = new short[256];
                _buffer[0] = x;
                _buffer[1] = y;
                _size = 2;
                _changedSinceLastCreation = true;
            }
            else
            {
                _buffer = EnsureBufferSize(_buffer, index * 2, out bool changed);

                if (!reused)
                    _size += 2;

                int bufferIndex = index * 2;

                if (_buffer[bufferIndex + 0] != x ||
                    _buffer[bufferIndex + 1] != y)
                {
                    _buffer[bufferIndex + 0] = x;
                    _buffer[bufferIndex + 1] = y;

                    _changedSinceLastCreation = true;
                }
                else if (changed)
                {
                    _changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, short x, short y)
        {
            int bufferIndex = index * 2;

            if (_buffer[bufferIndex + 0] != x ||
                _buffer[bufferIndex + 1] != y)
            {
                _buffer[bufferIndex + 0] = x;
                _buffer[bufferIndex + 1] = y;

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
