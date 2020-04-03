using System.Drawing;

namespace SilkUI
{
    public abstract class RootComponent : Component
    {
        private Control _hoveredControl = null;
        private Control _focusedControl = null;
        private ControlRenderer _controlRenderer;
        private InputEventManager _inputEventManager;

        internal override ControlRenderer ControlRenderer => _controlRenderer;
        internal override InputEventManager InputEventManager => _inputEventManager;        

        internal void SetControlRenderer(IControlRenderer controlRenderer)
        {
            _controlRenderer = new ControlRenderer(controlRenderer);
        }

        internal void SetInputEventManager(InputEventManager inputEventManager)
        {
            _inputEventManager = inputEventManager;
            _inputEventManager.GlobalMouseMove += UpdateHovering;
            _inputEventManager.GlobalMouseDown += UpdateFocus;
        }

        private void UpdateHovering(Point position)
        {
            var child = GetChildAtAbsolutePosition(position, true, true);

            if (child == _hoveredControl)
                return;

            if (_hoveredControl != null)
                _hoveredControl.Hovered = false;

            _hoveredControl = child;

            if (_hoveredControl != null)
                _hoveredControl.Hovered = true;
        }

        private void UpdateFocus(Point position)
        {
            var child = GetChildAtAbsolutePosition(position, true, false);

            if (child == _focusedControl)
                return;

            if (_focusedControl != null)
                _focusedControl.Focused = false;

            _focusedControl = child;

            if (_focusedControl != null)
                _focusedControl.Focused = true;
        }
    }
}