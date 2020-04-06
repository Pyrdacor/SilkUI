using System;
using System.Drawing;
using SilkUI.Renderer.OpenGL.Shaders;

namespace SilkUI.Renderer.OpenGL
{
    internal class DrawCommandPolygon : DrawCommand
    {
        public DrawCommandPolygon(Point[] vertexPositions, uint z, Color color)
            : base(vertexPositions, z, color, color.A < 255, 0u, 0u, PolygonShader.Instance, null, null)
        {
            
        }
    }
}
