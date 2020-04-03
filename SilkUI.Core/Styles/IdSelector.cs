using System;
using System.Collections.Generic;

namespace SilkUI
{
    internal class IdSelector : Selector
    {
        internal override int Priority => 1000000;
        private List<string> _ids;

        public IdSelector(params string[] ids)
        {
            _ids = new List<string>(ids);
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            return control.Id == null ? false : _ids.Contains(control.Id);
        }

        public override bool Equals(Selector other)
        {
            var otherIdSelector = other as IdSelector;

            if (otherIdSelector == null || _ids.Count != otherIdSelector._ids.Count)
                return false;

            for (int i = 0; i < _ids.Count; ++i)
            {
                if (_ids[i] != otherIdSelector._ids[i])
                    return false;
            }

            return true;
        }

        protected override int CalculateHashCode()
        {
            int hash = 17;

            hash = hash * 23 + Priority.GetHashCode();

            foreach (var id in _ids)
                hash = hash * 23 + id.GetHashCode();

            return hash;
        }
    }
}
