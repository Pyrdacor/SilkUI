using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkUI
{
    internal interface IObservableStatusProvider
    {
        bool HasValue { get; }
        bool Completed { get; }
        bool Errored { get; }
    }

    public abstract class Observable<T>
    {
        private readonly List<Subscriber<T>> _subscribers = new List<Subscriber<T>>();

        public virtual Subscription<T> Subscribe(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            var subscriber = new Subscriber<T>(next, error, complete);

            _subscribers.Add(subscriber);

            return new Subscription<T>(this, subscriber);
        }

        public Subscription<T> Subscribe(IObserver<T> observer)
        {
            return Subscribe(observer.Next, observer.Error, observer.Complete);
        }

        public void SubscribeUntilCompleted(Action<T> next, Action<Exception> error = null, Action complete = null)
        {
            var subscription = Subscription<T>.Empty;
            subscription = this.Subscribe(next, error, () =>
            {
                complete?.Invoke();
                subscription.Unsubscribe();
            });
        }

        public void SubscribeUntilCompleted(IObserver<T> observer)
        {
            SubscribeUntilCompleted(observer.Next, observer.Error, observer.Complete);
        }

        internal void Unsubscribe(Subscriber<T> subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        protected void CallNextActions(T value)
        {
            // use to list as the collection may change in Next
            _subscribers.ToList().ForEach(s => s.Next(value));
        }

        protected void CallErrorActions(Exception error)
        {
            // use to list as the collection may change in Error
            _subscribers.ToList().ForEach(s => s.Error(error));
        }

        protected void CallCompleteActions()
        {
            // use to list as the collection may change in Complete
            _subscribers.ToList().ForEach(s => s.Complete());
        }

        public static implicit operator Observable<T>(T value)
        {
            return Observable.Of<T>(value);
        }

        internal virtual Subject<T> AsSubject()
        {
            var subject = new Subject<T>();

            SubscribeUntilCompleted(subject);

            return subject;
        }    
    }

    public static class Observable
    {
        /// <summary>
        /// Creates an observable that holds the given value.
        /// It will always emit the value on subscription but
        /// not after it. It will also emit the complete
        /// event on each subscription.
        /// </summary>
        public static Observable<T> Of<T>(T value)
        {
            return new ConstantObservable<T>(value);
        }

        /// <summary>
        /// Emits all values of the given observables if all of them
        /// have at least emit one value.
        /// Always the most recent value of each observable is emitted
        /// and after all observables have emitted a value, each subsequent
        /// value emission of any of the given observables will lead to
        /// a re-emission of the combined observable.
        /// The resulting observable is only completed when
        /// all given observables are completed.
        /// </summary>
        public static Observable<T[]> All<T>(params Observable<T>[] observables)
        {
            return new CombinedObservable<T, T[]>
            (
                CombinedType.All, CombinedType.All, false, false, false,
                (IEnumerable<T> input) => input.ToArray(),
                observables
            );
        }

        /// <summary>
        /// Emits all values of the given observables that have been
        /// emitted first by them.
        /// The resulting observable is immediately completed when
        /// all given observables have emitted one value.
        /// </summary>
        public static Observable<T[]> FirstOfAll<T>(params Observable<T>[] observables)
        {
            return new CombinedObservable<T, T[]>
            (
                CombinedType.All, CombinedType.All, false, false, true,
                (IEnumerable<T> input) => input.ToArray(),
                observables
            );
        }

        /// <summary>
        /// Emits all values of the given observables that have been
        /// emitted last by them.
        /// The resulting observable is immediately completed when
        /// all given observables have emitted one value.
        /// </summary>
        public static Observable<T[]> LastOfAll<T>(params Observable<T>[] observables)
        {
            var subject = new Subject<T[]>();
            var combinedObservable = new CombinedObservable<T, T[]>
            (
                CombinedType.All, CombinedType.All, false, false, false,
                (IEnumerable<T> input) => input.ToArray(),
                observables
            );
            combinedObservable.SubscribeUntilCompleted(null, subject.Error, () => subject.CompleteWith(combinedObservable.LastValue));
            return subject;
        }

        /// <summary>
        /// Emits any new value from the given observables.
        /// The resulting observable is only completed when
        /// all given observables are completed.
        /// </summary>
        public static Observable<T> Any<T>(params Observable<T>[] observables)
        {
            return new CombinedObservable<T, T>
            (
                CombinedType.Any, CombinedType.All, false, false, false,
                (IEnumerable<T> input) => input.First(),
                observables
            );
        }

        /// <summary>
        /// Emits the first new value from any of the given observables.
        /// The resulting observable is immediately completed when
        /// the first value was emitted.
        /// </summary>
        public static Observable<T> First<T>(params Observable<T>[] observables)
        {
            return new CombinedObservable<T, T>
            (
                CombinedType.Any, CombinedType.Any, false, false, true,
                (IEnumerable<T> input) => input.First(),
                observables
            );
        }

        /// <summary>
        /// Reduces the values of all given observables by the given reducer.
        /// The reduce result is emitted when all observables have emitted
        /// at least one value and after that every time any of the observables
        /// emit a new value.
        /// The resulting observable is only completed when
        /// all given observables are completed.
        /// </summary>
        public static Observable<U> Reduce<T, U>(Func<IEnumerable<T>, U> reducer, params Observable<T>[] observables)
        {
            return new CombinedObservable<T, U>
            (
                CombinedType.All, CombinedType.All, false, false, false,
                reducer,
                observables
            );
        }
    }

    public static class ObservableExtensions
    {
        /// <summary>
        /// Map the value of an observable to another value.
        /// The result is a new observable which contains
        /// the map value.
        /// </summary>
        public static Observable<U> Map<T, U>(this Observable<T> observable, Func<T, U> mapper)
        {
            var newObservable = new Subject<U>();

            observable.SubscribeUntilCompleted(value => newObservable.Next(mapper(value)), newObservable.Error, newObservable.Complete);

            return newObservable;
        }

        /// <summary>
        /// Combines two observables to a new observable with both values
        /// as its value. The combined value is stored as a tuple with
        /// two elements.
        /// </summary>
        public static Observable<Tuple<T, U>> Combine<T, U>(this Observable<T> observable, Observable<U> other)
        {
            return new CombinedObservable<object, Tuple<T, U>>
            (
                CombinedType.All, CombinedType.All, false, false, false,
                (IEnumerable<object> input) => Tuple.Create((T)input.First(), (U)input.Last()),
                observable, other
            );
        }

        /// <summary>
        /// Merges two observables to a new observable.
        /// The new value is determined by the given merge function.
        /// </summary>
        public static Observable<V> Merge<T, U, V>(this Observable<T> observable, Observable<U> other, Func<T, U, V> merger)
        {
            return Combine(observable, other).Map(combined => merger(combined.Item1, combined.Item2));
        }

        /// <summary>
        /// Delays the emission of this observable until another observable is completed.
        /// The new observable completes if:
        /// - The other observable completes and this observable already has a value to emit.
        /// - The other observable is completed and the observable emits its first value.
        /// </summary>
        public static Observable<T> WaitFor<T, U>(this Observable<T> observable, Observable<U> other)
        {
            var observableWrapper = observable.AsSubject();
            var observableStatusProvider = observableWrapper as IObservableStatusProvider;
            var subject = new Subject<T>();
            other.SubscribeUntilCompleted(null, subject.Error, () =>
                {
                    if (observableStatusProvider.Errored)
                        subject.Error(observableWrapper.ErrorException);
                    else if (observableStatusProvider.HasValue)
                        subject.CompleteWith(observableWrapper.CurrentValue);
                    else
                        observable.SubscribeUntilCompleted(value => subject.CompleteWith(value), subject.Error, subject.Complete);
                }
            );
            return subject;
        }

        /// <summary>
        /// Delays the emission of this observable until another observable's value
        /// meets a given condition.
        /// The new observable will complete if the second observable completes, even
        /// if the condition was never met.
        /// </summary>
        public static Observable<T> WaitForCondition<T, U>(this Observable<T> observable, Observable<U> other, Func<U, bool> condition)
        {
            var observableWrapper = observable.AsSubject();
            var observableStatusProvider = observableWrapper as IObservableStatusProvider;
            var subject = new Subject<T>();
            other.SubscribeUntilCompleted(value =>
                {
                    if (condition(value))
                    {
                        if (observableStatusProvider.Errored)
                            subject.Error(observableWrapper.ErrorException);
                        else if (observableStatusProvider.HasValue)
                            subject.CompleteWith(observableWrapper.CurrentValue);
                        else
                            observable.SubscribeUntilCompleted(subject);
                    }
                },
                subject.Error,
                subject.Complete
            );
            return subject;
        }

        /// <summary>
        /// Creates a new observable that only emits values that pass the given filter.
        /// </summary>
        public static Observable<T> Filter<T>(this Observable<T> observable, Func<T, bool> filter)
        {
            var subject = new Subject<T>();
            observable.SubscribeUntilCompleted(value =>
                {
                    if (filter(value))
                        subject.Next(value);

                },
                subject.Error,
                subject.Complete
            );
            return subject;
        }
    }
}