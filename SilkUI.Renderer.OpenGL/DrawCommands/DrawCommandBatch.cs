using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Silk.NET.OpenGL;
using SilkUI.Renderer.OpenGL.Shaders;

namespace SilkUI.Renderer.OpenGL
{
    internal class DrawCommandBatch
    {
        private class BatchKey : IEquatable<BatchKey>, IComparable<BatchKey>
        {
            private int _index = -1;
            private readonly ShaderBase _shader;
            private readonly bool _transparency;
            private readonly Texture _texture;

            public BatchKey(ShaderBase shader, bool transparency, Texture texture)
            {
                _shader = shader;
                _transparency = transparency;
                _texture = texture;
            }

            public BatchKey WithIndex(int index)
            {
                return new BatchKey(_shader, _transparency, _texture)
                {
                    _index = index,
                };
            }

            public int CompareTo(BatchKey other)
            {
                if (Object.ReferenceEquals(other, null))
                    return 1;

                // Draw transparent objects later.
                if (_transparency && !other._transparency)
                    return 1;
                if (!_transparency && other._transparency)
                    return -1;

                if (_shader != other._shader)
                    return _shader.GetType().Name.CompareTo(other._shader.GetType().Name);

                if (_texture != other._texture)
                {
                    if (_texture == null && other._texture != null)
                        return 1;
                    if (_texture != null && other._texture == null)
                        return -1;

                    return _texture.Index.CompareTo(other._texture.Index);
                }

                if (_index == -1 || other._index == -1)
                    return 0;

                return _index.CompareTo(other._index);
            }

            public bool Equals(BatchKey other)
            {
                return CompareTo(other) == 0;
            }

            public override bool Equals(object obj)
            {
                if (Object.ReferenceEquals(obj, null))
                    return false;

                return this.Equals((BatchKey)obj);
            }

            public override int GetHashCode()
            {
                int hash = 17;

                if (_shader != null)
                    hash = hash * 23 + _shader.GetHashCode();
                hash = hash * 23 + _transparency.GetHashCode();

                return hash;
            }

            public static bool operator ==(BatchKey lhs, BatchKey rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(BatchKey lhs, BatchKey rhs)
            {
                return !(lhs == rhs);
            }
        }

        private static BatchKey CreateBatchKey(DrawCommand command)
        {
            return new BatchKey(command.Shader, command.Transparency, command.Texture);
        }

        private readonly SortedDictionary<BatchKey, VertexArrayObject> _vertexArrayObjects =
            new SortedDictionary<BatchKey, VertexArrayObject>();
        private readonly Dictionary<BatchKey, SortedSet<DrawCommand>> _transparencyVaoDrawCommands =
            new Dictionary<BatchKey, SortedSet<DrawCommand>>();

