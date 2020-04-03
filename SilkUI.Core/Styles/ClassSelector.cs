using System.Collections.Generic;
using System.Linq;

namespace SilkUI
{
    internal class ClassSelector : Selector
    {
        internal override int Priority => 10000;
        private List<string> _classes;

        public ClassSelector(params string[] classes)
        {
            _classes = new List<string>(classes);
        }

        protected override bool Match(Control control, SelectorPathNode path)
        {
            if (control.Classes.Count == 0)
                return false;
                
            return _classes.Any(clazz => control.Classes.Contains(clazz));
        }

        public override bool Equals(Selector other)
        {
            var otherClassSelector = other as ClassSelector;

            if (otherClassSelector == null || _classes.Count != otherClassSelector._classes.Count)
                return false;

            for (int i = 0; i < _classes.Count; ++i)
            {
                if (_classes[i] != otherClassSelector._classes[i])
                    return false;
            }

            return true;
        }

        protected override int CalculateHashCode()
        {
            int hash = 17;

            hash = hash * 23 + Priority.GetHashCode();

            foreach (var clazz in _classes)
                hash = hash * 23 + clazz.GetHashCode();

            return hash;
        }
    }
}
