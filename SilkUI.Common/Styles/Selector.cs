using System;
using System.Runtime.CompilerServices;

namespace SilkUI
{
    // TODO: do we still need this since SublevelSelector is there? maybe with string-format paths?
    public class SelectorPathNode
    {
        public SelectorPathNode Prev = null;
        public Control Control = null;
    }

    public abstract class Selector : IEquatable<Selector>
    {
        internal abstract int Priority { get; }
        public string Name { get; }

        protected Selector([CallerMemberName] string name = null)
        {
            Name = name ?? nameof(Selector);
        }

        internal bool MatchControl(Control control, SelectorPathNode path) => Match(control, path);

        protected abstract bool Match(Control control, SelectorPathNode path);

        public static Selector ForType(params Type[] type)
        {
            return new TypeSelector(type);
        }

        public static Selector ForId(params string[] id)
        {
            return new IdSelector(id);
        }

        public static Selector ForClass(params string[] clazz)
        {
            return new ClassSelector(clazz);
        }

        public abstract bool Equals(Selector other);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return this.Equals((Selector)obj);
        }

        protected abstract int CalculateHashCode();

        public override int GetHashCode()
        {
            return CalculateHashCode();
        }

        public static bool operator ==(Selector lhs, Selector rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
                return Object.ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Selector lhs, Selector rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
                return !Object.ReferenceEquals(rhs, null);

            return !(lhs == rhs);
        }
    }

    public static class SelectorExtensions
    {
        public static Selector And(this Selector selector, Selector other)
        {
            return new ChainSelector(selector, other, ChainOperator.And);
        }

        public static Selector Or(this Selector selector, Selector other)
        {
            return new ChainSelector(selector, other, ChainOperator.Or);
        }

        public static Selector Xor(this Selector selector, Selector other)
        {
            return new ChainSelector(selector, other, ChainOperator.Xor);
        }

        public static Selector Not(this Selector selector, Selector other)
        {
            return new NegateSelector(selector);
        }

        public static Selector Child(this Selector selector, Selector childSelector)
        {
            return new SublevelSelector(selector, childSelector);
        }

        // TODO: ancestor, etc

        public static Selector When(this Selector selector, bool value)
        {
            return When(selector, (control) => value);
        }

        public static Selector When(this Selector selector, Func<Control, bool> condition)
        {
            return new ConditionalSelector(selector, condition);
        }

        public static Selector When(this Selector selector, Func<Control, SelectorPathNode, bool> condition)
        {
            return new ConditionalSelector(selector, condition);
        }

        public static Selector WhenEnabled(this Selector selector)
        {
            return WhenEnabledIs(selector, true);
        }

        public static Selector WhenDisabled(this Selector selector)
        {
            return WhenEnabledIs(selector, false);
        }

        public static Selector WhenEnabledIs(this Selector selector, bool value)
        {
            return StateSelector.CreateEnabledStateSelector(selector, value);
        }

        public static Selector WhenFocused(this Selector selector)
        {
            return WhenFocusedIs(selector, true);
        }

        public static Selector WhenNotFocused(this Selector selector)
        {
            return WhenFocusedIs(selector, false);
        }

        public static Selector WhenFocusedIs(this Selector selector, bool value)
        {
            return StateSelector.CreateFocusedStateSelector(selector, value);
        }

        public static Selector WhenHovered(this Selector selector)
        {
            return WhenHoveredIs(selector, true);
        }

        public static Selector WhenNotHovered(this Selector selector)
        {
            return WhenHoveredIs(selector, false);
        }

        public static Selector WhenHoveredIs(this Selector selector, bool value)
        {
            return StateSelector.CreateHoveredStateSelector(selector, value);
        }
    }
}
