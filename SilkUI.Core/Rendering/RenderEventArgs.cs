using System;

namespace SilkUI
{
    public class RenderEventArgs : EventArgs
    {
        public ControlRenderer Renderer { get; }

        internal RenderEventArgs(ControlRenderer renderer)
        {
            Renderer = renderer;
        }
    }

    public delegate void RenderEventHandler(object sender, RenderEventArgs args);
}