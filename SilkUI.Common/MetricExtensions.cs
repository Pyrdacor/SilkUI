using System.Drawing;

namespace SilkUI
{
    public static class MetricExtensions
    {
        public static Point Add(this Point point, Point other)
        {
            return new Point(point.X + other.X, point.Y + other.Y);
        }

        public static Point Sub(this Point point, Point other)
        {
            return new Point(point.X - other.X, point.Y - other.Y);
        }

        public static Point Approximate(this PointF point)
        {
            return new Point(Util.Round(point.X), Util.Round(point.Y));
        }
    }
}