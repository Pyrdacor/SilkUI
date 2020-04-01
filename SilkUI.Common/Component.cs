using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilkUI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TemplateAttribute : Attribute
    {
        internal Type TemplateType { get; }

        public TemplateAttribute(Type templateType)
        {
            TemplateType = templateType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class StylesAttribute : Attribute
    {
        internal Type StylesType { get; }

        public StylesAttribute(Type stylesType)
        {
            StylesType = stylesType;
        }
    }

    public abstract class Component : ContainerControl
    {
        private Template _template;
        private Styles _styles;
        private bool _viewInitialized = false;

        protected Component()
            : base(null)
        {

        }

        internal override void InitView()
        {
            _template.CreateFor(this);
            _template.Bind();
            _styles.Apply(this);
            Invalidate();
            _viewInitialized = true;
        }

        internal override void DestroyView()
        {
            // TODO remove from renderer
            _viewInitialized = false;
        }

        internal override void CheckStyleChanges()
        {
            if (_viewInitialized)
            {
                Parent?.CheckStyleChanges();
            
                if (_styles.Apply(this))
                    Invalidate();
            }
        }

        internal static Component Create(Type type, string id, bool root)
        {
            var templateAttribute = type.GetCustomAttribute(typeof(TemplateAttribute), false);

            if (templateAttribute == null)
                throw new InvalidOperationException($"Component `{type.Name}` needs the attribute `Template`.");

            var stylesAttribute = type.GetCustomAttribute(typeof(StylesAttribute), false);

            if (stylesAttribute == null)
                throw new InvalidOperationException($"Component `{type.Name}` needs the attribute `Styles`.");

            var templateType = (templateAttribute as TemplateAttribute).TemplateType;
            var stylesType = (stylesAttribute as StylesAttribute).StylesType;

            if (!templateType.IsSubclassOf(typeof(Template)))
                throw new InvalidOperationException($"Component template type {templateType.Name} is not derived from class `Template`.");
            if (!stylesType.IsSubclassOf(typeof(Styles)))
                throw new InvalidOperationException($"Component styles type {stylesType.Name} is not derived from class `Styles`.");

            Component component = root ? TryTypeCreation<RootComponent>(type) : TryTypeCreation<Component>(type);

            if (component == null)
                throw new InvalidOperationException($"Type {type.Name} is not derived from class `Component`.");

            component.Id = id;
            component._template = TryTypeCreation<Template>(templateType);
            component._styles = TryTypeCreation<Styles>(stylesType);

            return component;
        }

        private static T TryTypeCreation<T>(Type type) where T : class
        {
            try
            {
                return (T)Activator.CreateInstance(type);
            }
            catch (Exception ex)            
            {                
                if (ex is MissingMemberException || ex is MissingMethodException ||
                    ex is MemberAccessException || ex is MethodAccessException)
                {
                    throw new InvalidOperationException($"There is no public parameterless constructor in type {type.Name}.");
                }
                else if (ex is TargetInvocationException)
                {
                    throw new InvalidOperationException($"Constructor of type {type.Name} threw an exception. See inner exception for details.", ex);
                }

                throw;
            }
        }

        internal IEnumerable<Control> FindMatchingControls(Component searchRoot, Selector selector)
        {
            return FindMatchingControls(searchRoot, null, selector);
        }

        private IEnumerable<Control> FindMatchingControls(Component searchRoot, SelectorPathNode path, Selector selector)
        {
            path = new SelectorPathNode() { Prev = path, Control = searchRoot };

            foreach (var control in Children)
            {
                if (control is Component)
                {
                    var component = control as Component;

                    foreach (var subControl in component.FindMatchingControls(component, path, selector))
                        yield return subControl;
                }

                if (selector.MatchControl(control, path))
                    yield return control;
            }
        }
    }
}
