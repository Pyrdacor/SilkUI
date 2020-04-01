using System.Runtime.ExceptionServices;
using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    // TODO: for OGL version < 4.1 we should use Gl.UniformX instead of Gl.ProgramUniformX
    internal class ShaderProgram : IDisposable
    {
        private Shader _fragmentShader = null;
        private Shader _vertexShader = null;
        private bool _disposed = false;
        private string _fragmentColorOutputName = "color";

        public uint ProgramIndex { get; private set; } = 0;
        public bool Loaded { get; private set; } = false;
        public bool Linked { get; private set; } = false;
        public static ShaderProgram ActiveProgram { get; private set; } = null;

        public ShaderProgram()
        {
            Create();
        }

        public ShaderProgram(Shader fragmentShader, Shader vertexShader)
        {
        	Create();

            AttachShader(fragmentShader);
            AttachShader(vertexShader);

            Link(false);
        }

        void Create()
        {
            ProgramIndex = State.Gl.CreateProgram();
        }

        public bool SetFragmentColorOutputName(string name)
        {
        	if (!string.IsNullOrWhiteSpace(name))
            {
            	_fragmentColorOutputName = name;
                return true;
            }

            return false;
        }

        public void AttachShader(Shader shader)
        {
        	if (shader == null)
            	return;

            if (shader.ShaderType == Shader.Type.Fragment)
            {
            	if (_fragmentShader == shader)
                	return;

            	if (_fragmentShader != null)
                    State.Gl.DetachShader(ProgramIndex, _fragmentShader.ShaderIndex);

                _fragmentShader = shader;
                State.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
            }
            else if (shader.ShaderType == Shader.Type.Vertex)
            {
            	if (_vertexShader == shader)
                	return;

            	if (_vertexShader != null)
                    State.Gl.DetachShader(ProgramIndex, _vertexShader.ShaderIndex);

                _vertexShader = shader;
                State.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
            }

            Linked = false;
            Loaded = _fragmentShader != null && _vertexShader != null;
        }

        public void Link(bool detachShaders)
        {
            if (!Linked)
            {
            	if (!Loaded)
            		throw new InvalidOperationException($"{nameof(ShaderProgram)}.{nameof(Link)}: Shader program was not loaded.");

                State.Gl.LinkProgram(ProgramIndex);

                // Auf Fehler pr�fen
                string infoLog = State.Gl.GetProgramInfoLog(ProgramIndex);

                if (!string.IsNullOrWhiteSpace(infoLog))
                {
                    throw new Exceptions.ShaderProgramLoadException(infoLog.Trim());
                }

                Linked = true;
            }

            if (detachShaders)
            {
            	if (_fragmentShader != null)
                {
                    State.Gl.DetachShader(ProgramIndex, _fragmentShader.ShaderIndex);
                    _fragmentShader = null;
                }

                if (_vertexShader != null)
                {
                    State.Gl.DetachShader(ProgramIndex, _vertexShader.ShaderIndex);
                    _vertexShader = null;
                }

                Loaded = false;
            }
        }

        public void Use()
        {
        	if (!Linked)
            	throw new InvalidOperationException("ShaderProgram.Use: Shader program was not linked.");

            State.Gl.UseProgram(ProgramIndex);
            ActiveProgram = this;

            //Gl.BindFragDataLocation(ProgramIndex, 0, fragmentColorOutputName);
        }

        public static void Use(ShaderProgram program)
        {
            if (program != null)
                program.Use();
            else
            {
                State.Gl.UseProgram(0);
                ActiveProgram = null;
            }
        }

        public uint BindInputBuffer<T>(string name, BufferObject<T> buffer)
        {
        	if (ActiveProgram != this)
            	throw new InvalidOperationException("ShaderProgram.SetInputBuffer: Shader program is not active.");

            var location = GetLocation(name, true);

            buffer.Bind();

            State.Gl.EnableVertexAttribArray(location);

            unsafe
            {
                State.Gl.VertexAttribIPointer(location, buffer.Dimension, buffer.Type, 0, (void*)0);
            }

            return location;
        }

        public void UnbindInputBuffer(uint location)
        {
            State.Gl.DisableVertexAttribArray(location);
        }

        uint GetLocation(string name, bool preferAttribute = false)
        {
            if (preferAttribute)
                return (uint)State.Gl.GetAttribLocation(ProgramIndex, name);

            return (uint)State.Gl.GetUniformLocation(ProgramIndex, name);
        }

        public void SetInputMatrix(string name, float[] matrix, bool transpose)
        {
            var location = GetLocation(name);

            switch (matrix.Length)
            {
                case 4: // 2x2
                    State.Gl.ProgramUniformMatrix2(ProgramIndex, (int)location, 1, transpose, matrix);
                    break;
                case 9: // 3x3
                    State.Gl.ProgramUniformMatrix3(ProgramIndex, (int)location, 1, transpose, matrix);
                    break;
                case 16: // 4x4
                    State.Gl.ProgramUniformMatrix4(ProgramIndex, (int)location, 1, transpose, matrix);
                    break;
                default:
                	throw new InvalidOperationException("ShaderProgram.SetInputMatrix: Unsupported matrix dimensions. Valid are 2x2, 3x3 or 4x4.");
            }
        }

		public void SetInput(string name, bool value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, (value) ? 1 : 0);
        }

        public void SetInput(string name, float value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInput(string name, double value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, (float)value);
        }

        public void SetInput(string name, int value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInput(string name, uint value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInputVector2(string name, float x, float y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector2(string name, int x, int y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector2(string name, uint x, uint y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector3(string name, float x, float y, float z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector3(string name, int x, int y, int z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector3(string name, uint x, uint y, uint z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector4(string name, float x, float y, float z, float w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
        }

        public void SetInputVector4(string name, int x, int y, int z, int w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
        }

        public void SetInputVector4(string name, uint x, uint y, uint z, uint w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ProgramIndex != 0)
                    {
                    	if (ActiveProgram == this)
                        {
                            State.Gl.UseProgram(0);
                            ActiveProgram = null;
                        }

                        State.Gl.DeleteProgram(ProgramIndex);
                        ProgramIndex = 0;
                    }

                    _disposed = true;
                }
            }
        }
   	}
}