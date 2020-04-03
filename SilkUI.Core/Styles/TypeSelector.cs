using System;
using System.Collections.Generic;

namespace SilkUI
{
    internal class TypeSelector : Selector
    {
        internal override int Priority => 100;
        private List<Type> _types;

        public TypeSelector(params Type[] types)
        {
            _types = new List<Type>(types);
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            return _types.Contains(control.GetType());
        }

        public override bool Equals(Selector other)
        {
            var otherIdSelector = other as TypeSelector;

            if (otherIdSelector == null || _types.Count != otherIdSelector._types.Count)
                return false;

            for (int i = 0; i < _types.Count; ++i)
            {
                if (_types[i] != otherIdSelector._types[i])
                    return false;
            }

            return true;
        }

        protected override int CalculateHashCode()
        {
            int hash = 17;

            hash = hash * 23 + Priority.GetHashCode();

            foreach (var type in _types)
                hash = hash * 23 + type.GetHashCode();

            return hash;
        }
    }
}
