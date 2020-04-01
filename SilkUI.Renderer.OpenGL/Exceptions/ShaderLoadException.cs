using System;

namespace SilkUI.Renderer.OpenGL.Exceptions
{
    public class ShaderLoadException : Exception
    {
        public ShaderLoadException(string message)
            : base(message)
        {

        }
    }
}