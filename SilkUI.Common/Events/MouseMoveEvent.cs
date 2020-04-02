namespace SilkUI
{
    public class MouseMoveEventArgs : MouseEventArgs
    {
        public MouseButtons Buttons { get; }
        public float PreciseX { get; }
        public float PreciseY { get; }

        internal MouseMoveEventArgs(float x, float y, MouseButtons buttons)
            : base(x, y)
        {
            Buttons = buttons;
            PreciseX = x;
            PreciseY = y;
        }

        internal override MouseEventArgs CloneWithOffset(int x, int y)
        {
            return new MouseMoveEventArgs(PreciseX + x, PreciseY + y, Buttons);
        }
    }

    public delegate void MouseMoveEventHandler(Control sender, MouseMoveEventArgs args);
}