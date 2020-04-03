namespace SilkUI
{
    internal class ConditionalComponentBinder : ComponentBinder
    {
        private Component _boundComponent;
        private Observable<bool> _condition;
        private string _componentTypeName;
        private string _componentId;

        public ConditionalComponentBinder(Observable<bool> condition, string componentTypeName, string componentId)
        {
            _condition = condition;
            _componentTypeName = componentTypeName;
            _componentId = componentId;
        }

        public Observable<bool> GetElseObservable()
        {
            return _condition.Map(c => !c);
        }

        public override void Bind(Component parentComponent)
        {
            _condition.Subscribe(result =>
            {
                if (result)
                {
                    if (_boundComponent == null)
                        _boundComponent = ComponentManager.InitializeComponent(_componentTypeName, _componentId);
                    
                    _boundComponent.AddTo(parentComponent);
                }
                else
                {
                    if (_boundComponent != null)
                    {
                        _boundComponent.DestroyControl();
                        _boundComponent = null;
                    }
                }
            });
        }
    }
}