using System;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    /// <summary>
    /// A shape is a colored polygon.
    /// </summary>
    internal class Shape : RenderNode
    {
        protected Shape(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            params Point[] vertexPositions)
            : base(renderDimensionReference, vertexPositions)
        {
            Layer = controlRenderer.GetRenderLayer(vertexPositions.Length);
        }

        protected Shape(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            RenderLayer layer, params Point[] vertexPositions)
            : base(renderDimensionReference, vertexPositions)
        {
            Layer = layer;
        }

        public static Shape CreateTriangle(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            Point p1, Point p2, Point p3)
        {
            return new Shape(controlRenderer, renderDimensionReference, p1, p2, p3);
        }

        public static Shape CreateRect(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height)
        {
            return new Shape(controlRenderer, renderDimensionReference,
                new Point[4]
                {
                    new Point(x, y),
                    new Point(x + width, y),
                    new Point(x + width, y + height),
                    new Point(x, y + height)
                }
            );
        }

        public static Shape CreateEllipse(ControlRenderer controlRenderer,
        RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height)
        {
            // TODO: 1 center, 64 outline (drawn as triangle fan)
            return null;
        }

        public static Shape CreateRoundRect(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference,
            int x, int y, int width, int height, float roundness)
        {
            // TODO: 1 center, 7 for each corner (drawn as triangle fan)
            return null;
        }

        public static Shape CreatePolygon(ControlRenderer controlRenderer,
            RenderDimensionReference renderDimensionReference, params Point[] points)
        {
            if (points.Length < 3)
                throw new ArgumentException("A polygon must consist of at least 3 points.");

            if (points.Length == 3)
            {
                return CreateTriangle(controlRenderer, renderDimensionReference,
                    points[0], points[1], points[2]);
            }

            return new Shape(controlRenderer, renderDimensionReference, points);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            for (int i = 0; i < VertexPositions.Length; ++i)
            {
                int newX = VertexPositions[i].X;
                int newY = VertexPositions[i].Y;

                if (VertexPositions[i].X != X)
                {
                    float ratio = (float)VertexPositions[i].X / Width;
                    newX = X + Util.Round(ratio * width);
                }
                if (VertexPositions[i].Y != Y)
                {
                    float ratio = (float)VertexPositions[i].Y / Height;
                    newY = Y + Util.Round(ratio * height);
                }

                VertexPositions[i] = new Point(newX, newY);
            }

            base.Resize(width, height);

            UpdatePosition();
        }
    }
}
