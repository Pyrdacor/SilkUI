using System;

namespace SilkUI
{
    internal interface IControlProperty
    {
        string Name { get; }
        bool ChangeEventsEnabled { get; set; }
        bool HasValue { get; }
        U ConvertTo<U>();
        void Bind<U>(Observable<U> variable);
        void SetValue<U>(U value);
        bool IsEqual<U>(U value);
        Type GetPropertyType();
        object GetValue();
        void SetValue(object value);
    }

    internal class DisableChangeEventContext : IDisposable
    {
        private IControlProperty[] _properties;

        internal DisableChangeEventContext(params IControlProperty[] properties)
        {
            _properties = properties;

            foreach (var property in _properties)
                property.ChangeEventsEnabled = false;
        }

        public void Dispose()
        {
            foreach (var property in _properties)
                property.ChangeEventsEnabled = true;
        }
    }

    public abstract class ControlProperty<T> : IControlProperty
    {
        public string Name { get; }
        public abstract T Value { get; set; }
        public bool HasValue { get; protected set; } = false;
        public bool ChangeEventsEnabled { get; set; } = true;

        internal event Action InternalValueChanged;
        public event Action ValueChanged;
        /// <summary>
        /// Is not triggered by manual value changes.
        /// Only if the bound variable changes.
        /// </summary>
        public event Action DynamicValueChanged;

        internal ControlProperty(string name)
        {
            Name = name;
        }

        internal void OnValueChanged()
        {
            if (ChangeEventsEnabled)
                ValueChanged?.Invoke();
            InternalValueChanged?.Invoke();
        }

        U IControlProperty.ConvertTo<U>()
        {
            return ConvertTo<U>();
        }

        void IControlProperty.Bind<U>(Observable<U> variable)
        {
            Bind<U>(variable);
        }
        void IControlProperty.SetValue<U>(U value)
        {
            SetValue(value);
        }

        void IControlProperty.SetValue(object value)
        {
            SetValue(value);
        }

        bool IControlProperty.IsEqual<U>(U value)
        {
            return IsEqual<U>(value);
        }

        Type IControlProperty.GetPropertyType() => typeof(T);

        object IControlProperty.GetValue() => Value;

        internal abstract U ConvertTo<U>();

        internal void Bind<U>(Observable<U> variable)
        {
            variable?.Subscribe(value =>
            {
                if (!Value.Equals(value))
                {
                    SetValue(value);
                    if (ChangeEventsEnabled)
                    {
                        OnValueChanged();
                        DynamicValueChanged?.Invoke();
                    }
                }
            }, error => throw error); // TODO: how to handle errors here
        }

        internal void SetValue<U>(U value)
        {
            SetValue((object)value);
        }

        internal abstract void SetValue(object value);
        
        internal abstract bool IsEqual<U>(U value);
    }
}
