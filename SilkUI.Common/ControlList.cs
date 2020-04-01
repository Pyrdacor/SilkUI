using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace SilkUI
{
    public sealed class ControlList : IList<Control>, INotifyCollectionChanged
    {
        private Control _parent;
        private List<Control> _controls = new List<Control>();

        internal ControlList(Control parent)
        {
            _parent = parent;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void InvokeCollectionChange(NotifyCollectionChangedAction action,
            int startIndex, int count, List<Control> controls = null)
        {
            NotifyCollectionChangedEventArgs args;
            List<Control> changedControls = controls ?? _controls.GetRange(startIndex, count);

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    args = new NotifyCollectionChangedEventArgs(action, changedControls, startIndex);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid collection changed action: {action}");
            }

            CollectionChanged?.Invoke(_parent, args);
        }

        public Control this[int index]
        {
            get => _controls[index];
            set
            {
                if (_controls[index] != value)
                {
                    _controls[index] = value;
                    InvokeCollectionChange(NotifyCollectionChangedAction.Replace, index, 1);
                }
            }
        }

        public Control this[string id]
        {
            get => _controls.FirstOrDefault(c => c.Id == id);
        }
        public int Count => _controls.Count;
        public bool IsReadOnly => false;

        public void Add(IEnumerable<Control> controls)
        {
            _controls.AddRange(controls);
            int count = controls.Count();
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                _controls.Count - count, count);
        }

        public void Add(params Control[] controls)
        {
            if (controls.Length == 0)
                return;

            _controls.AddRange(controls);
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                _controls.Count - controls.Length, controls.Length);
        }

        public void Add(Control control)
        {
            _controls.Add(control);
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                _controls.Count - 1, 1);
        }

        public void Insert(int index, IEnumerable<Control> controls)
        {
            _controls.InsertRange(index, controls);
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                index, controls.Count());
        }

        public void Insert(int index, params Control[] controls)
        {
            if (controls.Length == 0)
                return;

            _controls.InsertRange(index, controls);
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                index, controls.Length);
        }

        public void Insert(int index, Control control)
        {
            _controls.Insert(index, control);
            InvokeCollectionChange(NotifyCollectionChangedAction.Add,
                index, 1);
        }

        public bool Remove(Control control)
        {
            int index = _controls.IndexOf(control);
            if (_controls.Remove(control))
            {
                CollectionChanged?.Invoke(_parent, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, control, index
                ));
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var control = _controls[index];
            _controls.RemoveAt(index);
            CollectionChanged?.Invoke(_parent, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, control, index
            ));
        }

        public void RemoveRange(int index, int count)
        {
            var removedControls = _controls.GetRange(index, count);
            _controls.RemoveRange(index, count);
            InvokeCollectionChange(NotifyCollectionChangedAction.Remove, index, count, removedControls);
        }

        public void Clear()
        {
            _controls.Clear();
            CollectionChanged?.Invoke(_parent, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset
            ));
        }

        public int IndexOf(Control control)
        {
            return _controls.IndexOf(control);
        }

        public bool Contains(Control control)
        {
            return _controls.Contains(control);
        }

        public void CopyTo(Control[] array, int arrayIndex)
        {
            _controls.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Control> GetEnumerator()
        {
            return _controls.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (_controls as IEnumerable).GetEnumerator();
        }
    }
}
