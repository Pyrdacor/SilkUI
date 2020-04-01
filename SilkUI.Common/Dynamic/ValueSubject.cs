using System;

namespace SilkUI
{
    /// <summary>
    /// Subject which has an initial value.
    /// On first subscription the value is immediately provided.
    /// </summary>
    public class ValueSubject<T> : Subject<T>
    {
        private bool _firstSubscription = true;

        public T Value => _currentValue;

        public ValueSubject(T value)
        {
            _currentValue = value;
            _hasValue = true;
        }

        public override Subscription<T> Subscribe(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            if (_completed)
                return Subscription<T>.Empty;

            var subscription = base.Subscribe(next, error, complete);

            if (_firstSubscription)
            {
                _firstSubscription = false;
                CallNextActions(_currentValue);
            }

            return subscription;
        }
    }
}