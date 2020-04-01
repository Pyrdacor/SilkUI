using Silk.NET.Input.Common;

namespace SilkUI
{
    public class MouseButtonEventArgs : PropagatedEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public MouseButton Button { get; }
        public KeyModifiers KeyModifiers { get; }
        public float PreciseX { get; }
        public float PreciseY { get; }

        internal MouseButtonEventArgs(float x, float y, MouseButton button,
            KeyModifiers modifiers = KeyModifiers.None)
        {
            X = Util.Round(x);
            Y = Util.Round(y);
            Button = button;
            KeyModifiers = modifiers;
            PreciseX = x;
            PreciseY = y;
        }
    }

    public delegate void MouseButtonEventHandler(Control sender, MouseButtonEventArgs args);
}