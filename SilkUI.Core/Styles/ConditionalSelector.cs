using System;

namespace SilkUI
{
    using Condition = Func<Control, SelectorPathNode, bool>;

    internal class ConditionalSelector : Selector
    {
        private static readonly Condition TrueCondition = (control, path) => true;

        internal override int Priority { get; }
        private readonly Selector _parentSelector;
        private Func<Control, SelectorPathNode, bool> _condition = null;

        public ConditionalSelector(Selector parentSelector, Func<Control, bool> condition)
        {
            Priority = parentSelector.Priority + 1;
            _parentSelector = parentSelector;
            _condition = condition == null ? TrueCondition : (control, path) => condition(control);
        }

        public ConditionalSelector(Selector parentSelector, Condition condition)
        {
            Priority = parentSelector.Priority + 1;
            _condition = condition ?? TrueCondition;
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            return _parentSelector.MatchControl(control, path) && _condition(control, path);
        }

        public override bool Equals(Selector other)
        {
            if (other == null)
                return false;

            return Object.ReferenceEquals(this, other);
        }

        protected override int CalculateHashCode()
        {
            int hash = 17;

            hash = hash * 23 + Priority.GetHashCode();
            hash = hash * 23 + _condition.GetHashCode();

            return hash;
        }
    }
}