using OpenTK;
using System;
using System.Linq;
using System.Collections.Generic;
using linerider.Game;
using linerider.Drawing;
using linerider.Lines;
using System.Collections;

namespace linerider
{
    public class SimulationCell : IEnumerable<StandardLine>
    {
        LinkedList<StandardLine> _list = new LinkedList<StandardLine>();
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }
        public void AddLine(StandardLine line)
        {
            var f = _list.First;
            if (f != null)
            {
                while (line.ID < f.Value.ID)
                {
                    f = f.Next;
                    if (f == null)
                    {
                        _list.AddLast(line);
                        return;
                    }
                }
                _list.AddBefore(f, line);
            }
            else
            {
                _list.AddFirst(line);
            }
        }
        public void RemoveLine(StandardLine line)
        {
            if (!_list.Remove(line))
                throw new Exception("Line was not found in the chunk");
        }
        public SimulationCell Clone()
        {
            var ret = new SimulationCell();
            foreach(var l in this)
            {
                ret.AddLine(l);
            }
            return ret;
        }
        public IEnumerator<StandardLine> GetEnumerator()
        {
            return _list.GetEnumerator();
		}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}