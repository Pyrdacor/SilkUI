using System;

namespace SilkUI
{
    using Condition = Func<Control, bool>;

    internal class StateSelector : ConditionalSelector
    {
        public StateSelector(Selector parentSelector, Condition condition)
            : base(parentSelector, condition)
        {

        }

        // Note: It does not make sense to provide a VisibileStateSelector as styles are only
        //       meaningful if the control is visible.

        internal static StateSelector CreateEnabledStateSelector(Selector parentSelector, bool enabled)
        {
            return new StateSelector(parentSelector, (control) => control.Enabled == enabled);
        }

        internal static StateSelector CreateHoveredStateSelector(Selector parentSelector, bool hovered)
        {
            return new StateSelector(parentSelector, (control) => control.Hovered == hovered);
        }

        internal static StateSelector CreateFocusedStateSelector(Selector parentSelector, bool focused)
        {
            return new StateSelector(parentSelector, (control) => control.Focused == focused);
        }
    }
}