        public void UpdateBatch(IEnumerable<DrawCommand> commands)
        {
            foreach (var command in commands)
            {
                var key = CreateBatchKey(command);

                if (!_vertexArrayObjects.ContainsKey(key))
                {
                    _vertexArrayObjects.Add(key.WithIndex(_vertexArrayObjects.Count),
                        new VertexArrayObject(command.Shader));
                }

                var vao = _vertexArrayObjects[key];

                // Is the command already added to the vao?
                if (command.VertexArrayObject != null)
                {
                    switch (command.State)
                    {
                        case DrawCommandState.New:
                        case DrawCommandState.Replaced:
                            throw new InvalidOperationException($"A draw command marked as '{command.State}' can not be attached to a VAO already.");
                        case DrawCommandState.Removed:
                            command.RemoveFromBuffers();
                            if (_transparencyVaoDrawCommands.ContainsKey(key))
                                _transparencyVaoDrawCommands[key].Remove(command);
                            continue;
                        case DrawCommandState.Active:
                            continue;
                         default:
                            throw new InvalidOperationException($"Unknown draw command state: {command.State}");
                    }                    
                }
                else if (command.State != DrawCommandState.New && command.State != DrawCommandState.Replaced)
                {
                    throw new InvalidOperationException("A draw command without a VAO must be marked as 'New' or 'Replaced'.");
                }

                if (command.State == DrawCommandState.New) // Only get a new index for new draw commands.
                    command.BufferIndex = vao.GetBufferIndex(command.VertexPositions.Length);

                command.VertexArrayObject = vao;
                command.State = DrawCommandState.Active;
                vao.Texture = command.Texture;

                // Everything has vertices.
                var positionBuffer = vao.EnsurePositionBuffer(ShaderBase.PositionName, false);
                for (int i = 0; i < command.VertexPositions.Length; ++i)
                {
                    positionBuffer.Add(command.BufferIndex + i,
                        Util.LimitToShort(command.VertexPositions[i].X), Util.LimitToShort(command.VertexPositions[i].Y));
                }
                // And a z-value / layer.
                var layerBuffer = vao.EnsureValueBuffer(ShaderBase.LayerName, false);
                // And a color.
                var colorBuffer = vao.EnsureColorBuffer(ShaderBase.ColorName, false);
                for (int i = 0; i < command.VertexPositions.Length; ++i)
                {
                    layerBuffer.Add(command.BufferIndex + i, command.Z);
                    colorBuffer.Add(command.BufferIndex + i, command.Color);
                }

                if (command.Roundness != 0 || command.BlurRadius != 0)
                {
                    // Ellipse / round rect or blurred polygon
                    var xValues = command.VertexPositions.Select(p => p.X);
                    var yValues = command.VertexPositions.Select(p => p.Y);
                    var x = xValues.Min();
                    var y = yValues.Min();
                    var width = Util.LimitToShort(xValues.Max() - x);
                    var height = Util.LimitToShort(yValues.Max() - y);
                    var originX = Util.LimitToShort(x + width / 2);
                    var originY = Util.LimitToShort(y + height / 2);
                    var originBuffer = vao.EnsurePositionBuffer(ShaderBase.OriginName, false);
                    var sizeBuffer = vao.EnsurePositionBuffer(ShaderBase.SizeName, true);
                    for (int i = 0; i < command.VertexPositions.Length; ++i)
                    {
                        originBuffer.Add(command.BufferIndex + i, originX, originY);
                        sizeBuffer.Add(command.BufferIndex + i, width, height);
                    }
                }

                if (command.Roundness != 0)
                {
                    // Ellipse / round rect
                    var roundnessBuffer = vao.EnsureValueBuffer(ShaderBase.RoundnessName, true);
                    for (int i = 0; i < command.VertexPositions.Length; ++i)
                        roundnessBuffer.Add(command.BufferIndex + i, command.Roundness);
                }

                if (command.Texture != null)
                {
                    // Textured sprite
                    var textureCoordBuffer = vao.EnsurePositionBuffer(ShaderBase.TexCoordName, true);
                    for (int i = 0; i < command.TexCoords.Length; ++i)
                    {
                        textureCoordBuffer.Add(command.BufferIndex + i,
                            Util.LimitToShort(command.TexCoords[i].X), Util.LimitToShort(command.TexCoords[i].Y));
                    }

                    var clipPositionXBuffer = vao.EnsureValueBuffer(ShaderBase.ClipRectXName, true);
                    for (int i = 0; i < command.TexCoords.Length; ++i)
                    {
                        if (command.ClipRect == null)
                        {
                            clipPositionXBuffer.Add(command.BufferIndex + i, uint.MaxValue);
                        }
                        else
                        {
                            clipPositionXBuffer.Add(command.BufferIndex + i, (uint)command.ClipRect.Value.X);
                            vao.EnsureValueBuffer(ShaderBase.ClipRectYName, true)
                                .Add(command.BufferIndex + i, (uint)command.ClipRect.Value.Y);
                            vao.EnsurePositionBuffer(ShaderBase.ClipRectSizeName, true)
                                .Add(command.BufferIndex + i, Util.LimitToShort(command.ClipRect.Value.Width),
                                    Util.LimitToShort(command.ClipRect.Value.Height));
                        }
                    }
                }

                if (command.Transparency)
                {
                    vao.DepthTest = true;
                    vao.DepthWrite = false;
                    vao.Blending = true;

                    if (command.BlurRadius != 0u)
                    {
                        // Blurred
                        var blurRadiusBuffer = vao.EnsureValueBuffer(ShaderBase.BlurRadiusName, true);
                        for (int i = 0; i < command.VertexPositions.Length; ++i)
                            blurRadiusBuffer.Add(command.BufferIndex + i, command.BlurRadius);
                    }

                    if (!_transparencyVaoDrawCommands.ContainsKey(key))
                        _transparencyVaoDrawCommands.Add(key, new SortedSet<DrawCommand>());

                    _transparencyVaoDrawCommands[key].Add(command);
                }
                else // Opaque (alpha test / color key is possible though)
                {
                    vao.DepthTest = true;
                    vao.DepthWrite = true;
                    vao.Blending = false;

                    // No sorting for index buffer needed.
                    vao.IndexBuffer.AddPrimitive(command.VertexPositions.Length, command.BufferIndex);
                }
            }

            // Create indices from sorted transparency draw calls.
            foreach (var transparencyVaoDrawCommand in _transparencyVaoDrawCommands)
            {
                var vao = _vertexArrayObjects[transparencyVaoDrawCommand.Key];

                vao.IndexBuffer.Clear();

                foreach (var command in transparencyVaoDrawCommand.Value)
                    vao.IndexBuffer.AddPrimitive(command.VertexPositions.Length, command.BufferIndex);
            }
        }

        public void Render(RenderDimensionReference renderDimensionReference, Color? colorKey = null)
        {
            State.Gl.Enable(GLEnum.PrimitiveRestart);
            State.Gl.PrimitiveRestartIndex(IndexBuffer.PrimitiveRestartIndex);

            foreach (var vertexArrayObject in _vertexArrayObjects)
            {
                var vao = vertexArrayObject.Value;

                vao.Bind();

                unsafe
                {
                    vao.Lock();

                    try
                    {
                        if (vao.Shader is IShaderWithScreenHeight shader)
                            shader.SetScreenHeight((uint)renderDimensionReference.Height);
                        vao.Shader.UpdateMatrices(false); // TODO: later support zoom?
                        vao.ColorKey = colorKey;
                        State.Gl.DrawElements(PrimitiveType.TriangleFan,
                            (uint)vao.IndexBuffer.Size, DrawElementsType.UnsignedInt, (void*)0);
                    }
                    catch
                    {
                        // ignore for now
                    }
                    finally
                    {
                        vao.Unlock();
                    }
                }
            }

            State.Gl.Disable(GLEnum.PrimitiveRestart);
        }
    }
}
