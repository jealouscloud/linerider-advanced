using OpenTK;
using System;
using System.Linq;
using System.Collections.Generic;
using linerider.Game;
using linerider.Rendering;
using System.Collections;
using System.Diagnostics;

namespace linerider
{
    /// <summary>
    /// A grid cell for the line rider simulation that puts lines
    /// with greater ids first. Newer = higher id, unless it's a scenery id
    /// Lines are guaranteed unique by ID, but collisions do not replace the 
    /// original or throw an exception
    /// </summary>
    public class LineContainer<T> : IEnumerable<T>, ICollection<T>
    where T : GameLine
    {
        LinkedList<T> _list = new LinkedList<T>();
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// return the node that would come right after line.id
        /// returns null if unsuccessful
        /// ex: if our state is 1 2 4 and line.id is 3, this function returns 4
        /// </summary>
        protected virtual LinkedListNode<T> FindNodeAfter(LinkedListNode<T> node, T line)
        {
            while (line.ID < node.Value.ID)
            {
                node = node.Next;
                if (node == null)
                {
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// Combines all unique lines in a cell in order
        /// </summary>
        /// <param name="cell"></param>
        public void Combine(LineContainer<T> cell)
        {
            if (this == cell)
                return;
            var node = _list.First;
            foreach (var line in cell)
            {
                if (node != null)
                {
                    node = FindNodeAfter(node, line);
                    if (node == null)
                        node = _list.AddLast(line);
                    else if (node.Value.ID != line.ID) // no redundant lines
                        _list.AddBefore(node, line);
                }
                else
                {
                    node = _list.AddFirst(line);
                }
            }
        }
        public void AddLine(T line)
        {
            var node = _list.First;
            if (node != null)
            {
                node = FindNodeAfter(node, line);
                if (node == null)
                {
                    _list.AddLast(line);
                }
                else
                {
                    if (node.Value.ID != line.ID)
                        _list.AddBefore(node, line);
                    else
                        Debug.WriteLine("Line ID collision in line container");
                }
            }
            else
            {
                _list.AddFirst(line);
            }
        }
        public void RemoveLine(int lineid)
        {
            var node = _list.First;
            while (node != null)
            {
                if (node.Value.ID == lineid)
                {
                    _list.Remove(node);
                    return;
                }
                node = node.Next;
            }
            throw new Exception("Line was not found in the chunk");
        }
        public LineContainer<T> Clone()
        {
            var ret = new LineContainer<T>();
            foreach (var l in this)
            {
                ret.AddLine(l);
            }
            return ret;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
            //AddLine(item);
        }
        void ICollection<T>.Clear()
        {
            _list.Clear();
        }
        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
            // return _list.Contains(item);
        }
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
            //      _list.CopyTo(array, arrayIndex);
        }
        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
            //      return _list.Remove(item);
        }
    }
}