using System;

namespace SilkUI.Renderer.OpenGL.Exceptions
{
    public class ShaderProgramLoadException : Exception
    {
        public ShaderProgramLoadException(string message)
            : base(message)
        {

        }
    }
}