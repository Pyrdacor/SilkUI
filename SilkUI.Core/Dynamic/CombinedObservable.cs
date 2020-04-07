using System.Linq;
using System;
using System.Collections.Generic;

namespace SilkUI
{
    internal enum CombinedType
    {
        Any,
        All
    }

    /// <summary>
    /// Creates a combined observable which react to changes of
    /// its child observables.
    /// </summary>
    /// <typeparam name="T">Value type of child observables</typeparam>
    /// <typeparam name="U">Combined value type (e.g. an array like T[])</typeparam>
    internal class CombinedObservable<T, U> : Observable<U>, IObservableStatusProvider
    {
        private enum ObservableState
        {
            Empty, // no next or complete yet
            Error,
            HasValue, // at least one next was called
            Completed, // completed with value
            CompletedEmpty // completed without a value
        }

        private class ObservableInfo
        {
            public ObservableState State;
            public T Value;
        }

        private readonly Dictionary<Observable<T>, ObservableInfo> _observables;
        private readonly Dictionary<Observable<T>, Subscription<T>> _subscriptions = new Dictionary<Observable<T>, Subscription<T>>();
        private Func<IEnumerable<T>, U> _mapper;
        private Exception _mostRecentError = null;
        private bool _replay = false;
        private bool _allowErrors = false;
        private bool _oneValuePerObservale = false;
        private bool _hasValue = false;
        private bool _errored = false;
        private bool _completed = false;
        private CombinedType _combinedNextType;
        private CombinedType _combinedCompleteType;
        private U _lastValue = default(U);

        bool IObservableStatusProvider.HasValue => _hasValue;
        bool IObservableStatusProvider.Errored => _errored;
        bool IObservableStatusProvider.Completed => _completed;
        internal U LastValue => _lastValue;

        /// <summary>
        /// Create a wrapper observable that handles a list of observables.
        /// </summary>
        /// <param name="combinedNextType">Any: Emit when any observable emits. All: Emit when all observables have emitted once.</param>
        /// <param name="combinedCompleteType">Any: Complete when any observable completes. All: Complete when all observables are completed.</param>
        /// <param name="replay">Emit for every new subscriber if not completed and a value is present.</param>
        /// <param name="allowErrors">If false any error will immediately error and complete the observable.</param>
        /// <param name="oneValuePerObservale">If true after first emission of an observable, value subscription to that observable ends.</param>
        /// <param name="mapper">Mapping function to convert a list of inputs to the desired output.</param>
        /// <param name="observables">Input observables</param>
        public CombinedObservable(CombinedType combinedNextType, CombinedType combinedCompleteType, bool replay, bool allowErrors,
            bool oneValuePerObservale, Func<IEnumerable<T>, U> mapper, params Observable<T>[] observables)
        {
            if (observables.Length == 0)
            {
                _completed = true;
                return;
            }

            _mapper = mapper;
            _combinedNextType = combinedNextType;
            _combinedCompleteType = combinedCompleteType;
            _replay = replay;
            _allowErrors = allowErrors;
            _oneValuePerObservale = oneValuePerObservale;
            _observables = new Dictionary<Observable<T>, ObservableInfo>(observables.Length);

            foreach (var observable in observables)
            {
                var state = ObservableState.Empty;

                if (observable is IObservableStatusProvider observableWithStatus)
                {                   
                    if (observableWithStatus.Errored)
                    {
                        if (allowErrors)
                            state = ObservableState.Error;
                        else
                        {
                            _completed = true;
                            return;
                        }
                    }
                    else if (observableWithStatus.HasValue)
                        state = observableWithStatus.Completed ? ObservableState.Completed : ObservableState.HasValue;
                    else if (observableWithStatus.Completed)
                        state = ObservableState.CompletedEmpty;
                }
                
                _observables.Add(observable, new ObservableInfo() { State = state });
                _subscriptions.Add(observable, observable.Subscribe(
                    value => {
                        var observableInfo = _observables[observable];
                        observableInfo.Value = value;
                        if (oneValuePerObservale)
                        {
                            observableInfo.State = ObservableState.Completed;
                            if (_subscriptions.ContainsKey(observable))
                            {
                                _subscriptions[observable].Unsubscribe();
                                _subscriptions.Remove(observable);
                            }
                        }
                        else if (observableInfo.State == ObservableState.Empty)
                            observableInfo.State = ObservableState.HasValue;
                        UpdateState();
                    },
                    error => {
                        _mostRecentError = error;
                        _observables[observable].State = ObservableState.Error;
                        UpdateState();
                    },
                    () => {
                        var observableInfo = _observables[observable];
                        if (observableInfo.State != ObservableState.Error)
                            observableInfo.State = observableInfo.State == ObservableState.HasValue ? ObservableState.Completed : ObservableState.CompletedEmpty;
                        UpdateState();
                    }
                ));
                if (_subscriptions.ContainsKey(observable) && _subscriptions[observable] == Subscription<T>.Empty)
                    _subscriptions.Remove(observable);
            }
        }

