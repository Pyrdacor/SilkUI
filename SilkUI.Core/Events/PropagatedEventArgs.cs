namespace SilkUI
{
    public class PropagatedEventArgs : EventArgs
    {
        public bool CancelPropagation { get; set; } = false;
    }

    public delegate void PropagatedEventHandler(Control sender, PropagatedEventArgs args);
}