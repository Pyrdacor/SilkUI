using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using Shaders;
    using System.Linq;

    // VAO
    internal class VertexArrayObject : IDisposable
    {
    	private uint _index = 0;
        // The key is the chunk size and the value is a list of indices.
        private readonly Dictionary<int, List<int>> _freeBufferChunks = new Dictionary<int, List<int>>();
        // The key is the end position of the free chunks and the value is their index/size pair as a tuple.
        private readonly Dictionary<int, Tuple<int, int>> _freeBufferChunkEndPositions = new Dictionary<int, Tuple<int, int>>();
        private int _fragmentedSize = 0;
        private int _usedSize = 0;
        private readonly Dictionary<string, PositionBuffer> _positionBuffers = new Dictionary<string, PositionBuffer>(4);
        private readonly Dictionary<string, ColorBuffer> _colorBuffers = new Dictionary<string, ColorBuffer>(4);
        private readonly Dictionary<string, ValueBuffer> _valueBuffers = new Dictionary<string, ValueBuffer>(1);
        private readonly Dictionary<string, int> _bufferLocations = new Dictionary<string, int>();
        private bool _disposed = false;
        private bool _buffersAreBound = false;        
        private object _vaoLock = new object();
        public ShaderBase Shader { get; }
        public static VertexArrayObject ActiveVAO { get; private set; } = null;

        public VertexArrayObject(ShaderBase shader)
        {
            Shader = shader;

            Create();
        }

        public bool DepthTest
        {
            get;
            set;
        } = true;

        public bool DepthWrite
        {
            get;
            set;
        } = true;

        public bool Blending
        {
            get;
            set;
        } = false;

        public Texture Texture
        {
            get;
            set;
        }

        public Color? ColorKey
        {
            get;
            set;
        }

        public IndexBuffer IndexBuffer { get; } = new IndexBuffer();

        void Create()
        {
            _index = State.Gl.GenVertexArray();
        }

        public void Lock()
        {
            Monitor.Enter(_vaoLock);
        }

        public void Unlock()
        {
            Monitor.Exit(_vaoLock);
        }

        public int GetBufferIndex(int numVertices)
        {
            int index;

            if (_freeBufferChunks.ContainsKey(numVertices))
            {
                var freeBufferChunks = _freeBufferChunks[numVertices];

                if (freeBufferChunks.Count != 0)
                {
                    index = freeBufferChunks[0];
                    freeBufferChunks.RemoveAt(0);
                    _freeBufferChunkEndPositions.Remove(index + numVertices);
                    return index;
                }
            }

            index = _usedSize;
            _usedSize += numVertices;
            return index;
        }

        /// <summary>
        /// Returns true if the buffers were defragmented.
        /// </summary>
        public void FreeChunk(int index, int size)
        {
            if (index + size == _usedSize)
            {
                _usedSize = index;

                while (_freeBufferChunkEndPositions.ContainsKey(_usedSize))
                {
                    var freeBufferChunkInfo = _freeBufferChunkEndPositions[_usedSize];
                    int freeBufferChunkIndex = freeBufferChunkInfo.Item1;
                    int freeBufferChunkSize = freeBufferChunkInfo.Item2;
                    _freeBufferChunks[freeBufferChunkSize].Remove(freeBufferChunkIndex);
                    _freeBufferChunkEndPositions.Remove(_usedSize);
                    _fragmentedSize -= freeBufferChunkSize;
                    _usedSize -= freeBufferChunkSize;
                }    

                return;
            }

            if (!_freeBufferChunks.ContainsKey(size))
                _freeBufferChunks.Add(size, new List<int>());

            _freeBufferChunkEndPositions.Add(index + size, Tuple.Create(index, size));
            _freeBufferChunks[size].Add(index);
            _fragmentedSize += size;

            if (_fragmentedSize >= 1024) // 1KB fragmented unused data reached -> defragment buffers.
                DefragmentBuffers();
        }

        private void DefragmentBuffers()
        {
            int newSize = _usedSize - _fragmentedSize;
            var chunks = new List<FreeBufferChunk>(_freeBufferChunks.SelectMany(
                c => c.Value.Select(index => new FreeBufferChunk { Index = index, Size = c.Key })
            ));
            chunks.Sort((a, b) => a.Index.CompareTo(b.Index));

            foreach (var positionBuffer in _positionBuffers)
            {
                positionBuffer.Value.Defragment(newSize, chunks);
            }

            foreach (var valueBuffer in _valueBuffers)
            {
                valueBuffer.Value.Defragment(newSize, chunks);
            }

            foreach (var colorBuffer in _colorBuffers)
            {
                colorBuffer.Value.Defragment(newSize, chunks);
            }

            // The index buffer will be messed up for VAOs with
            // transparency rendering but it will be recreated in
            // that case anyway.
            IndexBuffer.Defragment(newSize, chunks);

            _fragmentedSize = 0;
            _usedSize -= _fragmentedSize;
            _freeBufferChunks.Clear();
            _freeBufferChunkEndPositions.Clear();
        }

        public PositionBuffer EnsurePositionBuffer(string name, bool staticData)
        {
            if (_positionBuffers.ContainsKey(name))
                return _positionBuffers[name];

            var buffer = new PositionBuffer(staticData);
            _positionBuffers.Add(name, buffer);
            return buffer;
        }

        public ColorBuffer EnsureColorBuffer(string name, bool staticData)
        {
            if (_colorBuffers.ContainsKey(name))
                return _colorBuffers[name];

            var buffer = new ColorBuffer(staticData);
            _colorBuffers.Add(name, buffer);
            return buffer;
        }

        public ValueBuffer EnsureValueBuffer(string name, bool staticData)
        {
            if (_valueBuffers.ContainsKey(name))
                return _valueBuffers[name];

            var buffer = new ValueBuffer(staticData);
            _valueBuffers.Add(name, buffer);
            return buffer;
        }

        public PositionBuffer GetPositionBuffer(string name)
        {
            return _positionBuffers[name];
        }

        public ColorBuffer GetColorBuffer(string name)
        {
            return _colorBuffers[name];
        }

        public ValueBuffer GetValueBuffer(string name)
        {
            return _valueBuffers[name];
        }

        private void BindBuffers()
        {
            if (_buffersAreBound)
                return;

            lock (_vaoLock)
            {
                Shader.ShaderProgram.Use();
                InternalBind(true);

                foreach (var buffer in _positionBuffers)
                {
                    _bufferLocations[buffer.Key] = (int)Shader.ShaderProgram.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in _colorBuffers)
                {
                    _bufferLocations[buffer.Key] = (int)Shader.ShaderProgram.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in _valueBuffers)
                {
                    _bufferLocations[buffer.Key] = (int)Shader.ShaderProgram.BindInputBuffer(buffer.Key, buffer.Value);
                }

                IndexBuffer.Bind();

                _buffersAreBound = true;
            }
        }

        private void UnbindBuffers()
        {
            if (!_buffersAreBound)
                return;

            lock (_vaoLock)
            {
                Shader.ShaderProgram.Use();
                InternalBind(true);

                foreach (var buffer in _positionBuffers)
                {
                    Shader.ShaderProgram.UnbindInputBuffer((uint)_bufferLocations[buffer.Key]);
                    _bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in _colorBuffers)
                {
                    Shader.ShaderProgram.UnbindInputBuffer((uint)_bufferLocations[buffer.Key]);
                    _bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in _valueBuffers)
                {
                    Shader.ShaderProgram.UnbindInputBuffer((uint)_bufferLocations[buffer.Key]);
                    _bufferLocations[buffer.Key] = -1;
                }

                IndexBuffer.Unbind();

                _buffersAreBound = false;
            }
        }

        public void Bind()
        {
            if (_disposed)
                throw new InvalidOperationException("Can not bind a disposed VAO.");

            InternalBind(false);
        }

        private void InternalBind(bool bindOnly)
        {
            lock (_vaoLock)
            {
                if (ActiveVAO != this)
                {
                    State.Gl.BindVertexArray(_index);
                    Shader.ShaderProgram.Use();
                }

                if (Shader is TextureShader textureShader)
                {
                    if (Texture == null)
                        return;

                    textureShader.SetSampler(0);
                    textureShader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                    State.Gl.ActiveTexture(GLEnum.Texture0);
                    Texture.Bind();

                    if (ColorKey.HasValue)
                        textureShader.SetColorKey(ColorKey.Value.R / 255.0f, ColorKey.Value.G / 255.0f, ColorKey.Value.B / 255.0f);
                    else
                        textureShader.SetColorKey(1.0f, 0.0f, 1.0f); // default to magenta
                }

                if (DepthTest)
                    State.Gl.Enable(EnableCap.DepthTest);
                else
                    State.Gl.Disable(EnableCap.DepthTest);

                State.Gl.DepthMask(DepthWrite);

                if (Blending)
                    State.Gl.Enable(EnableCap.Blend);
                else
                    State.Gl.Disable(EnableCap.Blend);

                if (!bindOnly)
                {
                    bool buffersChanged = false;

                    // ensure that all buffers are up to date
                    foreach (var buffer in _positionBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in _colorBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in _valueBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    if (IndexBuffer.RecreateUnbound())
                        buffersChanged = true;

                    if (buffersChanged)
                    {
                        UnbindBuffers();
                        BindBuffers();
                    }
                }

                ActiveVAO = this;
            }
        }

        public static void Bind(VertexArrayObject vao)
        {
            if (vao != null)
            	vao.Bind();
            else
            	Unbind();
        }

        public static void Unbind()
        {
            State.Gl.BindVertexArray(0);
            ActiveVAO = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_index != 0)
                {
                    if (ActiveVAO == this)
                    	Unbind();

                    foreach (var buffer in _positionBuffers)
                        buffer.Value.Dispose();
                    _positionBuffers.Clear();

                    foreach (var buffer in _colorBuffers)
                        buffer.Value.Dispose();
                    _colorBuffers.Clear();

                    foreach (var buffer in _valueBuffers)
                        buffer.Value.Dispose();
                    _valueBuffers.Clear();

                    IndexBuffer.Dispose();

                    State.Gl.DeleteVertexArray(_index);
                    _index = 0;
                }

                _disposed = true;
            }
        }
   	}
}