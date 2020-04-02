using System.Linq;
using System.Drawing;

namespace SilkUI.Renderer.OpenGL
{
    internal interface IRenderNode
    {
        void Delete();
    }

    internal abstract class RenderNode : IRenderNode
    {
        protected int? _drawIndex = null;
        private int _x = short.MaxValue;
        private int _y = short.MaxValue;
        private uint _displayLayer = 0;
        private Color _color = Color.White;
        private bool _visible = false;
        private RenderLayer _layer = null;
        private bool _visibleRequest = false;
        private bool _deleted = false;
        private bool _notOnScreen = true;
        private readonly RenderDimensionReference _renderDimensionReference = null;

        protected RenderNode(RenderDimensionReference renderDimensionReference,
            params Point[] vertexPositions)
        {
            // Note: The order of assignments matters!
            _renderDimensionReference = renderDimensionReference;
            var boundingRect = CalculateBoundingRect(vertexPositions);
            Width = boundingRect.Width;
            Height = boundingRect.Height;
            X = boundingRect.X;
            Y = boundingRect.Y;            
            VertexPositions = vertexPositions;            
        }

        private static Rectangle CalculateBoundingRect(params Point[] points)
        {
            var xValues = points.Select(p => p.X);
            var yValues = points.Select(p => p.Y);
            int x = xValues.Min();
            int y = yValues.Min();

            return new Rectangle(
                x, y,
                xValues.Max() - x,
                yValues.Max() - y
            );
        }

        public bool Visible
        {
            get => _visible && !_deleted && !_notOnScreen;
            set
            {
                if (_deleted)
                    return;

                if (_layer == null)
                {
                    _visibleRequest = value;
                    _visible = false;
                    return;
                }

                _visibleRequest = false;

                if (_visible == value)
                    return;

                _visible = value;
                
                if (Visible)
                    AddToLayer();
                else if (!_visible)
                    RemoveFromLayer();
            }
        }

        public uint DisplayLayer
        {
            get => _displayLayer;
            set
            {
                if (_displayLayer == value)
                    return;

                _displayLayer = value;

                UpdateDisplayLayer();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;

                _color = value;
                
                UpdateColor();
            }
        }

        protected RenderLayer Layer
        {
            get => _layer;
            set
            {
                if (_layer == value)
                    return;

                if (_layer != null && Visible)
                    RemoveFromLayer();

                _layer = value;

                if (_layer != null && _visibleRequest && !_deleted)
                {
                    _visible = true;
                    _visibleRequest = false;
                    CheckOnScreen();
                }

                if (_layer == null)
                {
                    _visibleRequest = false;
                    _visible = false;
                    _notOnScreen = true;
                }

                if (_layer != null && Visible)
                    AddToLayer();
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Point[] VertexPositions { get; private set; }

        public virtual void Resize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        protected void AddToLayer()
        {
            _drawIndex = Layer.GetDrawIndex(this);
        }

        protected void RemoveFromLayer()
        {
            if (_drawIndex.HasValue)
            {
                Layer.FreeDrawIndex(_drawIndex.Value);
                _drawIndex = null;
            }
        }

        protected void UpdatePosition()
        {
            if (_drawIndex.HasValue)
                Layer.UpdatePosition(_drawIndex.Value, this);
        }

        protected void UpdateDisplayLayer()
        {
            if (_drawIndex.HasValue)
                Layer.UpdateDisplayLayer(_drawIndex.Value, _displayLayer);
        }

        protected virtual void UpdateColor()
        {
            if (_drawIndex.HasValue)
                Layer.UpdateColor(_drawIndex.Value, _color);
        }

        bool CheckOnScreen()
        {
            bool oldNotOnScreen = _notOnScreen;
            bool oldVisible = Visible;

            _notOnScreen = !_renderDimensionReference.IntersectsWith(new Rectangle(X, Y, Width, Height));

            if (oldNotOnScreen != _notOnScreen)
            {
                if (oldVisible != Visible)
                {
                    if (Visible)
                        AddToLayer();
                    else
                        RemoveFromLayer();

                    return true; // handled
                }
            }

            return false;
        }

        public virtual void Delete()
        {
            if (!_deleted)
            {
                RemoveFromLayer();
                _deleted = true;
                _visible = false;
                _visibleRequest = false;
            }
        }

        public int X
        {
            get => _x;
            set
            {
                if (_x == value)
                    return;

                _x = value;

                if (!_deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                if (_y == value)
                    return;

                _y = value;

                if (!_deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }
    }
}
