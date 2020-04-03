using System;

namespace SilkUI
{
    internal class Subscriber<T> : IObserver<T>
    {
        protected Action<T> _nextAction = null;
        protected Action<Exception> _errorAction = null;
        protected Action _completeAction = null;

        public Subscriber(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            _nextAction = next;
            _errorAction = error;
            _completeAction = complete;
        }

        public void Next(T value) => _nextAction?.Invoke(value);
        public void Error(Exception exception) => _errorAction?.Invoke(exception);
        public void Complete() => _completeAction?.Invoke();
    }
}