//
//  GLWindow.cs
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

using System;
using System.Linq;
using System.Text;
using OpenTK;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using linerider.Tools;
using linerider.Drawing;
using linerider.Game;
using linerider.Lines;
using linerider.UI;
namespace linerider.Utils
{
    /// <summary>
    /// playback scrubber/buffer manager
    /// properties:
    /// has full access to track.RiderStates at all times
    /// calls thread safe access to createtrackreader to simulate
    /// </summary>
    public class PlaybackBufferManager : GameService
    {
        private List<LineState> _changes = new List<LineState>();
        private ResourceSync _sync;
        private bool _restart = false;
        private bool _running = false;
        private const int MaxLinesBeforeCellAutoPasses = 10;
        private const int DummyID = -1;
        private const int Aborted = -2;
        public PlaybackBufferManager()
        {
            _sync = new ResourceSync();
        }
        public void AddChange(LineState state)
        {
            using (_sync.AcquireWrite())
            {
                _changes.Add(state);
                _restart = true;
            }
        }
        public void RemoveChange(LineState state)
        {
            using (_sync.AcquireWrite())
            {
                _changes.Remove(state);
                _restart = true;
            }
        }
        public void Update()
        {
            using (_sync.AcquireWrite())
            {
                if (!_running)
                {
                    _running = true;
                    ThreadPool.QueueUserWorkItem(RunUpdate);
                }
            }
        }
        public void Reset()
        {
            using (_sync.AcquireWrite())
            {
                _restart = true;
                _changes.Clear();
            }
        }
        private void RunUpdate(Object callerstate)
        {
            game.Title = Program.WindowTitle;
            while (_restart)
            {
                LineState[] changes;
                using (_sync.AcquireWrite())
                {
                    _restart = false;
                    if (_changes.Count == 0 || game.Track.EndFrameID == 0)
                        continue;
                    changes = _changes.ToArray();
                }
                Track track;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    track = trk.Track;//hacky way to grab onto the track
                    //have to be careful here about thread safety
                }
                int start = FindUpdateStart(track, changes);
                if (start == Aborted)
                    continue;
                if (start > 0)
                {
                    Rider[] states = new Rider[track.RiderStates.Count - start];
                    Rider state = track.RiderStates[start - 1];
                    for (int i = 0; i < states.Length; i++)
                    {
                        if (_restart)
                            break;
                        //todo hit test
                        state = state.Simulate(track, null);
                        states[i] = state;
                    }
                    using (_sync.AcquireWrite())
                    {
                        if (_restart)
                            continue;
                        for (int i = 0; i < states.Length; i++)
                        {
                            track.RiderStates[start + i] = states[i];
                        }
                        _changes.Clear();
                    }
                    game.Title = "Updated frames " + start + "-" + track.RiderStates.Count;
                    game.Track.UpdateRenderRider();
                    game.InvalidateTrack();
                }
                using (_sync.AcquireWrite())
                {
                    _running = false;
                }
            }
        }
        private int FindUpdateStart(Track track, LineState[] changes)
        {
            Dictionary<GridPoint, SimulationCell> points = new Dictionary<GridPoint, SimulationCell>();
            List<InteractionTestLine> lines = new List<InteractionTestLine>();
            for (int i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                InteractionTestLine sl = new InteractionTestLine(change.Pos1, change.Pos2, change.Inverted) { Extension = change.extension, ID = DummyID };
                var positions = track.Grid.GetGridPositions(sl);
                foreach (var p in positions)
                {
                    SimulationCell cell;
                    if (!points.TryGetValue(p.Point, out cell))
                    {
                        cell = new SimulationCell();
                        points[p.Point] = cell;
                    }

                    cell.AddLine(sl);
                }
            }
            return CalculateFirstInteraction(track, 0, track.RiderStates.Count - 1, points);
        }

        public int CalculateFirstInteraction(Track track, int start, int end, Dictionary<GridPoint, SimulationCell> cells)
        {
            var gridpoints = cells.Keys.ToArray();
            Dictionary<int, Line> collisions = new Dictionary<int, Line>();
            int idx = start;
            for (; idx <= end; idx++)
            {
                if (_restart)
                    return Aborted;
                var state = track.RiderStates[idx];
                for (int icx = 0; icx < gridpoints.Length; icx++)
                {
                    if (state.PhysInfo.ContainsCell(gridpoints[icx]))
                    {
                        if (CheckInteraction(track, state, cells))
                            return idx + 1;//the next frame will be different. this one stays the same.
                    }
                }
            }
            return -1;
        }
        private bool CheckInteraction(Track track, Rider state, Dictionary<GridPoint, SimulationCell> changedcells)
        {
            SimulationPoint[] joints = new SimulationPoint[state.Body.Length];
            bool dead = state.Crashed;
            for (int r = 0; r < joints.Length; r++)
            {
                joints[r] = state.Body[r].StepMomentum();
            }
            using (var reader = game.Track.CreateTrackReader())
            {
                for (int iteration = 0; iteration < 6; iteration++)
                {
                    Rider.ProcessBones(track.Bones, joints, ref dead);
                    for (int i = 0; i < joints.Length; i++)
                    {
                        var cellx = (int)Math.Floor(joints[i].Location.X / 14);
                        var celly = (int)Math.Floor(joints[i].Location.Y / 14);
                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                SimulationCell changelines;
                                var lines = track.Grid.GetCell(cellx + x, celly + y);
                                if (changedcells.TryGetValue(new GridPoint(cellx + x, celly + y), out changelines))
                                {
                                    if (TestCell(ref joints[i], changelines, lines))
                                        return true;
                                }
                                else
                                {
                                    if (lines != null)
                                        joints[i] = Rider.ProcessCell(lines, joints[i]);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool TestCell(ref SimulationPoint joint, SimulationCell changelines, SimulationCell tracklines)
        {
            if (tracklines != null)
            {
                if (tracklines.Count + changelines.Count > MaxLinesBeforeCellAutoPasses)
                {
                    return true;//unreasonable to test this
                }
                foreach (var line in tracklines)
                {
                    foreach (var test in changelines)
                    {
                        if (test.Interact(ref joint))
                        {
                            return true;
                        }
                    }
                    line.Interact(ref joint);
                }
            }
            foreach (var test in changelines)
            {
                if (test.Interact(ref joint))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
