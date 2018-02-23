//
//  Track.cs
//
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
using linerider.Lines;
using System.Diagnostics;

namespace linerider.Game
{
    public partial class Timeline
    {
        public int Length
        {
            get
            {
                return _frames.Count;
            }
        }
        private ResourceSync _framesync = new ResourceSync();
        private AutoArray<Rider> _frames = new AutoArray<Rider>(5 * 60 * 40);
        private Track _track;
        private List<LineTrigger> _activetriggers = null;
        public HitTestManager HitTest { get; private set; } = new HitTestManager();
        public Timeline(Track track)
        {
            _track = track;
            Restart(track.GetStart());
        }
        public void Restart(Rider state)
        {
            using (_framesync.AcquireWrite())
            {
                _frames.Empty();
                _frames.Add(state);
            }
        }
        public Rider GetFrame(int frame)
        {
            int invalid;
            lock (_changesync)
            {
                invalid = _first_invalid_frame;
                _first_invalid_frame = Math.Max(invalid, frame + 1);
                int start = Math.Min(_frames.Count, invalid);
                int count = frame - (start - 1);
                if (count > 0)
                {
                    RunFrames(start, count);
                }
            }
            return _frames[frame];
        }
        private void RunFrames(int start, int count)
        {
            Rider[] steps = new Rider[count];
            List<HashSet<int>> changedcollision = new List<HashSet<int>>(count);
            Rider current = _frames[start - 1];
            var framecount = _frames.Count;
            for (int i = 0; i < count; i++)
            {
                int currentframe = start + i;
                HashSet<int> collisions = new HashSet<int>();
                current = current.Simulate(_track.Grid, _track.Bones, _activetriggers, collisions);
                steps[i] = current;
                if (currentframe >= framecount)
                    HitTest.AddFrame(collisions);
                else
                    changedcollision.Add(collisions);
            }
            using (_framesync.AcquireWrite())
            {
                if (start + count > framecount)
                {
                    _frames.EnsureCapacity(start + count);
                    _frames.UnsafeSetCount(start + count);
                }
                for (int i = 0; i < steps.Length; i++)
                {
                    _frames.unsafe_array[start + i] = steps[i];
                }
                if (changedcollision.Count != 0)
                    HitTest.ChangeFrames(start, changedcollision);
                if (start <= _first_invalid_frame)
                {
                    _first_invalid_frame = start + count;
                }
            }
        }
    }
}