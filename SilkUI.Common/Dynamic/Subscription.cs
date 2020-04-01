namespace SilkUI
{
    public class Subscription<T>
    {
        internal static readonly Subscription<T> Empty = new Subscription<T>(null, null);
        private Observable<T> _observable;
        private Subscriber<T> _subscriber;

        internal Subscription(Observable<T> observable, Subscriber<T> subscriber)
        {
            _observable = observable;
            _subscriber = subscriber;
        }

        public void Unsubscribe()
        {
            _observable?.Unsubscribe(_subscriber);
        }
    }
}