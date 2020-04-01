using System;

namespace SilkUI
{
    internal class ConstantObservable<T> : Observable<T>, IObservableStatusProvider
    {
        private T _value;

        bool IObservableStatusProvider.HasValue => true;
        bool IObservableStatusProvider.Errored => false;
        bool IObservableStatusProvider.Completed => true;

        internal ConstantObservable(T value)
        {
            _value = value;
        }

        public override Subscription<T> Subscribe(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            next?.Invoke(_value);
            complete?.Invoke();

            return new Subscription<T>(null, null);
        }
    }
}