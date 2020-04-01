namespace SilkUI
{
    public abstract class RootComponent : Component
    {        
        private ControlRenderer _controlRenderer;
        private InputEventManager _inputEventManager;

        internal override ControlRenderer ControlRenderer => _controlRenderer;
        internal override InputEventManager InputEventManager => _inputEventManager;

        internal void SetControlRenderer(IControlRenderer controlRenderer)
        {
            _controlRenderer = new ControlRenderer(controlRenderer);
        }

        internal void SetInputEventManager(InputEventManager inputEventManager)
        {
            _inputEventManager = inputEventManager;
        }
    }
}