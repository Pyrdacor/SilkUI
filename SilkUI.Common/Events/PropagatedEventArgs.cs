namespace SilkUI
{
    public class PropagatedEventArgs : System.EventArgs
    {
        public bool CancelPropagation { get; set; } = false;
    }

    public delegate void PropagatedEventHandler(Control sender, PropagatedEventArgs args);
}