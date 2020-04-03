namespace SilkUI
{
    public class MouseMoveEventArgs : MouseEventArgs
    {
        public MouseButtons Buttons { get; }

        internal MouseMoveEventArgs(float x, float y, MouseButtons buttons)
            : base(x, y)
        {
            Buttons = buttons;
        }

        internal override MouseEventArgs CloneWithOffset(int x, int y)
        {
            return new MouseMoveEventArgs(PreciseX + x, PreciseY + y, Buttons);
        }
    }

    public delegate void MouseMoveEventHandler(Control sender, MouseMoveEventArgs args);
}