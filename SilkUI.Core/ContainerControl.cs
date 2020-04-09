using System.Collections.Specialized;

namespace SilkUI
{
    public abstract class ContainerControl : Controls.Panel
    {
        public ControlList Children => base.InternalChildren;
        
        public event NotifyCollectionChangedEventHandler ChildrenChanged
        {
            add => Children.CollectionChanged += value;
            remove => Children.CollectionChanged -= value;
        }

        public ContainerControl(string id)
            : base(id)
        {

        }
    }
}
