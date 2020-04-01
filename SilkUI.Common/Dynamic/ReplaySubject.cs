using System;

namespace SilkUI
{
    /// <summary>
    /// Subject which has an optional initial value and
    /// will emit its value on every subscription immediately.
    /// </summary>
    public class ReplaySubject<T> : Subject<T>
    {
        public ReplaySubject()
        {

        }

        public ReplaySubject(T value)
        {
            _currentValue = value;
            _hasValue = true;
        }

        public override Subscription<T> Subscribe(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            if (_completed)
                return Subscription<T>.Empty;

            var subscription = base.Subscribe(next, error, complete);

            CallNextActions(_currentValue);

            return subscription;
        }
    }
}