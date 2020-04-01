using System;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    public class RenderDimensionReference
    {
        public int Width { get; private set; } = 0;
        public int Height { get; private set; } = 0;

        public void SetDimensions(int width, int height)
        {
            if (Width != width || Height != height)
            {
                Width = width;
                Height = height;
                DimensionsChanged?.Invoke();
            }
        }

        internal event Action DimensionsChanged;

        public bool IntersectsWith(Rectangle rect)
        {
            return new Rectangle(0, 0, Width, Height).IntersectsWith(rect);
        }
    }
}