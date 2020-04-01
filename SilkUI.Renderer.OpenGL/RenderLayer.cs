using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    public delegate Point PositionTransformation(Point position);

    internal class RenderLayer : IDisposable
    {
        private bool _disposed = false;
        private Texture _texture = null;
        private bool _blur = false;
        private readonly PrimitiveRenderer _primitiveRenderer = null;
        private readonly RenderDimensionReference _renderDimensionReference = null;

        public Color? ColorKey
        {
            get;
            set;
        } = null;

        public Color? ColorOverlay
        {
            get;
            set;
        } = null;

        public bool Visible
        {
            get;
            set;
        } = true;

        public PositionTransformation PositionTransformation
        {
            get;
            set;
        } = null;

        public float Z
        {
            get;
            set;
        } = 0.0f;

        public RenderLayer(Texture texture, int numVerticesPerNode, bool supportBlur,
            RenderDimensionReference renderDimensionReference, Color? colorKey = null, Color? colorOverlay = null)
        {
            _renderDimensionReference = renderDimensionReference;
            _primitiveRenderer = new PrimitiveRenderer(texture != null, numVerticesPerNode, supportBlur);
            _texture = texture;
            _blur = supportBlur;
            ColorKey = colorKey;
            ColorOverlay = colorOverlay;
        }

        public bool SupportZoom => false; // TODO

        public void Render()
        {
            if (!Visible)
                return;

            if (_texture != null)
            {
                var textureShader = TextureShader.Instance;

                textureShader.UpdateMatrices(SupportZoom);
                textureShader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                State.Gl.ActiveTexture(GLEnum.Texture0);
                _texture.Bind();

                textureShader.SetAtlasSize((uint)_texture.Width, (uint)_texture.Height);
                textureShader.SetZ(Z);

                if (ColorKey == null)
                    textureShader.SetColorKey(1.0f, 0.0f, 1.0f);
                else
                    textureShader.SetColorKey(ColorKey.Value.R / 255.0f, ColorKey.Value.G / 255.0f, ColorKey.Value.B / 255.0f);

                if (ColorOverlay == null)
                    textureShader.SetColorOverlay(1.0f, 1.0f, 1.0f, 1.0f);
                else
                    textureShader.SetColorOverlay(ColorOverlay.Value.R / 255.0f, ColorOverlay.Value.G / 255.0f, ColorOverlay.Value.B / 255.0f, ColorOverlay.Value.A / 255.0f);

                // TODO: blur
            }
            else
            {
                ColorShader colorShader = _blur ? BlurColorShader.Instance : ColorShader.Instance;

                colorShader.UpdateMatrices(SupportZoom);
                colorShader.SetZ(Z);

                if (_blur)
                {
                    (colorShader as BlurColorShader).SetScreenHeight((uint)_renderDimensionReference.Height);
                }
            }
            
            _primitiveRenderer.Render();
        }

        public int GetDrawIndex(RenderNode renderNode)
        {
            return _primitiveRenderer.GetDrawIndex(renderNode, PositionTransformation);
        }

        public int GetDrawIndex(Shape shape)
        {
            return _primitiveRenderer.GetDrawIndex(shape, PositionTransformation);
        }

        public void FreeDrawIndex(int index)
        {
            _primitiveRenderer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, RenderNode renderNode)
        {
            _primitiveRenderer.UpdatePosition(index, renderNode, PositionTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, Sprite sprite)
        {
            _primitiveRenderer.UpdateTextureAtlasOffset(index, sprite);
        }

        public void UpdateDisplayLayer(int index, uint displayLayer)
        {
            _primitiveRenderer.UpdateDisplayLayer(index, displayLayer);
        }

        public void UpdateBlurRadius(int index, uint blurRadius)
        {
            _primitiveRenderer.UpdateBlurRadius(index, blurRadius);
        }

        public void UpdateRoundness(int index, uint roundness)
        {
            _primitiveRenderer.UpdateRoundness(index, roundness);
        }

        public void UpdateColor(int index, Color color)
        {
            _primitiveRenderer.UpdateColor(index, color);
        }
        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _primitiveRenderer?.Dispose();
                    _texture?.Dispose();
                    Visible = false;
                    _disposed = true;
                }
            }
        }
    }
}
