using System.Drawing;

namespace SilkUI
{
    public abstract class MouseEventArgs : PropagatedEventArgs
    {
        public int X => Position.X;
        public int Y => Position.Y;
        public Point Position { get; }
        public float PreciseX => PrecisePosition.X;
        public float PreciseY => PrecisePosition.Y;
        public PointF PrecisePosition { get; }

        protected MouseEventArgs(float x, float y)
        {
            PrecisePosition = new PointF(x, y);
            Position = PrecisePosition.Approximate();
        }

        internal abstract MouseEventArgs CloneWithOffset(int x, int y);
    }
}