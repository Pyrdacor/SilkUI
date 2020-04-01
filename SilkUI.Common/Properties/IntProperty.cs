using System;

namespace SilkUI
{
    public class IntProperty : ControlProperty<int?>
    {
        private int? _value = null;

        public override int? Value 
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

        internal IntProperty(string name, int? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (type == typeof(int))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (type == typeof(int?))
                return (U)(object)_value;
            else if (type == typeof(uint))
            {
                if (!_value.HasValue)
                    throw new InvalidCastException();
                if (_value.Value < 0)
                    throw new ArgumentOutOfRangeException($"The value {_value.Value} can not be converted to an unsigned integer.");
                return (U)(object)_value;
            }
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else if (type == typeof(bool))
                return (U)(object)(_value.HasValue ? _value.Value != 0 : false);
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

            if (type == typeof(int))
                Value = (int)value;
            else if (type == typeof(int?))
                Value = (int?)value;
            else if (type == typeof(uint))
            {
                uint uintValue = (uint)value;

                if (uintValue > int.MaxValue)
                    throw new ArgumentOutOfRangeException($"The given value {uintValue} is too large for a signed integer.");

                Value = (int)uintValue;
            }
            else if (type == typeof(string))
                Value = int.Parse((string)value);
            else if (type == typeof(bool))
                Value = (bool)value ? 1 : 0;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == typeof(int))
                return _value == (int)(object)value;
            else if (type == typeof(int?))
            {
                var nullableInt = (int?)(object)value;
                if (nullableInt.HasValue != _value.HasValue)
                    return false;
                return nullableInt == _value;
            }
            else if (type == typeof(uint))
                return _value == (uint)(object)value;
            else if (type == typeof(string))
            {
                string stringValue = (string)(object)value;

                if (stringValue == null)
                    return !_value.HasValue;
                else if (!_value.HasValue)
                    return false;

                return _value.Value.ToString() == stringValue;
            }
            else if (type == typeof(bool))
                return _value.HasValue && (_value.Value != 0) == (bool)(object)value;
            else
                return false;
        }
    }
}
