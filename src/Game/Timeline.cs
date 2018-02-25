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
        public HitTestManager HitTest { get; private set; } = new HitTestManager();
        public int Length => _frames.Count;

        private ResourceSync _framesync = new ResourceSync();
        private AutoArray<Rider> _frames = new AutoArray<Rider>(2 * 60 * 40);
        private Track _track;
        private List<LineTrigger> _activetriggers = null;
        public Timeline(Track track)
        {
            _track = track;
            var start = _track.GetStart();
            _savedcells.BaseGrid = _track.Grid;
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
        public RiderFrame ExtractFrame(int frame, int iteration = 6)
        {
            Rider rider;
            List<int> diagnosis;
            bool isiteration = iteration != 6 && frame > 0;

            if (isiteration)
            {
                rider = GetFrame(frame - 1);
            }
            else
            {
                rider = GetFrame(frame);
            }
            using (_track.Grid.Sync.AcquireRead())
            {
                diagnosis = rider.Diagnose(
                    _track.Grid,
                    _track.Bones,
                    null,
                    Math.Min(6, iteration + 1));

                if (isiteration)
                {
                    rider = rider.Simulate(
                        _track.Grid,
                        _track.Bones,
                        null,
                        null,
                        iteration);
                }
            }
            return new RiderFrame(frame, rider, diagnosis, iteration);
        }
        public Rider GetFrame(int frame)
        {
            using (_framesync.AcquireWrite())
            {
                int start;
                int count;
                int invalid;
                lock (_changesync)
                {
                    invalid = _first_invalid_frame;
                    start = Math.Min(_frames.Count, invalid);
                    count = frame - (start - 1);
                    _first_invalid_frame = Math.Max(invalid, frame + 1);
                }
                if (count > 0)
                {
                    ThreadUnsafeRunFrames(start, count);
                }
                return _frames[frame];
            }
        }
        private void ThreadUnsafeRunFrames(int start, int count)
        {
            var steps = new Rider[count];
            var changedcollision = new List<HashSet<int>>(count);
            Rider current = _frames[start - 1];
            int framecount = _frames.Count;
            lock (_changesync)
            {
                // we use the savedcells buffer exactly so runframes can
                // be completely consistent with the track state at the
                // time of running

                // we could also get a lock on the track grid, but that would
                // block user input on the track until we finish running.
                _savedcells.Clear();
            }
            for (int i = 0; i < count; i++)
            {
                int currentframe = start + i;
                HashSet<int> collisions = new HashSet<int>();
                current = current.Simulate(_savedcells, _track.Bones, _activetriggers, collisions);
                steps[i] = current;
                if (currentframe >= framecount)
                    HitTest.AddFrame(collisions);
                else
                    changedcollision.Add(collisions);
            }

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
        }

    }
}