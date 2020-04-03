using Silk.NET.Input.Common;

namespace SilkUI
{
    public class MouseButtonEventArgs : MouseEventArgs
    {
        public MouseButton Button { get; }
        public KeyModifiers KeyModifiers { get; }

        internal MouseButtonEventArgs(float x, float y, MouseButton button,
            KeyModifiers modifiers = KeyModifiers.None)
            : base(x, y)
        {
            Button = button;
            KeyModifiers = modifiers;
        }

        internal override MouseEventArgs CloneWithOffset(int x, int y)
        {
            return new MouseButtonEventArgs(PreciseX + x, PreciseY + y, Button, KeyModifiers);
        }
    }

    public delegate void MouseButtonEventHandler(Control sender, MouseButtonEventArgs args);
}