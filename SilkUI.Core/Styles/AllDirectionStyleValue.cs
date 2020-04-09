using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SilkUI
{
    public enum StyleDirection
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public struct AllDirectionStyleValue<T> : IEquatable<AllDirectionStyleValue<T>> where T : struct
    {
        private static readonly Regex Pattern = new Regex(@"^([^ ]+) ?([^ ]+)? ?([^ ]+)? ?([^ ]+)?\s*$", RegexOptions.Compiled);

        public T Left;
        public T Right;
        public T Top;
        public T Bottom;

        public bool Equals(AllDirectionStyleValue<T> other)
        {
            var comparer = EqualityComparer<T>.Default;
            return comparer.Equals(Left, other.Left) &&
                   comparer.Equals(Right, other.Right) &&
                   comparer.Equals(Top, other.Top) &&
                   comparer.Equals(Bottom, other.Bottom);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return Equals((AllDirectionStyleValue<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, Right, Top, Bottom);
        }

        public static bool operator ==(AllDirectionStyleValue<T> lhs, AllDirectionStyleValue<T> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(AllDirectionStyleValue<T> lhs, AllDirectionStyleValue<T> rhs)
        {
            return !(lhs == rhs);
        }

        public AllDirectionStyleValue(T all)
        {
            Left = Right = Top = Bottom = all;
        }
        
        public AllDirectionStyleValue(T topBottom, T leftRight)
        {
            Left = Right = leftRight;
            Top = Bottom = topBottom;
        }

        public AllDirectionStyleValue(T top, T leftRight, T bottom)
        {
            Left = Right = leftRight;
            Top = top;
            Bottom = bottom;
        }

        public AllDirectionStyleValue(T top, T right, T bottom, T left)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static implicit operator AllDirectionStyleValue<T>(T value)
        {
            return new AllDirectionStyleValue<T>(value);
        }

        public static implicit operator AllDirectionStyleValue<T>(Tuple<T, T> value)
        {
            return new AllDirectionStyleValue<T>(value.Item1, value.Item2);
        }

        public static implicit operator AllDirectionStyleValue<T>(Tuple<T, T, T> value)
        {
            return new AllDirectionStyleValue<T>(value.Item1, value.Item2, value.Item3);
        }

        public static implicit operator AllDirectionStyleValue<T>(Tuple<T, T, T, T> value)
        {
            return new AllDirectionStyleValue<T>(value.Item1, value.Item2, value.Item3, value.Item4);
        }

        public static implicit operator AllDirectionStyleValue<T>(string value)
        {
            var match = Pattern.Match(value);

            if (match.Success)
            {
                if (match.Groups.Count >= 5 && !string.IsNullOrEmpty(match.Groups[4].Value))
                {
                    return new AllDirectionStyleValue<T>
                    (
                        (T)Convert.ChangeType(match.Groups[1].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[2].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[3].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[4].Value, typeof(T))
                    );
                }
                else if (match.Groups.Count >= 4 && !string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    return new AllDirectionStyleValue<T>
                    (
                        (T)Convert.ChangeType(match.Groups[1].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[2].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[3].Value, typeof(T))
                    );
                }
                else if (match.Groups.Count >= 3 && !string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    return new AllDirectionStyleValue<T>
                    (
                        (T)Convert.ChangeType(match.Groups[1].Value, typeof(T)),
                        (T)Convert.ChangeType(match.Groups[2].Value, typeof(T))
                    );
                }
                else if (match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    try
                    {
                        return new AllDirectionStyleValue<T>
                        (
                            (T)Convert.ChangeType(match.Groups[1].Value, typeof(T))
                        );
                    }
                    catch (InvalidCastException)
                    {
                        // try to convert string to the underlying type T
                        if (typeof(T) == typeof(ColorValue))
                            return new AllDirectionStyleValue<T>((T)(object)(ColorValue)value);
                    }
                }
            }
            
            throw new FormatException("Invalid input value format.");
        }
    }
}