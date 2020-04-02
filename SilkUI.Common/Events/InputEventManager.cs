using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Silk.NET.Input.Common;

namespace SilkUI
{
    // TODO: touch gestures, etc

    internal class InputEventManager
    {
        private IInputContext _inputContext;
        private readonly List<Control> _registeredControls = new List<Control>();
        private MouseButtons _currentMouseButtons = MouseButtons.None;
        private KeyModifiers _currentModifiers = KeyModifiers.None;
        private readonly Dictionary<MouseButton, PointF> _lastMouseDownPositions = new Dictionary<MouseButton, PointF>();

        public InputEventManager(IInputContext inputContext)
        {
            _inputContext = inputContext;

            InitMouseEvents();
            InitKeyEvents();
        }

        private void InitMouseEvents()
        {
            // TODO: mouse wheel

            var mouse = _inputContext.Mice.FirstOrDefault();

            if (mouse != null)
            {
                mouse.MouseMove += (mouse, point) =>
                    RunMouseHandlers((c, args) => c.OnMouseMove(args),
                        new MouseMoveEventArgs(point.X, point.Y, _currentMouseButtons)
                    );
                mouse.MouseDown += (mouse, button) =>
                {
                    _lastMouseDownPositions[button] = mouse.Position;
                    RunMouseHandlers((c, args) => c.OnMouseDown(args),
                        new MouseButtonEventArgs(mouse.Position.X, mouse.Position.Y, button, _currentModifiers)
                    );
                };
                mouse.MouseUp += (mouse, button) =>
                    RunMouseHandlers((c, args) => c.OnMouseUp(args),
                        new MouseButtonEventArgs(mouse.Position.X, mouse.Position.Y, button, _currentModifiers)
                    );
                mouse.Click += (mouse, button) =>
                    RunMouseHandlers((c, args) => c.OnMouseClick(args),
                        new MouseButtonEventArgs(_lastMouseDownPositions[button].X,
                        _lastMouseDownPositions[button].Y, button, _currentModifiers)
                    );
                mouse.DoubleClick += (mouse, button) =>
                    RunMouseHandlers((c, args) => c.OnMouseDoubleClick(args),
                        new MouseButtonEventArgs(_lastMouseDownPositions[button].X,
                        _lastMouseDownPositions[button].Y, button, _currentModifiers)
                    );
            }
        }

        private void InitKeyEvents()
        {
            // TODO: keychar?

            var keyboard = _inputContext.Keyboards.FirstOrDefault();

            if (keyboard != null)
            {
                keyboard.KeyDown += (keyboard, key, modifiers) =>
                {
                    // We only support shift, control and alt.
                    _currentModifiers = (KeyModifiers)(modifiers & 0x07);
                    RunHandlers((c, args) => c.OnKeyDown(args), new KeyEventArgs(key, _currentModifiers));
                };
                keyboard.KeyUp += (keyboard, key, modifiers) =>
                {
                    // We only support shift, control and alt.
                    _currentModifiers = (KeyModifiers)(modifiers & 0x07);
                    RunHandlers((c, args) => c.OnKeyUp(args), new KeyEventArgs(key, _currentModifiers));
                };
            }
        }

        private void RunMouseHandlers<T>(Action<Control, T> handler, T args) where T : MouseEventArgs
        {
            // In general later created controls will be
            // registered later so childs come later in
            // the list of register controls.
            // As childs should process input events first
            // we reverse the collection order here.
            for (int i = _registeredControls.Count - 1; i >= 0; --i)
            {
                if (args.CancelPropagation)
                    return;

                var absoluteRect = _registeredControls[i].AbsoluteRectangle;

                if (absoluteRect.Contains(args.X, args.Y))
                {
                    handler(_registeredControls[i], (T)args.CloneWithOffset
                    (
                        -absoluteRect.X, -absoluteRect.Y
                    ));
                }
            }
        }

        private void RunHandlers<T>(Action<Control, T> handler, T args) where T : PropagatedEventArgs
        {
            // In general later created controls will be
            // registered later so childs come later in
            // the list of register controls.
            // As childs should process input events first
            // we reverse the collection order here.
            for (int i = _registeredControls.Count - 1; i >= 0; --i)
            {
                if (args.CancelPropagation)
                    return;

                handler(_registeredControls[i], args);
            }
        }

        public void RegisterControl(Control control)
        {
            _registeredControls.Add(control);
        }

        public void UnregisterControl(Control control)
        {
            _registeredControls.Remove(control);
        }
    }
}