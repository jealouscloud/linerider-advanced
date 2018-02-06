using OpenTK;
using System;
using System.Linq;
using System.Collections.Generic;
using linerider.Game;
using linerider.Rendering;
using linerider.Lines;
using System.Collections;

namespace linerider
{
    public class SimulationCell : SimulationCell<StandardLine>
    {
    }
    /// <summary>
    /// A grid cell for the line rider simulation that puts lines with larger IDs first
    /// </summary>
    public class SimulationCell<T> : IEnumerable<T>
    where T : Line
    {
        LinkedList<T> _list = new LinkedList<T>();
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }
        /// <summary>
        /// Combines all unique lines in a cell in order
        /// </summary>
        /// <param name="cell"></param>
        public void Combine(SimulationCell<T> cell)
        {
            var node = _list.First;
            foreach (var line in cell)
            {
                // scenery line ids dont matter
                if (node != null && line.ID >= 0)
                {
                    while (line.ID < node.Value.ID)
                    {
                        node = node.Next;
                        if (node == null)
                        {
                            break;
                        }
                    }
                    if (node == null)
                        _list.AddLast(line);
                    else if (node.Value != line) // no redundant lines
                        _list.AddBefore(node, line);
                }
                else
                {
                    _list.AddLast(line);
                }
            }
            int last = 0;
            foreach (var v in this)
            {
                if (v.ID < last && v.ID >= 0)
                    throw new Exception("Unacceptable combine [ remove this check before release ]");
            }
        }
        public void AddLine(T line)
        {
            var node = _list.First;
            if (node != null)
            {
                while (line.ID < node.Value.ID)
                {
                    node = node.Next;
                    if (node == null)
                    {
                        _list.AddLast(line);
                        return;
                    }
                }
                _list.AddBefore(node, line);
            }
            else
            {
                _list.AddFirst(line);
            }
        }
        public void RemoveLine(T line)
        {
            if (!_list.Remove(line))
                throw new Exception("Line was not found in the chunk");
        }
        public SimulationCell<T> Clone()
        {
            var ret = new SimulationCell<T>();
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
    }
}