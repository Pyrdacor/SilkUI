using System;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    internal class DrawCommandRect : DrawCommandPolygon
    {
        public DrawCommandRect(int x, int y, int width, int height, uint z, Color color)
            : base(new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }, z, color)
        {
            
        }
    }
}
