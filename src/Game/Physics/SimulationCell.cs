using OpenTK;
using System;
using System.Linq;
using System.Collections.Generic;
using linerider.Game;
using linerider.Rendering;
using System.Collections;

namespace linerider
{
    public class SimulationCell : LineContainer<StandardLine>
    {
        public SimulationCell FullClone()
        {
            var ret = new SimulationCell();
            foreach (var l in this)
            {
                ret.AddLine((StandardLine)l.Clone());
            }
            return ret;
        }
    }
}