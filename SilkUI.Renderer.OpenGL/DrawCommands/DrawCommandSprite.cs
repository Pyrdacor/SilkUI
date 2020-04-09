using System.Drawing;
using SilkUI.Renderer.OpenGL.Shaders;

namespace SilkUI.Renderer.OpenGL
{
    internal class DrawCommandSprite : DrawCommand
    {
        public DrawCommandSprite(int x, int y, int width, int height, uint z, Color colorOverlay,
            Texture texture, Point texCoords, bool transparency, Rectangle? clipRect)
            : base(new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }, z, colorOverlay, transparency || colorOverlay.A < 255, 0u, 0u, TextureShader.Instance, texture,
                // TODO: check texture before if it contains semi-transparent pixels and set transparency to true in that case
                new Point[4]
                {
                    new Point(texCoords.X, texCoords.Y),
                    new Point(texCoords.X + width, texCoords.Y),
                    new Point(texCoords.X + width, texCoords.Y + height),
                    new Point(texCoords.X, texCoords.Y + height)
                }, clipRect)
        {
            
        }
    }
}
