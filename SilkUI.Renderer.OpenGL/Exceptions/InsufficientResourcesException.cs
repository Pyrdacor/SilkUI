using System;

namespace SilkUI.Renderer.OpenGL.Exceptions
{
    public class InsufficientResourcesException : Exception
    {
        public InsufficientResourcesException(string message)
            : base(message)
        {

        }
    }
}