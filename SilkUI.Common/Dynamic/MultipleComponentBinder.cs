using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkUI
{
    internal class MultipleComponentBinder<T> : ComponentBinder
    {
        private Observable<IEnumerable<T>> _values;
        private readonly List<Component> _boundComponents = new List<Component>();
        private string _componentTypeName;
        private Func<T, string> _componentIdProvider;

        public MultipleComponentBinder(Observable<IEnumerable<T>> values, string componentTypeName, Func<T, string> componentIdProvider)
        {
            this._values = values;
            this._componentTypeName = componentTypeName;
            this._componentIdProvider = componentIdProvider;
        }

        public override void Bind(Component parentComponent)
        {
            _values.Subscribe(result =>
            {
                foreach (var boundComponent in _boundComponents)
                {
                    boundComponent.DestroyControl(); // TODO: recheck if this is the right way and if this even work
                }

                _boundComponents.Clear();

                foreach (var value in result)
                {
                    var boundComponent = ComponentManager.InitializeComponent(_componentTypeName,
                        _componentIdProvider == null ? null : _componentIdProvider(value));
                    boundComponent.AddTo(parentComponent);
                    _boundComponents.Add(boundComponent);
                }
            });
        }
    }

    internal class MultipleComponentBinder : MultipleComponentBinder<int>
    {
        public MultipleComponentBinder(Observable<int> count, string componentTypeName, Func<int, string> componentIdProvider)
            : base(count.Map(c => Enumerable.Range(0, c)), componentTypeName, componentIdProvider)
        {

        }
    }
}
