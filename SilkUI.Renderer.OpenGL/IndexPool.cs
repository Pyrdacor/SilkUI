using System;
using System.Collections.Generic;

namespace SilkUI.Renderer.OpenGL
{
    internal class IndexPool
    {
        private readonly List<int> _releasedIndices = new List<int>();
        private int _firstFree = 0;

        public int AssignNextFreeIndex(out bool reused)
        {
            if (_releasedIndices.Count > 0)
            {
                reused = true;

                int index = _releasedIndices[0];

                _releasedIndices.RemoveAt(0);

                return index;
            }

            reused = false;

            if (_firstFree == int.MaxValue)
            {
                throw new Exceptions.InsufficientResourcesException("No free index available.");
            }

            return _firstFree++;
        }

        public void UnassignIndex(int index)
        {
            _releasedIndices.Add(index);
        }

        public bool AssignIndex(int index)
        {
            if (_releasedIndices.Contains(index))
            {
                _releasedIndices.Remove(index);
                return true;
            }

            if (index == _firstFree)
                ++_firstFree;

            return false;
        }
    }
}
