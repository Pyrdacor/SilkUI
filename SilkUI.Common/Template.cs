using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SilkUI
{
    public abstract class Template
    {
        internal Component Component { get; private set; }
        public string Name { get; }
        private readonly List<ComponentBinder> _componentBinders = new List<ComponentBinder>();

        protected Template([CallerMemberName] string name = null)
        {
            Name = name ?? nameof(Template);
        }

        internal void Create(Component component)
        {
            Component = component;
            CreateFor(Component);
        }

        internal protected abstract void CreateFor(Component component);

        internal void Bind()
        {
            foreach (var componentBinder in _componentBinders)
                componentBinder.Bind(Component);
        }

        protected void Add(string componentTypeName, string id = null)
        {
            AddIf(componentTypeName, true, id);
        }

        protected void AddMultiple(string componentTypeName, int count, Func<int, string> idProvider = null)
        {
            AddMultiple(componentTypeName, new ValueSubject<int>(count), idProvider);
        }

        protected void AddMultiple(string componentTypeName, Observable<int> count, Func<int, string> idProvider = null)
        {
            _componentBinders.Add(new MultipleComponentBinder(count, componentTypeName, idProvider));
        }

        protected void AddForEach<T>(string componentTypeName, IEnumerable<T> collection, Func<T, string> idProvider = null)
        {
            AddForEach(componentTypeName, new ValueSubject<IEnumerable<T>>(collection), idProvider);
        }

        protected void AddForEach<T>(string componentTypeName, Observable<IEnumerable<T>> collection, Func<T, string> idProvider = null)
        {
            _componentBinders.Add(new MultipleComponentBinder<T>(collection, componentTypeName, idProvider));
        }

        protected AddIfElse AddIf(string componentTypeName, bool condition, string id = null)
        {
            return AddIf(componentTypeName, new ValueSubject<bool>(condition), id);
        }

        protected AddIfElse AddIf(string componentTypeName, Observable<bool> condition, string id = null)
        {
            var conditionalComponentBinder = new ConditionalComponentBinder(condition, componentTypeName, id);
            _componentBinders.Add(conditionalComponentBinder);
            return new AddIfElse(this, conditionalComponentBinder.GetElseObservable());
        }

        public class AddIfElse
        {
            private Template _template;
            private Observable<bool> _elseCondition;

            internal AddIfElse(Template template, Observable<bool> elseCondition)
            {
                _template = template;
                _elseCondition = elseCondition;
            }

            public AddIfElse ElseAddIf(string componentTypeName, bool condition, string id = null)
            {
                return ElseAddIf(componentTypeName, new ValueSubject<bool>(condition), id);
            }

            public AddIfElse ElseAddIf(string componentTypeName, Observable<bool> condition, string id = null)
            {
                var totalCondition = condition.Merge(_elseCondition, (a, b) => a && b);
                var conditionalComponentBinder = new ConditionalComponentBinder(totalCondition, componentTypeName, id);
                _template._componentBinders.Add(conditionalComponentBinder);
                return new AddIfElse(_template, conditionalComponentBinder.GetElseObservable());
            }

            public void ElseAdd(string componentTypeName, string id = null)
            {
                _template._componentBinders.Add(new ConditionalComponentBinder(_elseCondition, componentTypeName, id));
            }
        }
    }
}
