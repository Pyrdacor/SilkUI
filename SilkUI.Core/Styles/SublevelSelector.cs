using System;

namespace SilkUI
{
    internal class SublevelSelector : Selector
    {
        internal override int Priority { get; }
        private Selector _parentSelector;
        private Selector _selector;

        public SublevelSelector(Selector parentSelector, Selector selector)
        {
            if (parentSelector == null)
                throw new ArgumentNullException(nameof(parentSelector));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            Priority = parentSelector.Priority + selector.Priority;
            _parentSelector = parentSelector;
            _selector = selector;
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            if (control.Parent == null)
                return false;
            
            return _parentSelector.MatchControl(control.Parent, path.Prev) &&
                _selector.MatchControl(control, path);
        }

        public override bool Equals(Selector other)
        {
            var otherSublevelSelector = other as SublevelSelector;

            return otherSublevelSelector != null &&
                _parentSelector == otherSublevelSelector._parentSelector &&
                _selector == otherSublevelSelector._selector;
        }

        protected override int CalculateHashCode()
        {
            return HashCode.Combine(_parentSelector, _selector);
        }
    }
}