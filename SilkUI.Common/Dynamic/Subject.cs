using System;

namespace SilkUI
{
    /// <summary>
    /// Subject which provides a value by using its
    /// <see cref="Next"> method.
    /// </summary>
    public class Subject<T> : Observable<T>, IObserver<T>, IObservableStatusProvider
    {
        protected T _currentValue = default(T);
        private Exception _errorException = null;
        protected bool _hasValue = false;
        protected bool _errored = false;
        protected bool _completed = false;

        internal T CurrentValue => _currentValue;
        internal Exception ErrorException => _errorException;
        bool IObservableStatusProvider.HasValue => _hasValue;
        bool IObservableStatusProvider.Errored => _errored;
        bool IObservableStatusProvider.Completed => _completed;

        public virtual void Next(T value)
        {
            if (_completed)
                return;

            _currentValue = value;
            _hasValue = true;

            CallNextActions(_currentValue);
        }

        public virtual void Error(Exception error)
        {
            if (_completed)
                return;

            _hasValue = false;
            _errorException = error;
            _errored = true;
            _completed = true;
            CallErrorActions(error);
        }

        public virtual void Complete()
        {
            if (_completed)
                return;

            _completed = true;
            CallCompleteActions();
        }

        public void CompleteWith(T value)
        {
            Next(value);
            Complete();
        }

        internal override Subject<T> AsSubject()
        {
            return this;
        }
    }
}