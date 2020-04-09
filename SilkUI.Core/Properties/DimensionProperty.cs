using System;

namespace SilkUI
{
    public class DimensionProperty : ControlProperty<Dimension?>
    {
        private Dimension? _value = null;

        public override Dimension? Value 
        { 
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    HasValue = _value != null;
                    OnValueChanged();
                }
            }
        }

        internal DimensionProperty(string name, string initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal DimensionProperty(string name, int? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal DimensionProperty(string name, uint? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal DimensionProperty(string name, Dimension? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (type == typeof(Dimension))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (type == typeof(Dimension?))
                return (U)(object)_value;
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else
                throw new InvalidCastException();
        }

        internal override void SetValue(object value)
        {
            if (Object.ReferenceEquals(value, null))
            {
                Value = null;
                return;
            }
            
            var type = value.GetType();

            if (type == typeof(Dimension))
                Value = (Dimension)value;
            else if (type == typeof(Dimension?))
                Value = (Dimension?)value;
            else if (type == typeof(string))
            {
                string stringValue = (string)value;

                if (stringValue == null)
                    Value = null;
                else
                    Value = stringValue;
            }
            else if (type == typeof(int))
                Value = (int)value;
            else if (type == typeof(uint))
                Value = (uint)value;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == typeof(Dimension))
                return _value.HasValue && _value.Equals((Dimension)(object)value);
            else if (type == typeof(Dimension?))
            {
                Dimension? nullableDimension = (Dimension?)(object)value;
                if (nullableDimension.HasValue != _value.HasValue)
                    return false;
                return !nullableDimension.HasValue || _value.Value.Equals(nullableDimension);
            }
            else if (type == typeof(string))
            {
                string stringValue = (string)(object)value;

                if (stringValue == null)
                    return !_value.HasValue;
                else if (!_value.HasValue)
                    return false;

                try
                {
                    return _value.Equals((Dimension)stringValue);
                }
                catch
                {
                    return false;
                }
            }
            else if (type == typeof(int))
                return _value.HasValue && _value.Equals((Dimension)(int)(object)value);
            else if (type == typeof(uint))
                return _value.HasValue && _value.Equals((Dimension)(uint)(object)value);
            else
                return false;
        }
    }
}
