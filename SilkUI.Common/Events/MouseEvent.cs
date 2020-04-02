namespace SilkUI
{
    public abstract class MouseEventArgs : PropagatedEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public float PreciseX { get; }
        public float PreciseY { get; }

        protected MouseEventArgs(float x, float y)
        {
            PreciseX = x;
            PreciseY = y;
            X = Util.Round(x);
            Y = Util.Round(y);
        }

        internal abstract MouseEventArgs CloneWithOffset(int x, int y);
    }
}