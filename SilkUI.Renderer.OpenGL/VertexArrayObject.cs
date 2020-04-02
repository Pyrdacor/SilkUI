using System;
using System.Collections.Generic;
using System.Threading;

namespace SilkUI.Renderer.OpenGL
{
    using Shaders;

	// VAO
    internal class VertexArrayObject : IDisposable
    {
    	private uint index = 0;
        private readonly Dictionary<string, PositionBuffer> _positionBuffers = new Dictionary<string, PositionBuffer>(4);
        private readonly Dictionary<string, ColorBuffer> _colorBuffers = new Dictionary<string, ColorBuffer>(4);
        private readonly Dictionary<string, ValueBuffer> _valueBuffers = new Dictionary<string, ValueBuffer>(1);
        private readonly Dictionary<string, IndexBuffer> _indexBuffers = new Dictionary<string, IndexBuffer>(4);
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

        void Create()
        {
            index = State.Gl.GenVertexArray();
        }

        public void Lock()
        {
            Monitor.Enter(_vaoLock);
        }

        public void Unlock()
        {
            Monitor.Exit(_vaoLock);
        }

        public void AddBuffer(string name, PositionBuffer buffer)
        {
            _positionBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, ColorBuffer buffer)
        {
            _colorBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, ValueBuffer buffer)
        {
            _valueBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, IndexBuffer buffer)
        {
            _indexBuffers.Add(name, buffer);
        }

        public void BindBuffers()
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

                foreach (var buffer in _indexBuffers)
                {
                    buffer.Value.Bind();
                }

                _buffersAreBound = true;
            }
        }

        public void UnbindBuffers()
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

                foreach (var buffer in _indexBuffers)
                {
                    buffer.Value.Unbind();
                }

                _buffersAreBound = false;
            }
        }

        public void Bind()
        {
            InternalBind(false);
        }

        void InternalBind(bool bindOnly)
        {
            lock (_vaoLock)
            {
                if (ActiveVAO != this)
                {
                    State.Gl.BindVertexArray(index);
                    Shader.ShaderProgram.Use();
                }

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

                    foreach (var buffer in _indexBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

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
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (index != 0)
                    {
                    	if (ActiveVAO == this)
                    		Unbind();

                        State.Gl.DeleteVertexArray(index);
                        index = 0;
                    }

                    _disposed = true;
                }
            }
        }
   	}
}