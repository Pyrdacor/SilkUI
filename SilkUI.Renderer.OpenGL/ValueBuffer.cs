using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using LayerValueType = UInt32;

    internal class ValueBuffer : BufferObject<LayerValueType>
    {
        private uint _index = 0;
        private bool _disposed = false;
        private LayerValueType[] _buffer = null;
        private readonly object _bufferLock = new object();
        private int _size; // count of values
        private readonly IndexPool _indices = new IndexPool();
        private bool _changedSinceLastCreation = true;
        private readonly GLEnum _usageHint = GLEnum.DynamicDraw;

        public override int Size => _size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedInt;

        public override int Dimension => 1;

        public ValueBuffer(bool staticData)
        {
            _index = State.Gl.GenBuffer();

            if (staticData)
                _usageHint = GLEnum.StaticDraw;
        }

        public int Add(LayerValueType layer, int index = -1)
        {
            bool reused;

            if (index == -1)
                index = _indices.AssignNextFreeIndex(out reused);
            else
                reused = _indices.AssignIndex(index);

            if (_buffer == null)
            {
                _buffer = new LayerValueType[128];
                _buffer[0] = layer;
                _size = 1;
                _changedSinceLastCreation = true;
            }
            else
            {
                if (index == _buffer.Length) // we need to recreate the buffer
                {
                    if (_buffer.Length < 512)
                        Array.Resize(ref _buffer, _buffer.Length + 128);
                    else if (_buffer.Length < 2048)
                        Array.Resize(ref _buffer, _buffer.Length + 256);
                    else
                        Array.Resize(ref _buffer, _buffer.Length + 512);

                    _changedSinceLastCreation = true;
                }

                if (!reused)
                    ++_size;

                if (_buffer[index] != layer)
                {
                    _buffer[index] = layer;

                    _changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, LayerValueType layer)
        {
            if (_buffer[index] != layer)
            {
                _buffer[index] = layer;

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
                    fixed (LayerValueType* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(LayerValueType)),
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
                    fixed (LayerValueType* ptr = &_buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(LayerValueType)),
                            ptr, _usageHint);
                    }
                }
            }

            _changedSinceLastCreation = false;

            return true;
        }
    }
}
