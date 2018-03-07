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
using System.Diagnostics;

namespace linerider.Game
{
    public partial class Timeline
    {
        public HitTestManager HitTest { get; private set; } = new HitTestManager();

        private ResourceSync _framesync = new ResourceSync();
        private AutoArray<Rider> _frames = new AutoArray<Rider>(2 * 60 * 40);
        private Track _track;
        private List<LineTrigger> _activetriggers = null;
        public Timeline(Track track, List<LineTrigger> triggerlist)
        {
            _track = track;
            var start = _track.GetStart();
            _savedcells.BaseGrid = _track.Grid;
            Restart(track.GetStart());
            _activetriggers = triggerlist;
        }
        public void Restart(Rider state)
        {
            using (_framesync.AcquireWrite())
            {
                HitTest.Reset();
                _frames.Clear();
                _frames.Add(state);
            }
        }
        public RiderFrame ExtractFrame(int frame, int iteration = 6)
        {
            Rider rider;
            List<int> diagnosis = null;
            using (_track.Grid.Sync.AcquireRead())
            {
                diagnosis = DiagnoseFrame(frame, iteration);
                rider = GetFrame(frame, iteration);
            }
            return new RiderFrame(frame, rider, diagnosis, iteration);
        }
        public List<int> DiagnoseFrame(int frame, int iteration = 6)
        {
            bool isiteration = iteration != 6 && frame > 0;
            frame = isiteration ? frame - 1 : frame;
            var next = GetFrame(frame + 1);
            var rider = GetFrame(frame);
            if (next.Crashed == rider.Crashed)
                return new List<int>();

            return rider.Diagnose(
                    _track.Grid,
                    _track.Bones,
                    Math.Min(6, iteration + 1));
        }
        public Rider GetFrame(int frame, int iteration = 6)
        {
            bool isiteration = iteration != 6 && frame > 0;
            if (iteration != 6 && frame > 0)
            {
                return GetFrame(frame - 1).Simulate(
                        _track.Grid,
                        _track.Bones,
                        null,
                        null,
                        iteration);
            }
            return GetFrame(frame);
        }
        public Rider GetFrame(int frame)
        {
            using (_framesync.AcquireWrite())
            {
                int start;
                int count;
                int invalid;
                using (changesync.AcquireRead())
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
            var collisionlist = new List<LinkedList<int>>(count);
            Rider current = _frames[start - 1];
            int framecount = _frames.Count;
            var bones = _track.Bones;
            using (changesync.AcquireWrite())
            {
                // we use the savedcells buffer exactly so runframes can
                // be completely consistent with the track state at the
                // time of running

                // we could also get a lock on the track grid, but that would
                // block user input on the track until we finish running.
                _savedcells.Clear();
            }
            using (var sync = changesync.AcquireRead())
            {
                HitTest.MarkFirstInvalid(start);
                for (int i = 0; i < count; i++)
                {
                    int currentframe = start + i;
                    var collisions = new LinkedList<int>();
                    current = current.Simulate(
                        _savedcells,
                        bones,
                        _activetriggers,
                        collisions);
                    steps[i] = current;
                    collisionlist.Add(collisions);
                    // 3 seconds of frames, 
                    // couldnt hurt to check?
                    if (i % 120 == 0)
                    {
                        sync.ReleaseWaiting();
                    }
                }
                HitTest.AddFrames(collisionlist);
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
        }
    }
}