using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using Shaders;

    public delegate Point PositionTransformation(Point position);

    internal class RenderLayer : IDisposable
    {
        private bool _disposed = false;
        private Texture _texture = null;
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

        public bool SupportZoom => false; // TODO

        public bool SupportTransparency => _primitiveRenderer.SupportTransparency;

        public RenderLayer(Texture texture, PrimitiveRenderer primitiveRenderer,
            RenderDimensionReference renderDimensionReference, Color? colorKey = null, Color? colorOverlay = null)
        {
            _renderDimensionReference = renderDimensionReference;
            _primitiveRenderer = primitiveRenderer;
            _texture = texture;
            ColorKey = colorKey;
            ColorOverlay = colorOverlay;
        }

        public void Render()
        {
            if (!Visible)
                return;

            if (_texture != null)
            {
                var textureShader = _primitiveRenderer.Shader as TextureAtlasShader; // TODO: later support TextureShader as well?

                textureShader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                State.Gl.ActiveTexture(GLEnum.Texture0);
                _texture.Bind();

                textureShader.SetAtlasSize((uint)_texture.Width, (uint)_texture.Height);

                if (ColorKey == null)
                    textureShader.SetColorKey(1.0f, 0.0f, 1.0f);
                else
                    textureShader.SetColorKey(ColorKey.Value.R / 255.0f, ColorKey.Value.G / 255.0f, ColorKey.Value.B / 255.0f);

                if (ColorOverlay == null)
                    textureShader.SetColorOverlay(1.0f, 1.0f, 1.0f, 1.0f);
                else
                    textureShader.SetColorOverlay(ColorOverlay.Value.R / 255.0f, ColorOverlay.Value.G / 255.0f, ColorOverlay.Value.B / 255.0f, ColorOverlay.Value.A / 255.0f);
            }
            else if (_primitiveRenderer.Shader is IShaderWithScreenHeight)
            {
                (_primitiveRenderer.Shader as IShaderWithScreenHeight).SetScreenHeight((uint)_renderDimensionReference.Height);
            }

            _primitiveRenderer.Shader.UpdateMatrices(SupportZoom);
            _primitiveRenderer.Shader.SetZ(Z);


            _primitiveRenderer.Render();
        }

        public int GetDrawIndex(RenderNode renderNode)
        {
            return _primitiveRenderer.GetDrawIndex(renderNode, PositionTransformation);
        }

        public int GetDrawIndex(Polygon shape)
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
