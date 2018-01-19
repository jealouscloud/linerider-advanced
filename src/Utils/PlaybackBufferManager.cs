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
using linerider.Utils;
namespace linerider
{
    public class PlaybackBufferManager : GameService
    {
        private Track _track;
        private List<LineState> _changes = new List<LineState>();
        private ResourceSync _sync;
        private bool _restart = false;
        private bool _running = false;
        private const int DummyID = -1;
        private const int Aborted = -2;
        public PlaybackBufferManager(Track track)
        {
            _track = track;
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
        private void RunUpdate(Object callerstate)
        {
            while (_restart)
            {
                LineState[] changes;
                using (_sync.AcquireRead())
                {
                    _restart = false;
                    if (_changes.Count == 0 || _track.RiderStates.Count <= 1)
                        continue;
                    changes = _changes.ToArray();
                }
                int start = CalculateUpdateStart(changes);
                if (start == Aborted)
                    continue;
                if (start > 0)
                {
                    Rider[] states = new Rider[_track.RiderStates.Count - start];
                    Rider state = _track.RiderStates[start - 1];
                    for (int i = 0; i < states.Length; i++)
                    {
                        if (_restart)
                            break;
                        //todo hit test
                        state = state.Simulate(_track, null);
                        states[i] = state;
                    }
                    using (_sync.AcquireWrite())
                    {
                        if (_restart)
                            continue;
                        for (int i = 0; i < states.Length; i++)
                        {
                            _track.RiderStates[start + i] = states[i];
                        }
                        _changes.Clear();
                    }
                    game.Track.UpdateRenderRider();
                    game.InvalidateTrack();
                }
            }
            using (_sync.AcquireWrite())
            {
                _running = false;
            }
        }
        private int CalculateUpdateStart(LineState[] changes)
        {
            Dictionary<GridPoint, SimulationCell> points = new Dictionary<GridPoint, SimulationCell>();
            List<InteractionTestLine> lines = new List<InteractionTestLine>();
            for (int i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                InteractionTestLine sl = new InteractionTestLine(change.Pos1, change.Pos2, change.Inverted) { Extension = change.extension, ID = DummyID };
                var positions = _track.Grid.GetGridPositions(sl);
                foreach (var p in positions)
                {
                    SimulationCell cell;
                    if (!points.TryGetValue(p.Point, out cell))
                    {
                        cell = _track.Grid.GetCell(p.X, p.Y).Clone();

                        points[p.Point] = cell;
                    }
                    var node = cell.First;
                    while (node != null)
                    {
                        node = cell.AddAfter(node, sl).Next;
                    }
                }
            }
            return GetUpdateStart(0, _track.RiderStates.Count - 1, points);
        }
        private void TestCells(Rider state, Dictionary<GridPoint, SimulationCell> cells)
        {
            SimulationPoint[] joints = new SimulationPoint[state.Body.Length];
            bool dead = state.Crashed;
            for (int r = 0; r < joints.Length; r++)
            {
                joints[r] = state.Body[r].StepMomentum();
            }
            for (int iteration = 0; iteration < 6; iteration++)
            {
                Rider.ProcessBones(_track.Bones, joints, ref dead);
                for (int i = 0; i < joints.Length; i++)
                {
                    SimulationPoint joint = joints[i];
                    var cellx = (int)Math.Floor(joint.Location.X / 14);
                    var celly = (int)Math.Floor(joint.Location.Y / 14);
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            SimulationCell cell;
                            if (cells.TryGetValue(new GridPoint(cellx + x, celly + y), out cell))
                            {
                                joints[i] = Rider.ProcessCell(cell, joint);
                            }
                            else
                            {
                                var lines = _track.Grid.GetCell(cellx + x, celly + y);
                                if (lines != null)
                                    joints[i] = Rider.ProcessCell(lines, joints[i]);
                            }
                        }
                    }
                }
            }
        }
        public int GetUpdateStart(int start, int end, Dictionary<GridPoint, SimulationCell> cells)
        {
            var gridpoints = cells.Keys.ToArray();
            Dictionary<int, Line> collisions = new Dictionary<int, Line>();
            int idx = start;
            try
            {
                for (; idx <= end; idx++)
                {
                    if (_restart)
                        return Aborted;
                    var state = _track.RiderStates[idx];
                    for (int icx = 0; icx < gridpoints.Length; icx++)
                    {
                        if (state.PhysInfo.ContainsCell(gridpoints[icx]))
                        {
                            TestCells(state, cells);
                        }
                    }
                }
            }
            catch (InteractionTestLine.LineInteractionException)
            {
                return idx;
            }
            return -1;
        }

    }
}
