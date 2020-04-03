using System;

namespace SilkUI
{
    public class StringProperty : ControlProperty<string>
    {
        private string _value = null;

        public override string Value 
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

        internal StringProperty(string name, string initialValue = null)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            if (typeof(U) == typeof(string))
                return (U)(object)_value;
            else
                throw new InvalidCastException();
        }

        internal override void SetValue(object value)
        {
            Value = Object.ReferenceEquals(value, null) ? null : value.ToString();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (type == typeof(string))
                return _value == (string)(object)value;
            else
                return false;
        }
    }
}
