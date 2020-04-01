using System;

namespace SilkUI
{
    public class EnumProperty : ControlProperty<int?>
    {
        private Type _enumType;
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

        internal EnumProperty(string name, Type enumType, int? initialValue = null)
            : base(name)
        {
            _enumType = enumType;
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (type == _enumType)
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) && type.GenericTypeArguments[0] == _enumType)
                return (U)(object)(_value.HasValue ? Enum.ToObject(_enumType, _value.Value) : null);
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else if (type == typeof(int))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
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

            if (type == _enumType)
                Value = (int)(object)value;
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) && type.GenericTypeArguments[0] == _enumType)
                Value = (int?)value;
            else if (type == typeof(string))
            {
                string stringValue = (string)value;

                try
                {
                    Value = int.Parse(stringValue);
                }
                catch
                {
                    Value = (int)Enum.Parse(_enumType, stringValue);
                }
            }
            else if (type == typeof(int))
                Value = (int)value;
            else if (type == typeof(int?))
                Value = (int?)value;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == _enumType)
                return _value.HasValue && (int)_value.Value == (int)(object)value;
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) && type.GenericTypeArguments[0] == _enumType)
            {
                var nullableInt = (int?)(object)value;
                if (nullableInt.HasValue != _value.HasValue)
                    return false;
                return !nullableInt.HasValue || nullableInt == (int)_value;
            }
            else if (type == typeof(string))
            {
                string stringValue = (string)(object)value;

                if (stringValue == null)
                    return !_value.HasValue;
                else if (!_value.HasValue)
                    return false;

                return _value.Value.ToString() == stringValue ||
                    Enum.ToObject(_enumType, _value.Value).ToString() == stringValue;
            }
            else if (type == typeof(int))
                return _value.HasValue && (int)_value.Value == (int)(object)value;
            else if (type == typeof(int?))
            {
                var nullableInt = (int?)(object)value;
                if (nullableInt.HasValue != _value.HasValue)
                    return false;
                return !nullableInt.HasValue || nullableInt == (int)_value;
            }
            else
                return false;
        }
    }
}
