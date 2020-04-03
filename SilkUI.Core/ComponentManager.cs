using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Input;
using Silk.NET.Windowing.Common;

namespace SilkUI
{
    public static class ComponentManager
    {
        private static readonly Dictionary<string, List<string>> componentTypesByName = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, Type> componentTypesByFullName = new Dictionary<string, Type>();

        public static void Run(Type rootComponentType, IView view,
            IControlRendererFactory controlRendererFactory)
        {
            if (!rootComponentType.IsSubclassOf(typeof(RootComponent)))
                throw new ArgumentException($"The given type is not a subclass of `{nameof(RootComponent)}`.");

            var rootComponent = Component.Create(rootComponentType, null, true) as RootComponent;

            // find and register all component types
            foreach (var type in FindTypes((type) => type.IsSubclassOf(typeof(Component))))
            {
                componentTypesByFullName.Add(type.FullName, type);

                if (!componentTypesByName.ContainsKey(type.Name))
                    componentTypesByName.Add(type.Name, new List<string>());

                componentTypesByName[type.Name].Add(type.FullName);
            }

            view.Load += () =>
            {
                var controlRenderer = controlRendererFactory.CreateControlRenderer(view);
                rootComponent.SetControlRenderer(controlRenderer);
                rootComponent.SetInputEventManager(new InputEventManager(view.CreateInput()));

                // init root component and its view
                rootComponent.InitControl();

                view.Render += (double deltaTime) =>
                {
                    view.MakeCurrent();
                    rootComponent.ControlRenderer.Init();
                    Update(rootComponent, deltaTime);
                    rootComponent.ControlRenderer.Render();
                    view.SwapBuffers();
                };
            };
            
            view.Run();

            // destroy root component view
            rootComponent.DestroyView();
        }

        internal static Component InitializeComponent(string name, string id)
        {
            if (componentTypesByFullName.ContainsKey(name))
                return Component.Create(componentTypesByFullName[name], id, false);

            if (!componentTypesByName.ContainsKey(name))
                throw new ArgumentException($"Unknown component {name}.");

            var possibleTypes = componentTypesByName[name];

            if (possibleTypes.Count != 1) // TODO: add the fully qualified names into the message
                throw new ArgumentException($"Component with type name {name} exists more than once. Specify it with fully qualified name.");

            return Component.Create(componentTypesByFullName[possibleTypes[0]], id, false);
        }

        private static void Update(Component rootComponent, double deltaTime)
        {
            // TODO: process UI events
            // TODO: draw / update UI
            rootComponent.RenderControl();
        }

        private static IEnumerable<Type> FindTypes(Func<Type, bool> condition)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => a.GetTypes())
                    .Where(t => condition(t));
        }
    }
}