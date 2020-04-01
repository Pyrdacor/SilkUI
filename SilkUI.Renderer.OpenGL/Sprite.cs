using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    /// <summary>
    /// A sprite has a fixed size and an offset into the layer's texture atlas or no texture at all.
    /// The layer will sort sprites by size and then by the texture atlas offset.
    /// </summary>
    internal class Sprite : RenderNode
    {
        private Point _textureAtlasOffset;

        private Sprite(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int textureAtlasX, int textureAtlasY,
            params Point[] vertexPositions)
            : base(renderDimensionReference, vertexPositions)
        {
            _textureAtlasOffset = new Point(textureAtlasX, textureAtlasY);
            Layer = controlRenderer.SpriteRenderLayer;
        }

        public static Sprite Create(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height,            
            int textureAtlasX, int textureAtlasY)
        {
            return new Sprite
            (
                controlRenderer,
                renderDimensionReference,
                textureAtlasX, textureAtlasY,
                new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }
            );
        }

        public Point TextureAtlasOffset
        {
            get => _textureAtlasOffset;
            set
            {
                if (_textureAtlasOffset == value)
                    return;

                _textureAtlasOffset = value;

                UpdateTextureAtlasOffset();
            }
        }

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (_drawIndex.HasValue && _textureAtlasOffset != null)
                Layer.UpdateTextureAtlasOffset(_drawIndex.Value, this);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            VertexPositions[1].X = X + Width;
            VertexPositions[2].X = X + Width;
            VertexPositions[2].Y = Y + Width;
            VertexPositions[3].Y = Y + Width;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
        }
    }
}
