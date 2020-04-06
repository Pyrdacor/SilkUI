using System.Drawing;
using SilkUI.Renderer.OpenGL.Shaders;

namespace SilkUI.Renderer.OpenGL
{
    internal class DrawCommandBlurredRect : DrawCommand
    {
        public DrawCommandBlurredRect(int x, int y, int width, int height, uint z, Color color, uint blurRadius)
            : base(new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }, z, color, blurRadius != 0u || color.A < 255, 0u, blurRadius, BlurRectShader.Instance, null, null)
        {
            
        }
    }
}
