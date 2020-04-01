using System;

namespace SilkUI
{
    public class BoolProperty : ControlProperty<bool?>
    {
        private bool? _value = null;

        public override bool? Value 
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

        internal BoolProperty(string name, bool? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (type == typeof(bool))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (type == typeof(bool?))
                return (U)(object)_value;
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else if (type == typeof(int))
                return (U)(object)(_value.HasValue ? (_value.Value ? 1 : 0) : throw new InvalidCastException());
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

            if (type == typeof(bool))
                Value = (bool)value;
            else if (type == typeof(bool?))
                Value = (bool?)value;
            else if (type == typeof(string))
            {
                string stringValue = (string)value;

                if (stringValue == null)
                    Value = null;

                stringValue = stringValue.ToLower();

                if (stringValue == "true" || stringValue == "1")
                    Value = true;
                else if (stringValue == "false" || stringValue == "0")
                    Value = false;
                else
                    throw new InvalidCastException();
            }
            else if (type == typeof(int))
                Value = ((int)value) != 0;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == typeof(bool))
                return _value == (bool)(object)value;
            else if (type == typeof(bool?))
            {
                var nullableBool = (bool?)(object)value;
                if (nullableBool.HasValue != _value.HasValue)
                    return false;
                return !nullableBool.HasValue || nullableBool == _value;
            }
            else if (type == typeof(string))
            {
                string stringValue = (string)(object)value;

                if (stringValue == null)
                    return !_value.HasValue;
                else if (!_value.HasValue)
                    return false;

                stringValue = stringValue.ToLower();

                if (_value.Value && (stringValue == "true" || stringValue == "1"))
                    return true;

                return !_value.Value && (stringValue == "false" || stringValue == "0");
            }
            else if (type == typeof(int))
                return _value == (((int)(object)value) != 0);
            else
                return false;
        }
    }
}