        public override Subscription<U> Subscribe(Action<U> next, Action<Exception> error = null, Action complete = null)
        {
            if (_completed)
                return Subscription<U>.Empty;

            if (_observables.Count == 0)
            {
                // This case happens if an error exists already
                // in the starting observables and errors are not allowed.
                // As there is no subscription on creating the observable
                // we will wait for the first subscription, pass the error
                // to the first subscriber and complete the observable.
                error?.Invoke(_mostRecentError);
                _completed = true;
                return Subscription<U>.Empty;
            }

            var subscription = base.Subscribe(next, error, complete);

            if (_replay && _hasValue)
                CallNextActions(_lastValue);

            return subscription;
        }

        private void UpdateState()
        {
            int numWithValue = 0;
            int numCompleted = 0;

            foreach (var observable in _observables)
            {
                var state = observable.Value.State;

                if (state == ObservableState.Error)
                {
                    if (!_allowErrors)
                    {
                        _hasValue = false;
                        _errored = true;
                        _completed = true;                    
                        CallErrorActions(_mostRecentError);
                        Clear();
                        return;
                    }
                    else if (_combinedNextType == CombinedType.All && _combinedCompleteType == CombinedType.All)
                    {
                        // no need to look any further -> can never call next nor complete with an error
                        return;
                    }
                }

                if (numWithValue != -1)
                {
                    bool hasValue = state == ObservableState.HasValue || state == ObservableState.Completed;

                    if (_combinedNextType == CombinedType.All && !hasValue)
                        numWithValue = -1; // no further checking
                    else if (hasValue)
                    {
                        if (_combinedNextType == CombinedType.All)
                            ++numWithValue;
                        else
                            numWithValue = int.MaxValue; // no further checking needed
                    }
                }

                if (numCompleted == -1)
                {
                    bool completed = state == ObservableState.Completed || state == ObservableState.CompletedEmpty;

                    if (_combinedCompleteType == CombinedType.All && !completed)
                        numCompleted = -1; // no further checking
                    else if (completed)
                    {
                        if (_combinedCompleteType == CombinedType.All)
                            ++numCompleted;
                        else
                            numCompleted = int.MaxValue; // no further checking needed
                    }
                }

                if ((numWithValue == -1 || numWithValue == int.MaxValue) && (numCompleted == -1 || numCompleted == int.MaxValue))
                    break; // no need to look any further
            }

            if (numWithValue >= _observables.Count)
            {
                _lastValue = _mapper(_observables.Select(o => o.Value.Value));
                _hasValue = true;
                CallNextActions(_lastValue);
            }

            if (numCompleted >= _observables.Count)
            {
                _completed = true;
                CallCompleteActions();
                Clear();
            }
        }

        private void Clear()
        {
            foreach (var subscription in _subscriptions.Values)
                subscription.Unsubscribe();

            _subscriptions.Clear();
            _observables.Clear();
            _lastValue = default(U);
        }
    }
}