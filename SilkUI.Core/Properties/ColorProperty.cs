using System;
using System.Drawing;

namespace SilkUI
{
    public class ColorProperty : ControlProperty<ColorValue?>
    {
        private ColorValue? _value = null;

        public override ColorValue? Value 
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

        internal ColorProperty(string name, string initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal ColorProperty(string name, int? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal ColorProperty(string name, Color? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal ColorProperty(string name, ColorValue? initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (type == typeof(ColorValue))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (type == typeof(ColorValue?))
                return (U)(object)_value;
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else if (type == typeof(int))
                return (U)(object)(_value.HasValue ? _value.Value.ToInt() : 0);
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

            if (type == typeof(ColorValue))
                Value = (ColorValue)value;
            else if (type == typeof(ColorValue?))
                Value = (ColorValue?)value;
            else if (type == typeof(string))
            {
                string stringValue = (string)value;

                if (stringValue == null)
                    Value = null;
                else
                    Value = (ColorValue)stringValue;
            }
            else if (type == typeof(int))
                Value = new ColorValue((int)value);
            else if (type == typeof(Color))
                Value = (ColorValue)(Color)value;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == typeof(ColorValue))
                return _value.HasValue && _value.Equals((ColorValue)(object)value);
            else if (type == typeof(ColorValue?))
            {
                ColorValue? nullableColor = (ColorValue?)(object)value;
                if (nullableColor.HasValue != _value.HasValue)
                    return false;
                return !nullableColor.HasValue || _value.Value.Equals(nullableColor);
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
                    return _value.Equals((ColorValue)stringValue);
                }
                catch
                {
                    return false;
                }
            }
            else if (type == typeof(int))
                return _value.HasValue && _value.Equals(new ColorValue((int)(object)value));
            else if (type == typeof(Color))
                return _value.HasValue && _value.Equals((ColorValue)(Color)(object)value);
            else
                return false;
        }
    }
}
