using System;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class Shader : IDisposable
    {
    	public enum Type
        {
            Fragment,
            Vertex
        }

        private string _code = "";
        private bool _disposed = false;

        public Type ShaderType { get; } = Type.Fragment;
        public uint ShaderIndex { get; private set; } = 0;

        public Shader(Type type, string code)
        {
        	ShaderType = type;
            _code = code;

            Create();
        }

        void Create()
        {
            ShaderIndex = State.Gl.CreateShader((ShaderType == Type.Fragment) ?
                GLEnum.FragmentShader :
                GLEnum.VertexShader);

            State.Gl.ShaderSource(ShaderIndex, 1, new string[] { _code }, new Int32[] { _code.Length });
            State.Gl.CompileShader(ShaderIndex);

            // Check for errors
            string infoLog = State.Gl.GetShaderInfoLog(ShaderIndex);

            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exceptions.ShaderLoadException(infoLog.Trim());
            }
        }

        public void AttachToProgram(ShaderProgram program)
        {
        	program.AttachShader(this);
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
                    if (ShaderIndex != 0)
                    {
                        State.Gl.DeleteShader(ShaderIndex);
                        ShaderIndex = 0;
                    }

                    _disposed = true;
                }
            }
        }
   	}
}