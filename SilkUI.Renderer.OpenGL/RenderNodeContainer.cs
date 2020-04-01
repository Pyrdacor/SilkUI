using System.Collections.Generic;

namespace SilkUI.Renderer.OpenGL
{
    internal class RenderNodeContainer : IRenderNode
    {
        private readonly List<RenderNode> _children = new List<RenderNode>();

        public void AddChild(RenderNode child)
        {
            if (!_children.Contains(child))
                _children.Add(child);
        }

        public void Delete()
        {
            foreach (var child in _children)
                child.Delete();

            _children.Clear();
        }
    }
}