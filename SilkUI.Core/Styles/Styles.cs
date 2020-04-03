using System;
using System.Linq;
using System.Collections.Generic;

namespace SilkUI
{
    public interface IStyles
    {
        void Add(Selector selector, Style style);
        void Add(Selector selector, Style style, Action<IStyles> subStyleBlock);
    }

    public abstract class Styles : IStyles
    {
        public class SubStyles : IStyles
        {
            private IStyles _parentStyles;
            private Selector _parentSelector;

            internal SubStyles(IStyles parentStyles, Selector parentSelector)
            {
                _parentStyles = parentStyles;
                _parentSelector = parentSelector;
            }

            public void Add(Selector selector, Style style)
            {
                _parentStyles.Add(_parentSelector.And(selector), style);
            }

            public void Add(Selector selector, Style style, Action<IStyles> subStyleBlock)
            {
                _parentStyles.Add(_parentSelector.And(selector), style, subStyleBlock);
            }
        }

        private readonly Dictionary<Selector, Style> _styles = new Dictionary<Selector, Style>();

        /// <summary>
        /// Searches for controls that match the given selector and
        /// then applies the given style to these controls.
        /// 
        /// The search is based on the current component as the
        /// root or starting point.
        /// </summary>
        /// <param name="selector">Control selector</param>
        /// <param name="style">Style to use for matched selections</param>
        public void Add(Selector selector, Style style)
        {
            // styles that are set later with the same selector will override previous styles
            if (_styles.ContainsKey(selector))
                _styles[selector] = MergeStyles(_styles[selector], style);
            else
                _styles[selector] = style;
        }

        /// <summary>
        /// Like <see cref="Add(Selector, Style)"/> but inside the <paramref name="subStyleBlock"/>
        /// additional styles can be added which then are restricted by the given
        /// <paramref name="selector"/>.
        /// 
        /// Example:
        /// 
        /// Add(Selector.ForId("someId"), new Style()
        /// {
        ///     BorderColor = "black"
        /// }, (subStyles) => {
        ///     subStyles.Add(Selector.ForClass("someClass"), new Style()
        ///     {
        ///         BorderColor = "red"
        ///     });
        /// });
        /// 
        /// This is the same as:
        /// 
        /// Add(Selector.ForId("someId"), new Style()
        /// {
        ///     BorderColor = "black"
        /// });
        /// Add(Selector.ForId("someId").And(Selector.ForClass("someClass")), new Style()
        /// {
        ///     BorderColor = "black"
        /// });
        /// </summary>
        /// <param name="selector">Main control selector</param>
        /// <param name="style">Style to use for matched selections</param>
        /// <param name="subStyleBlock">Action in which you can add substyles</param>
        public void Add(Selector selector, Style style, Action<IStyles> subStyleBlock)
        {
            // styles that are set later with the same selector will override previous styles
            if (_styles.ContainsKey(selector))
                _styles[selector] = MergeStyles(_styles[selector], style);
            else
                _styles[selector] = style;

            subStyleBlock?.Invoke(new SubStyles(this, selector));
        }

        private Style MergeStyles(Style oldStyle, Style newStyle)
        {
            var mergedStyle = new Style();

            CopyFields(oldStyle, mergedStyle);
            CopyFields(newStyle, mergedStyle);

            return mergedStyle;
        }

        private static void CopyFields(Style source, Style target)
        {
            Type type = typeof(Style);
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var value = field.GetValue(source);

                if (value != null)
                    field.SetValue(target, value);
            }
        }

        private static object GetStylePropertyValue(object parent, string name)
        {
            var type = parent.GetType();

            if (Util.CheckGenericType(type, typeof(Nullable<>)))
                type = type.GenericTypeArguments[0];

            int dotIndex = name.IndexOf(".");
            
            if (dotIndex != -1)
            {
                var newParent = type.GetField(name.Substring(0, dotIndex)).GetValue(parent);

                if (newParent == null)
                    return null;

                return GetStylePropertyValue(newParent, name.Substring(dotIndex + 1));
            }

            return type.GetField(name).GetValue(parent);
        }

        private static IEnumerable<KeyValuePair<string, object>> EnumerateFields(Style style)
        {
            return ControlStyle.StylePropertyNames
                .Select(name => new KeyValuePair<string, object>(name, GetStylePropertyValue(style, name)))
                .Where(result => result.Value != null);
        }

        internal bool Apply(Component component)
        {
            bool stylesApplied = false;
            var styleList = _styles.ToList();
            styleList.Sort(new StyleComparer()); // sort by selector priority
            var controlStylesInitialized = new List<Control>();

            foreach (var style in styleList)
            {
                var matchingControls = component.FindMatchingControls(component, style.Key);

                foreach (var control in matchingControls)
                {
                    if (!controlStylesInitialized.Contains(control))
                    {
                        controlStylesInitialized.Add(control);
                        control.Style.StartStyling();
                    }

                    foreach (var field in EnumerateFields(style.Value))
                    {
                        control.Style.SetStyleProperty(field.Key, field.Value);
                        stylesApplied = true;
                    }
                }
            }

            return stylesApplied;
        }

        private class StyleComparer : IComparer<KeyValuePair<Selector, Style>>
        {
            public int Compare(KeyValuePair<Selector, Style> lhs, KeyValuePair<Selector, Style> rhs)
            {
                // higher values win
                return rhs.Key.Priority.CompareTo(lhs.Key.Priority);
            }
        }
    }
}
