using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    using Shaders;

    internal class PrimitiveRenderer : IDisposable
    {
        private bool _disposed = false;
        private bool _supportTextures = false;
        private int _numVerticesPerNode = 0;
        private bool _supportBlur = false;
        private bool _ellipse = false;
        private readonly VertexArrayObject _vertexArrayObject = null;
        private readonly PositionBuffer _positionBuffer = null;
        private readonly PositionBuffer _originBuffer = null;
        private readonly PositionBuffer _sizeBuffer = null;
        private readonly PositionBuffer _textureAtlasOffsetBuffer = null;
        private readonly ColorBuffer _colorBuffer = null;
        private readonly ValueBuffer _layerBuffer = null;
        private readonly ValueBuffer _blurRadiusBuffer = null;
        private readonly ValueBuffer _roundnessBuffer = null;
        private readonly IndexBuffer _indexBuffer = null;
        private const uint PrimitiveRestartIndex = uint.MaxValue;

        public static PrimitiveRenderer CreatePolygonRenderer(int numVerticesPerNode, bool transparent)
        {
            return new PrimitiveRenderer(false, numVerticesPerNode, false, false, transparent, PolygonShader.Instance);
        }

        public static PrimitiveRenderer CreateBlurRectRenderer()
        {
            return new PrimitiveRenderer(false, 4, true, false, true, BlurRectShader.Instance);
        }

        public static PrimitiveRenderer CreateBlurEllipseRenderer()
        {
            return new PrimitiveRenderer(false, 4, true, true, true, BlurEllipseShader.Instance);
        }

        public static PrimitiveRenderer CreateEllipseRenderer(bool transparent)
        {
            return new PrimitiveRenderer(false, 4, false, true, transparent, EllipseShader.Instance);
        }

        public static PrimitiveRenderer CreateSpriteRenderer()
        {
            // TODO: support non-atlas textures later?
            return new PrimitiveRenderer(true, 4, false, false, true, TextureAtlasShader.Instance);
        }

        private PrimitiveRenderer(bool supportTextures, int numVerticesPerNode, bool supportBlur,
            bool ellipse, bool supportTransparency, ShaderBase shader)
        {
            _supportTextures = supportTextures;
            _numVerticesPerNode = numVerticesPerNode;
            _supportBlur = supportBlur;
            _ellipse = ellipse;
            SupportTransparency = supportTransparency;
            _vertexArrayObject = new VertexArrayObject(shader);

            if (supportTextures)
            {
                _textureAtlasOffsetBuffer = new PositionBuffer(false);
                _vertexArrayObject.AddBuffer(ShaderBase.TexCoordName, _textureAtlasOffsetBuffer);
            }
            else
            {
                _colorBuffer = new ColorBuffer(true);
                _vertexArrayObject.AddBuffer(ShaderBase.ColorName, _colorBuffer);

                if (ellipse || supportBlur)
                {
                    _originBuffer = new PositionBuffer(true);
                    _vertexArrayObject.AddBuffer(ShaderBase.OriginName, _originBuffer);

                    _sizeBuffer = new PositionBuffer(true);
                    _vertexArrayObject.AddBuffer(ShaderBase.SizeName, _sizeBuffer);
                }

                if (supportBlur)
                {
                    _blurRadiusBuffer = new ValueBuffer(true);
                    _vertexArrayObject.AddBuffer(ShaderBase.BlurRadiusName, _blurRadiusBuffer);
                }

                if (ellipse)
                {
                    _roundnessBuffer = new ValueBuffer(true);
                    _vertexArrayObject.AddBuffer(ShaderBase.RoundnessName, _roundnessBuffer);
                }
            }

            _indexBuffer = new IndexBuffer(_numVerticesPerNode + 1, supportTransparency);
            _positionBuffer = new PositionBuffer(false);
            _layerBuffer = new ValueBuffer(true);

            _vertexArrayObject.AddBuffer("index", _indexBuffer);
            _vertexArrayObject.AddBuffer(ShaderBase.PositionName, _positionBuffer);            
            _vertexArrayObject.AddBuffer(ShaderBase.LayerName, _layerBuffer);
        }

        public ShaderBase Shader => _vertexArrayObject?.Shader;
        public bool SupportTransparency { get; } = false;

        // TODO: do we really need all these IndexOutOfRangeException?
        public int GetDrawIndex(RenderNode renderNode,
            PositionTransformation positionTransformation)
        {
            if (_supportTextures && !(renderNode is Sprite))
                return -1; // would be invisible

            var vertexPositions = renderNode.VertexPositions;

            if (vertexPositions.Length < 3)
                return -1;

            int index = _positionBuffer.Add((short)vertexPositions[0].X, (short)vertexPositions[0].Y);
            int colorBufferIndex = -1;
            int textureAtlasOffsetBufferIndex;
            
            if (_supportTextures)
            {
                var sprite = renderNode as Sprite; // We checked above
                var textureAtlasOffset = sprite.TextureAtlasOffset;
                textureAtlasOffsetBufferIndex = _textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)textureAtlasOffset.Y);

                if (textureAtlasOffsetBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid texture atlas offset buffer index");

                _textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + sprite.Width), (short)textureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                _textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + sprite.Width), (short)(textureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 2);
                _textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)(textureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 3);
            }
            else
            {
                colorBufferIndex = _colorBuffer.Add(renderNode.Color);

                if (colorBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid color buffer index");
            }

            int originBufferIndex = -1;
            int sizeBufferIndex = -1;
            int blurRadiusBufferIndex = -1;
            int roundnessBufferIndex = -1;
            uint blurRadius =  0u;
            uint roundness = 0u;
            short originX = 0;
            short originY = 0;

            if (_supportBlur)
            {
                if (renderNode is Polygon)
                    blurRadius = (renderNode as Polygon).BlurRadius ?? 0u;

                blurRadiusBufferIndex = _blurRadiusBuffer.Add(blurRadius);

                if (blurRadiusBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid blur radius buffer index");
            }

            if (_ellipse)
            {
                if (renderNode is Ellipse)
                    roundness = (uint)(renderNode as Ellipse).Roundness;

                roundnessBufferIndex = _roundnessBuffer.Add(roundness);

                if (roundnessBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid roundness buffer index");
            }

            if (_ellipse || _supportBlur)
            {
                originX = Util.LimitToShort(renderNode.X + renderNode.Width / 2);
                originY = Util.LimitToShort(renderNode.Y + renderNode.Height / 2);
                originBufferIndex = _originBuffer.Add(originX, originY);

                if (originBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid origin buffer index");

                sizeBufferIndex = _sizeBuffer.Add(Util.LimitToShort(renderNode.Width),
                    Util.LimitToShort(renderNode.Height));

                if (sizeBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid size buffer index");
            }

            var layer = renderNode.DisplayLayer;
            int layerBufferIndex = _layerBuffer.Add(layer);

            if (layerBufferIndex != index)
                throw new IndexOutOfRangeException("Invalid layer buffer index");

            for (int i = 1; i < vertexPositions.Length; ++i)
            {
                var position = vertexPositions[i];

                if (positionTransformation != null)
                    position = positionTransformation(position);

                _positionBuffer.Add(Util.LimitToShort(position.X), Util.LimitToShort(position.Y), index + i);
                _layerBuffer.Add(layer, layerBufferIndex + i);
                if (!_supportTextures)
                    _colorBuffer.Add(renderNode.Color, colorBufferIndex + i);
                if (_supportBlur)
                    _blurRadiusBuffer.Add(blurRadius, blurRadiusBufferIndex + i);
                if (_ellipse)
                    _roundnessBuffer.Add(roundness, roundnessBufferIndex + i);
                if (_ellipse || _supportBlur)
                {
                    _originBuffer.Add(originX, originY,
                        originBufferIndex + i);
                    _sizeBuffer.Add(Util.LimitToShort(renderNode.Width), Util.LimitToShort(renderNode.Height),
                        sizeBufferIndex + i);
                }
            }

            int primitiveIndex = index / _numVerticesPerNode;
            int indexBufferOffset = primitiveIndex * (_numVerticesPerNode + 1); // +1 for restart index
            _indexBuffer.InsertPrimitive(indexBufferOffset, PrimitiveRestartIndex);

            return index;
        }

        public void UpdatePosition(int index, RenderNode renderNode,
            PositionTransformation positionTransformation)
        {
            var vertexPositions = renderNode.VertexPositions;

            for (int i = 0; i < vertexPositions.Length; ++i)
            {
                var position = vertexPositions[i];

                if (positionTransformation != null)
                    position = positionTransformation(position);

                _positionBuffer.Update(index + i,
                    Util.LimitToShort(position.X),
                    Util.LimitToShort(position.Y)
                );

                if (_supportBlur)
                {
                    _originBuffer.Update(index + i,
                        Util.LimitToShort(renderNode.X),
                        Util.LimitToShort(renderNode.Y)
                    );
                    _sizeBuffer.Update(index + i,
                        Util.LimitToShort(renderNode.Width),
                        Util.LimitToShort(renderNode.Height)
                    );
                }
            }
        }

        public void UpdateTextureAtlasOffset(int index, Sprite sprite)
        {
            if (_textureAtlasOffsetBuffer == null)
                return;

            short x = Util.LimitToShort(sprite.TextureAtlasOffset.X);
            short y = Util.LimitToShort(sprite.TextureAtlasOffset.Y);
            short width = Util.LimitToShort(sprite.Width);
            short height = Util.LimitToShort(sprite.Height);

            _textureAtlasOffsetBuffer.Update(index, x, y);
            _textureAtlasOffsetBuffer.Update(index + 1, (short)(x + width), y);
            _textureAtlasOffsetBuffer.Update(index + 2, (short)(x + width), (short)(y + height));
            _textureAtlasOffsetBuffer.Update(index + 3, x, (short)(y + height));
        }

        public void UpdateColor(int index, Color color)
        {
            if (_colorBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _colorBuffer.Update(index + i, color);
            }
        }

        public void UpdateDisplayLayer(int index, uint displayLayer)
        {
            if (_layerBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _layerBuffer.Update(index + i, displayLayer);
            }
        }

        public void UpdateBlurRadius(int index, uint blurRadius)
        {
            if (_blurRadiusBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _blurRadiusBuffer.Update(index + i, blurRadius);
            }
        }

        public void UpdateRoundness(int index, uint roundness)
        {
            if (_roundnessBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _roundnessBuffer.Update(index + i, roundness);
            }
        }

        public void FreeDrawIndex(int index)
        {
            for (int i = 0; i < _numVerticesPerNode; ++i)
            {
                _positionBuffer.Update(index + i, short.MaxValue, short.MaxValue); // Ensure it is not visible
                _positionBuffer.Remove(index + i);
            }

            if (_textureAtlasOffsetBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _textureAtlasOffsetBuffer.Remove(index + i);
            }

            if (_colorBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _colorBuffer.Remove(index + i);
            }

            if (_layerBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _layerBuffer.Remove(index + i);
            }

            if (_blurRadiusBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _blurRadiusBuffer.Remove(index + i);
            }

            if (_roundnessBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _roundnessBuffer.Remove(index + i);
            }

            if (_originBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _originBuffer.Remove(index + i);
            }

            if (_sizeBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _sizeBuffer.Remove(index + i);
            }
        }

        public void Render()
        {
            if (_disposed)
                return;

            _vertexArrayObject.Bind();

            unsafe
            {
                _vertexArrayObject.Lock();

                try
                {
                    int numVertices = _positionBuffer.Size / 2;
                    int numPrimitives = numVertices / _numVerticesPerNode;
                    int numIndices = numPrimitives * (_numVerticesPerNode + 1);
                    State.Gl.Enable(GLEnum.PrimitiveRestart);
                    State.Gl.PrimitiveRestartIndex(PrimitiveRestartIndex);
                    State.Gl.DrawElements(PrimitiveType.TriangleFan, (uint)numIndices, DrawElementsType.UnsignedInt, (void*)0);
                    State.Gl.Disable(GLEnum.PrimitiveRestart);
                }
                catch
                {
                    // ignore for now
                }
                finally
                {
                    _vertexArrayObject.Unlock();
                }
            }
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
                    _vertexArrayObject?.Dispose();
                    _positionBuffer?.Dispose();
                    _textureAtlasOffsetBuffer?.Dispose();
                    _colorBuffer?.Dispose();
                    _layerBuffer?.Dispose();
                    _blurRadiusBuffer?.Dispose();
                    _roundnessBuffer?.Dispose();
                    _originBuffer.Dispose();
                    _sizeBuffer?.Dispose();
                    _indexBuffer?.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
