using System;

namespace SilkUI
{
    public struct Dimension : IEquatable<Dimension>
    {
        public enum Type
        {
            Fill,
            Value,
            Percent
        }

        private readonly uint? _value;
        public Type DimensionType { get; }

        public bool Equals(Dimension other)
        {
            return DimensionType == other.DimensionType && _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return Equals((Dimension)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DimensionType, _value);
        }

        public static bool operator ==(Dimension lhs, Dimension rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Dimension lhs, Dimension rhs)
        {
            return !(lhs == rhs);
        }

        public Dimension(Type type, uint? value = null)
        {
            DimensionType = type;
            _value = type switch
            {
                Type.Fill => null,
                Type.Value => value ?? throw new ArgumentNullException(nameof(value), "No dimension value given."),
                Type.Percent => value ?? throw new ArgumentNullException(nameof(value), "No dimension percentage value given."),
                _ => throw new ArgumentException($"Invalid dimension type {type}.")
            };
        }

        public static implicit operator Dimension(uint value)
        {
            if (value > int.MaxValue)
                value = int.MaxValue; // Internal signed int is used but without negative values possible.

            return new Dimension(Type.Value, value);
        }

        public static implicit operator Dimension(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Negative value {value} for dimension was given.");

            return new Dimension(Type.Value, (uint)value);
        }

        public static implicit operator Dimension(Type dimensionType)
        {
            return new Dimension(dimensionType);
        }

        public static implicit operator Dimension(string value)
        {
            if (int.TryParse(value, out int intValue))
                return intValue;
            else if (uint.TryParse(value, out uint uintValue))
                return uintValue;
            else if (Enum.TryParse(typeof(Type), value, true, out object enumValue))
                return (Type)enumValue;
            else
                throw new FormatException($"Invalid dimension value `{value}`.");
        }

        internal uint GetValue(uint parentDimension)
        {
            return DimensionType switch
            {
                Type.Fill => parentDimension,
                Type.Value => Math.Max(0u, _value.Value),
                Type.Percent => Math.Max(0u, _value.Value) * parentDimension / 100u,
                _ => throw new ArgumentException($"Invalid dimension type {DimensionType}.")
            };
        }
    }
}
