using Silk.NET.Input.Common;

namespace SilkUI
{
    public class MouseButtonEventArgs : MouseEventArgs
    {
        public MouseButton Button { get; }
        public KeyModifiers KeyModifiers { get; }
        public float PreciseX { get; }
        public float PreciseY { get; }

        internal MouseButtonEventArgs(float x, float y, MouseButton button,
            KeyModifiers modifiers = KeyModifiers.None)
            : base(Util.Round(x), Util.Round(y))
        {
            Button = button;
            KeyModifiers = modifiers;
            PreciseX = x;
            PreciseY = y;
        }

        internal override MouseEventArgs CloneWithOffset(int x, int y)
        {
            return new MouseButtonEventArgs(PreciseX + x, PreciseY + y, Button, KeyModifiers);
        }
    }

    public delegate void MouseButtonEventHandler(Control sender, MouseButtonEventArgs args);
}