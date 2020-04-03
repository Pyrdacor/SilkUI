using System;
using System.Linq;

namespace SilkUI
{
    public class AllDirectionProperty<T> : ControlProperty<AllDirectionStyleValue<T>?> where T : struct
    {
        private AllDirectionStyleValue<T>? _value = null;

        public override AllDirectionStyleValue<T>? Value 
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

        internal AllDirectionProperty(string name)
            : base(name)
        {
            HasValue = false;
        }

        internal AllDirectionProperty(string name, AllDirectionStyleValue<T>? initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, AllDirectionStyleValue<T> initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, string initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, T? initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, T initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, Tuple<T, T> initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, Tuple<T, T, T> initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal AllDirectionProperty(string name, Tuple<T, T, T, T> initialValue)
            : base(name)
        {
            _value = initialValue;
            HasValue = _value != null;
        }

        internal override U ConvertTo<U>()
        {
            var type = typeof(U);

            if (Util.CheckGenericType(type, typeof(AllDirectionStyleValue<>)) && type.GenericTypeArguments[0] == typeof(T))
                return (U)(object)(_value.HasValue ? _value.Value : throw new InvalidCastException());
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) && Util.CheckGenericType(type.GenericTypeArguments[0], typeof(AllDirectionStyleValue<>)) &&
                type.GenericTypeArguments[0].GenericTypeArguments[0] == typeof(T))
                return (U)(object)_value;
            else if (type == typeof(string))
                return (U)(object)(_value.HasValue ? _value.Value.ToString() : null);
            else if (type == typeof(T))
                return (U)(object)(_value.HasValue ? _value.Value.Top : throw new InvalidCastException());
            else if (Util.CheckGenericType(type, typeof(Tuple<>)))
            {
                if (!_value.HasValue)
                    return (U)(object)null;
                
                if (type.GenericTypeArguments.Any(t => t != typeof(T)))
                    throw new InvalidCastException();

                return type.GenericTypeArguments.Length switch
                {
                    2 => (U)(object)Tuple.Create(_value.Value.Top, _value.Value.Right),
                    3 => (U)(object)Tuple.Create(_value.Value.Top, _value.Value.Right, _value.Value.Bottom),
                    4 => (U)(object)Tuple.Create(_value.Value.Top, _value.Value.Right, _value.Value.Bottom, _value.Value.Left),
                    _ => throw new InvalidCastException()
                };
            }
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

            if (Util.CheckGenericType(type, typeof(AllDirectionStyleValue<>)) && type.GenericTypeArguments[0] == typeof(T))
                Value = (AllDirectionStyleValue<T>)(object)value;
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) &&
                Util.CheckGenericType(type.GenericTypeArguments[0], typeof(AllDirectionStyleValue<>)) &&
                type.GenericTypeArguments[0].GenericTypeArguments[0] == typeof(T))
                Value = (AllDirectionStyleValue<T>?)value;
            else if (type == typeof(string))
                Value = (AllDirectionStyleValue<T>)(string)value;
            else if (type == typeof(T))
                Value = (AllDirectionStyleValue<T>)(T)value;
            else if (Util.CheckGenericType(type, typeof(Tuple<>)))
                // TODO?
                Value = (AllDirectionStyleValue<T>)value;
            else
                throw new InvalidCastException();
        }

        internal override bool IsEqual<U>(U value)
        {
            var type = typeof(U);

            if (Util.CheckGenericType(type, typeof(AllDirectionStyleValue<>)) && type.GenericTypeArguments[0] == typeof(T))
                return _value.HasValue && _value.Value == (AllDirectionStyleValue<T>)(object)value;
            else if (Util.CheckGenericType(type, typeof(Nullable<>)) && Util.CheckGenericType(type.GenericTypeArguments[0], typeof(AllDirectionStyleValue<>)) &&
                type.GenericTypeArguments[0].GenericTypeArguments[0] == typeof(T))
                // TODO
                return false;
            else if (type == typeof(string))
                // TODO
                return false;
            else if (type == typeof(T))
                // TODO
                return false;
            else if (Util.CheckGenericType(type, typeof(Tuple<>)))
                // TODO
                return false;
            else
                return false;
        }
    }
}
