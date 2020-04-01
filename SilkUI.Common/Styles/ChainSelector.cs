using System;

namespace SilkUI
{
    internal enum ChainOperator
    {
        And,
        Or,
        Xor
    }

    internal class ChainSelector : Selector
    {
        internal override int Priority { get; }
        private Selector _prevSelector;
        private Selector _nextSelector;
        private ChainOperator _chainOperator;

        public ChainSelector(Selector prevSelector, Selector nextSelector, ChainOperator chainOperator)
        {
            if (prevSelector == null)
                throw new ArgumentNullException(nameof(prevSelector));
            if (nextSelector == null)
                throw new ArgumentNullException(nameof(nextSelector));

            Priority = Math.Max(prevSelector.Priority, nextSelector.Priority);
            _prevSelector = prevSelector;
            _nextSelector = nextSelector;
            _chainOperator = chainOperator;
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            return _chainOperator switch
            {
                ChainOperator.And => _prevSelector.MatchControl(control, path) &&
                    _nextSelector.MatchControl(control, path),
                ChainOperator.Or => _prevSelector.MatchControl(control, path) ||
                    _nextSelector.MatchControl(control, path),
                ChainOperator.Xor => _prevSelector.MatchControl(control, path) !=
                    _nextSelector.MatchControl(control, path),
                _ => throw new ArgumentException("Invalid chain operator.")
            };
        }

        public override bool Equals(Selector other)
        {
            var otherChainSelector = other as ChainSelector;

            return otherChainSelector != null &&
                _prevSelector == otherChainSelector._prevSelector &&
                _nextSelector == otherChainSelector._nextSelector &&
                _chainOperator == otherChainSelector._chainOperator;
        }

        protected override int CalculateHashCode()
        {
            int hash = 17;

            hash = hash * 23 + _prevSelector.GetHashCode();
            hash = hash * 23 + _nextSelector.GetHashCode();

            return hash;
        }
    }
}