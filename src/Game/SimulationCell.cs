using OpenTK;
using System;
using System.Linq;
using System.Collections.Generic;
using linerider.Game;
using linerider.Drawing;

namespace linerider
{
    public class SimulationCell : LinkedList<StandardLine>
    {
        public void AddLine(StandardLine line)
        {
            var f = First;
            if (f != null)
            {
                while (line.ID < f.Value.ID)
                {
                    f = f.Next;
                    if (f == null)
                    {
                        AddLast(line);
                        return;
                    }
                }
                AddBefore(f, line);
            }
            else
            {
                AddFirst(line);
            }
        }
        public void RemoveLine(StandardLine line)
        {
            if (!Remove(line))
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
    }
}