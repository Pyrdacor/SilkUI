namespace SilkUI
{
    public class EventArgs : System.EventArgs
    {
        public new static readonly EventArgs Empty = new EventArgs();
    }

    public delegate void EventHandler(Control sender, EventArgs args);
}
