namespace SilkUI
{
    public class MouseMoveEventArgs : PropagatedEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public MouseButtons Buttons { get; }
        public float PreciseX { get; }
        public float PreciseY { get; }

        internal MouseMoveEventArgs(float x, float y, MouseButtons buttons)
        {
            X = Util.Round(x);
            Y = Util.Round(y);
            Buttons = buttons;
            PreciseX = x;
            PreciseY = y;
        }
    }

    public delegate void MouseMoveEventHandler(Control sender, MouseMoveEventArgs args);
}