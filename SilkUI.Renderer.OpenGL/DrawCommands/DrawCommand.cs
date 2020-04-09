using System;
using System.Drawing;
using SilkUI.Renderer.OpenGL.Shaders;

namespace SilkUI.Renderer.OpenGL
{
    internal enum DrawCommandState
    {
        New,
        Active,
        Removed,
        Replaced
    }

    internal abstract class DrawCommand : IComparable<DrawCommand>
    {
        public DrawCommandState State { get; internal set; } = DrawCommandState.New;
        public VertexArrayObject VertexArrayObject { get; internal set; }
        public int BufferIndex { get; internal set; } = -1;
        public Point[] VertexPositions { get; protected set; }
        public uint Z { get; private set; }
        public Color Color { get; private set; }
        public bool Transparency { get; protected set; }
        public uint Roundness { get; protected set; } // 0: Rect, 2: Ellipse, 4, 8 or 16: Round rect
        public uint BlurRadius { get; protected set; }
        public ShaderBase Shader { get; }
        public Texture Texture { get; protected set; }
        public Point[] TexCoords { get; protected set; }
        public Rectangle? ClipRect { get; protected set; }

        public DrawCommand(Point[] vertexPositions, uint z, Color color, bool transparency,
            uint roundness, uint blurRadius, ShaderBase shader, Texture texture, Point[] texCoords,
            Rectangle? clipRect = null)
        {
            if (vertexPositions == null)
                throw new ArgumentNullException("Vertex positions must not be null.");

            if (shader == null)
                throw new ArgumentNullException("Shader must not be null.");

            if (roundness != 0u && roundness != 2u && roundness != 4u && roundness != 8u && roundness != 16u)
                throw new ArgumentException("The given roundness must be one of: 0, 2, 4, 8, 16.");

            if (texture != null)
            {
                if (vertexPositions.Length != 4)
                    throw new ArgumentException("Textures can only be used with rectangular polygons.");

                if (texCoords == null)
                    throw new ArgumentNullException("Tex coords must not be null if the texture is set.");

                if (texCoords.Length != vertexPositions.Length)
                    throw new ArgumentException("Tex coord amount must match the amount of vertices.");

                if (blurRadius != 0u)
                    throw new ArgumentException("Blur is not available for textured sprites.");
            }

            if (blurRadius != 0u && !transparency)
                throw new ArgumentException("If blur is active, transparency must be set as well.");

            VertexPositions = vertexPositions;
            Z = z;
            Color = color;
            Transparency = transparency;
            Roundness = roundness;
            BlurRadius = blurRadius;
            Shader = shader;
            Texture = texture;
            TexCoords = texCoords;
            ClipRect = clipRect;
        }

        /// <summary>
        /// This is used by the transparency z-order sorting.
        /// 
        /// Therefore highest z-values (which are in the back) come
        /// first as they need to be drawn before front ones.
        /// </summary>
        public int CompareTo(DrawCommand other)
        {
            if (other == null)
                return 1;

            if (Z == other.Z)
                return BufferIndex.CompareTo(other.BufferIndex);

            // This is equal to the greater check.
            return other.Z.CompareTo(Z);
        }

        public void RemoveFromBuffers()
        {
            var positionBuffer = VertexArrayObject.GetPositionBuffer(ShaderBase.PositionName);
            var colorBuffer = VertexArrayObject.GetColorBuffer(ShaderBase.ColorName);

            for (int i = 0; i < VertexPositions.Length; ++i)
            {
                // Set the position outside of the screen.
                positionBuffer.Add(BufferIndex + i, short.MaxValue, short.MaxValue);
                // Set alpha to 0 to make it invisible (discard by alpha test immediately).
                colorBuffer.Add(BufferIndex + i, Color.Transparent);
            }

            VertexArrayObject.FreeChunk(BufferIndex, VertexPositions.Length);

            VertexArrayObject = null;
            BufferIndex = -1;
        }

        public void Offset(int x, int y)
        {
            for (int i = 0; i < VertexPositions.Length; ++i)
                VertexPositions[i].Offset(x, y);
        }
    }
}
