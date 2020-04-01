using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class PrimitiveRenderer : IDisposable
    {
        private bool _disposed = false;
        private bool _supportTextures = false;
        private int _numVerticesPerNode = 0;
        private bool _supportBlur = false;
        private readonly VertexArrayObject _vertexArrayObject = null;
        private readonly PositionBuffer _positionBuffer = null;
        private readonly PositionBuffer _offsetBuffer = null;
        private readonly PositionBuffer _sizeBuffer = null;
        private readonly PositionBuffer _textureAtlasOffsetBuffer = null;
        private readonly ColorBuffer _colorBuffer = null;
        private readonly ValueBuffer _layerBuffer = null;
        private readonly ValueBuffer _blurRadiusBuffer = null;
        private readonly ValueBuffer _roundnessBuffer = null;
        private readonly IndexBuffer _indexBuffer = null;
        private const uint PrimitiveRestartIndex = uint.MaxValue;

        public PrimitiveRenderer(bool supportTextures, int numVerticesPerNode, bool supportBlur)
        {
            _supportTextures = supportTextures;
            _numVerticesPerNode = numVerticesPerNode;
            _supportBlur = supportBlur;

            if (supportTextures)
            {
                _vertexArrayObject = new VertexArrayObject(TextureShader.Instance.ShaderProgram);

                _textureAtlasOffsetBuffer = new PositionBuffer(false);
                _vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, _textureAtlasOffsetBuffer);

                if (supportBlur)
                {
                    // TODO: add blur texture buffer
                }
            }
            else
            {
                _vertexArrayObject = new VertexArrayObject
                (
                    supportBlur
                        ? BlurColorShader.Instance.ShaderProgram
                        : ColorShader.Instance.ShaderProgram
                );

                _colorBuffer = new ColorBuffer(true);
                _vertexArrayObject.AddBuffer(ColorShader.DefaultColorName, _colorBuffer);

                if (supportBlur)
                {
                    _offsetBuffer = new PositionBuffer(true);
                    _vertexArrayObject.AddBuffer(BlurColorShader.DefaultOffsetName, _offsetBuffer);

                    _sizeBuffer = new PositionBuffer(true);
                    _vertexArrayObject.AddBuffer(BlurColorShader.DefaultSizeName, _sizeBuffer);

                    _blurRadiusBuffer = new ValueBuffer(true);
                    _vertexArrayObject.AddBuffer(BlurColorShader.DefaultBlurRadiusName, _blurRadiusBuffer);

                    _roundnessBuffer = new ValueBuffer(true);
                    _vertexArrayObject.AddBuffer(BlurColorShader.DefaultRoundnessName, _roundnessBuffer);
                }
            }

            _indexBuffer = new IndexBuffer(_numVerticesPerNode + 1);
            _positionBuffer = new PositionBuffer(false);
            _layerBuffer = new ValueBuffer(true);

            _vertexArrayObject.AddBuffer("index", _indexBuffer);
            _vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, _positionBuffer);            
            _vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, _layerBuffer);
        }

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

            int offsetBufferIndex = -1;
            int sizeBufferIndex = -1;
            int blurRadiusBufferIndex = -1;
            int roundnessBufferIndex = -1;
            uint blurRadius = (renderNode is Shadow) ? (renderNode as Shadow).BlurRadius : 0u;
            uint roundness = (renderNode is Shadow) ? (renderNode as Shadow).Roundness : 0u;

            if (_supportBlur)
            {
                offsetBufferIndex = _offsetBuffer.Add(Util.LimitToShort(renderNode.X),
                    Util.LimitToShort(renderNode.Y));

                if (offsetBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid offset buffer index");

                sizeBufferIndex = _sizeBuffer.Add(Util.LimitToShort(renderNode.Width),
                    Util.LimitToShort(renderNode.Height));

                if (sizeBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid size buffer index");

                blurRadiusBufferIndex = _blurRadiusBuffer.Add(blurRadius);

                if (blurRadiusBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid blur radius buffer index");

                roundnessBufferIndex = _roundnessBuffer.Add(roundness);

                if (roundnessBufferIndex != index)
                    throw new IndexOutOfRangeException("Invalid roundness buffer index");
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
                if (!_supportTextures)
                    _colorBuffer.Add(renderNode.Color, colorBufferIndex + i);
                if (_supportBlur)
                {
                    _offsetBuffer.Add(Util.LimitToShort(renderNode.X), Util.LimitToShort(renderNode.Y),
                        offsetBufferIndex + i);
                    _sizeBuffer.Add(Util.LimitToShort(renderNode.Width), Util.LimitToShort(renderNode.Height),
                        sizeBufferIndex + i);
                    _blurRadiusBuffer.Add(blurRadius, blurRadiusBufferIndex + i);
                    _roundnessBuffer.Add(roundness, roundnessBufferIndex + i);
                }
                _layerBuffer.Add(layer, layerBufferIndex + i);
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
                    _offsetBuffer.Update(index + i,
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

            if (_offsetBuffer != null)
            {
                for (int i = 0; i < _numVerticesPerNode; ++i)
                    _offsetBuffer.Remove(index + i);
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
                    _offsetBuffer.Dispose();
                    _sizeBuffer?.Dispose();
                    _indexBuffer?.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
