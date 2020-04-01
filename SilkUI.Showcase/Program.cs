using System;
using System.Drawing;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using SilkUI.Controls;

namespace SilkUI.Showcase
{
    class MyTemplate : Template
    {
        protected override void CreateFor(Component component)
        {
            new Panel("panel").AddTo(component);
            new Button("foo").WithClasses("bar").AddTo(component);                        
        }
    }

    class MyStyles : Styles
    {
        public MyStyles()
        {
            Add(Selector.ForId("foo"), new Style()
            {
                Border = new BorderStyle {
                    Color = "green"
                },
                BorderSize = 1
            }, (subStyles) => {
                // substyles
                subStyles.Add(Selector.ForClass("bar"), new Style()
                {
                    BorderSize = 8
                });
            });
            Add(Selector.ForType(typeof(Panel)), new Style()
            {
                BackgroundColor = Color.Beige,
                BorderSize = 1,
                BorderColor = "black",
                BorderLineStyle = BorderLineStyle.Solid,
                ShadowBlurRadius = 10,
                ShadowSpreadRadius = -5,
                ShadowColor = Color.Red,
                ShadowXOffset = 5,
                ShadowYOffset = 5,
                ShadowVisible = true
            });
            Add(Selector.ForType(typeof(Panel)).WhenHovered(), new Style()
            {
                BackgroundColor = Color.Red,
                BorderSize = 12,
                BorderColor = "orange",
                BorderLineStyle = BorderLineStyle.Inset
            });
        }
    }

    [Template(typeof(MyTemplate))]
    [Styles(typeof(MyStyles))]
    class MyComponent : RootComponent
    {
        protected override void OnAfterViewInit()
        {
            // Test output
            Console.WriteLine("Id: " + Children[0].Id);

            var panel = this.Children["panel"];

            panel.X = 210;
            panel.Y = 210;
            panel.Width = 150;
            panel.Height = 120;

            var button = this.Children["foo"];

            button.X = 100;
            button.Y = 40;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = new WindowOptions(true, true, new Point(20, 40),
                new Size(1024, 768), 60, 60, GraphicsAPI.Default, "ShowCase",
                WindowState.Normal, WindowBorder.Fixed, VSyncMode.Off, int.MaxValue,
                false, new VideoMode(60));
            var window = Window.Create(options);

            ComponentManager.Run(typeof(MyComponent), window, new Renderer.OpenGL.ControlRendererFactory());
        }
    }
}
