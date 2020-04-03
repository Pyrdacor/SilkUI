using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SilkUI
{
    internal class RenderReference
    {
        public Control Control;
        public int Index;
    }

    public class ControlRenderer
    {
        private IControlRenderer _renderer;
        private Dictionary<int, RenderReference> _lastRenderObjects;
        private readonly List<RenderReference> _currentRenderObjects = new List<RenderReference>();
        private readonly List<Control> _skippedControls = new List<Control>();
        internal bool ForceRedraw { get; set; } = false;

        internal ControlRenderer(IControlRenderer renderer)
        {
            _renderer = renderer;
        }

        internal void Init()
        {
            _lastRenderObjects = _currentRenderObjects.ToDictionary(o => o.Index, o => o);
            _currentRenderObjects.Clear();
            _skippedControls.Clear();
            _renderer.StartRenderCycle();
        }

        internal void Render()
        {
            // Remove no longer existing render objects.
            foreach (var lastRenderObject in _lastRenderObjects)
            {
                if (!_skippedControls.Contains(lastRenderObject.Value.Control))
                    _renderer.RemoveRenderObject(lastRenderObject.Key);
            }

            _renderer.EndRenderCycle();
        }

        internal void SkipControlDrawing(Control control)
        {
            _skippedControls.Add(control);
        }

        private int RunDrawCall(Control control, int? reference, Func<int> drawActionWrapper)
        {
            int renderObjectIndex;

            if (reference == null || !_lastRenderObjects.ContainsKey(reference.Value))
            {
                renderObjectIndex = drawActionWrapper();
            }
            else
            {
                if (ForceRedraw)
                {
                    _renderer.RemoveRenderObject(reference.Value);
                    renderObjectIndex = drawActionWrapper();
                    _currentRenderObjects.Add(new RenderReference() { Control = control, Index = renderObjectIndex });
                    _lastRenderObjects.Remove(reference.Value);
                    return renderObjectIndex;
                }
                else
                    renderObjectIndex = reference.Value;
            }

            _currentRenderObjects.Add(new RenderReference() { Control = control, Index = renderObjectIndex });
            _lastRenderObjects.Remove(renderObjectIndex);
            return renderObjectIndex;
        }

        public void RemoveRenderObject(int renderObjectIndex)
        {
            _renderer.RemoveRenderObject(renderObjectIndex);
        }

        public int DrawRectangle(Control control, int? reference, int x, int y, int width, int height, Color color, int lineSize)
        {
            return RunDrawCall(control, reference, () => _renderer.DrawRectangle(x, y, width, height, color, lineSize));
        }

        public int FillRectangle(Control control, int? reference, int x, int y, int width, int height, Color color)
        {
            return RunDrawCall(control, reference, () => _renderer.FillRectangle(x, y, width, height, color));
        }

        public int DrawRectangleLine(Control control, int? reference, int x, int y, int width, int height, Color color, LineStyle lineStyle)
        {
            return RunDrawCall(control, reference, () => _renderer.DrawRectangleLine(x, y, width, height, color, lineStyle));
        }

        public int DrawImage(Control control, int? reference, int x, int y, Image image, Color? colorOverlay = null)
        {
            return RunDrawCall(control, reference, () => _renderer.DrawImage(x, y, image, colorOverlay));
        }

        public int FillTriangle(Control control, int? reference, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            return RunDrawCall(control, reference, () => _renderer.FillTriangle(x1, y1, x2, y2, x3, y3, color));
        }

        public int FillPolygon(Control control, int? reference, Color color, params Point[] points)
        {
            return RunDrawCall(control, reference, () => _renderer.FillPolygon(color, points));
        }

        public int DrawShadow(Control control, int? reference, int x, int y, int width, int height, Color color, int blurRadius, bool inset)
        {
            return RunDrawCall(control, reference, () => _renderer.DrawShadow(x, y, width, height, color, blurRadius, inset));
        }

        public int DrawText(Control control, int? reference, int x, int y, string text, Font font, Color color)
        {
            return RunDrawCall(control, reference, () => _renderer.DrawText(x, y, text, font, color));
        }
    }
}