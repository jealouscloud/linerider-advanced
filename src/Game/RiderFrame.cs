//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using linerider.Rendering;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using linerider.Game;
using linerider.Utils;
using System.Diagnostics;

namespace linerider.Game
{
    /// <summary>
    /// Full information on a frame.
    /// </summary>
    public class RiderFrame
    {
        public int FrameID;
        public Rider State;
        public List<int> Diagnosis;
        public int IterationID = 6;
        public RiderFrame()
        {
        }
        public RiderFrame(int frameid, Rider state, List<int> diagnosis, int iterationid)
        {
            FrameID = frameid;
            State = state;
            Diagnosis = diagnosis;
            IterationID = iterationid;
        }
    }
}