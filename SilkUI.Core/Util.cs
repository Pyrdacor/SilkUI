using System;
using System.Drawing;

namespace SilkUI
{
    public static class Util
    {
        public static bool FloatEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.00001f;
        }

        public static int Floor(float f)
        {
            return (int)Math.Floor(f);
        }

        public static int Ceiling(float f)
        {
            return (int)Math.Ceiling(f);
        }

        public static int Round(float f)
        {
            return (int)Math.Round(f);
        }

        public static int Limit(int minValue, int value, int maxValue)
        {
            return Math.Max(minValue, Math.Min(value, maxValue));
        }

        public static short LimitToShort(int value)
        {
            return (short)Limit(short.MinValue, value, short.MaxValue);
        }

        public static float Min(float firstValue, float secondValue, params float[] values)
        {
            float min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static float Max(float firstValue, float secondValue, params float[] values)
        {
            float max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }

        public static int Min(int firstValue, int secondValue, params int[] values)
        {
            int min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static int Max(int firstValue, int secondValue, params int[] values)
        {
            int max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }

        public static float Distance(Point p1, Point p2)
        {
            var distX = p2.X - p1.X;
            var distY = p2.Y - p1.Y;
            return (float)Math.Sqrt(distX * distX + distY * distY);
        }

        internal static bool CheckGenericType(Type typeToCheck, Type baseType)
        {
            return typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == baseType;
        }
    }
}