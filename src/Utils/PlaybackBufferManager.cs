//
//  PlaybackBufferManager.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using linerider.Tools;
using linerider.Rendering;
using linerider.Game;
using linerider.Lines;
using linerider.UI;
using System.Diagnostics;

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
        private SimulationGridOverlay _overlay = new SimulationGridOverlay();
        private SimulationGrid _grid;
        private ResourceSync _statesync;
        private ManualResetEvent _updatesync = new ManualResetEvent(true);
        private bool _restart = false;
        private bool _running = false;
        private const int Aborted = -2;
        public PlaybackBufferManager(SimulationGrid grid)
        {
            _statesync = new ResourceSync();
            Reset(grid);
        }
        public void SaveCells(Vector2d start, Vector2d end)
        {
            using (_statesync.AcquireWrite())
            {
                var positions = SimulationGrid.GetGridPositions(start, end, _grid.GridVersion);
                using (_grid.Sync.AcquireRead())
                {
                    foreach (var cellpos in positions)
                    {
                        var cell = _grid.GetCell(cellpos.X, cellpos.Y);
                        //cell can be null, we back that up too
                        _overlay.BackupCell(cellpos.Point, cell);
                    }
                    _restart = true;
                }
            }
        }
        public void UpdateOnThisThread()
        {
            using (_statesync.AcquireWrite())
            {
                if (!_running)
                {
                    _updatesync.Reset();
                    _running = true;
                    RunUpdate(null);
                    return;
                }
            }
            _updatesync.WaitOne();
        }
        public void Update()
        {
            using (_statesync.AcquireWrite())
            {
                if (!_running)
                {
                    _updatesync.Reset();
                    _running = true;
                    ThreadPool.QueueUserWorkItem(RunUpdate);
                }
            }
        }
        public void Reset(SimulationGrid grid)
        {
            using (_statesync.AcquireWrite())
            {
                _restart = true;
                _grid = grid;
                _overlay.BaseGrid = grid;
                _overlay.Clear();
            }
        }
        private void RunUpdate(Object callerstate)
        {
            game.Title = Program.WindowTitle;
            try
            {
                while (_restart)
                {
                    using (_statesync.AcquireWrite())
                    {
                        _restart = false;
                        if (_overlay.Overlay.Count == 0 || game.Track.EndFrameID == 0)

                        {
                            _running = false;
                            return;
                        }
                    }
                    Track track;
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        track = trk.Track;//hacky way to grab onto the track
                                          //have to be careful here about thread safety
                    }
                    int start = FindUpdateStart(track);
                    if (start == Aborted)
                        continue;
                    if (start != -1)
                    {
                        var playbacksync = game.Track.GetPlaybackSync();

                        using (var rw = playbacksync.AcquireUpgradableRead())
                        {
                            Rider[] states = SimulateChanges(track, start);
                            using (_statesync.AcquireWrite())
                            {
                                if (_restart)
                                    continue;
                                rw.UpgradeToWriter();
                                UpdateBuffer(track, start, states);
                                _overlay.Clear();
                            }
                            game.Track.UpdateRenderRider();
                        }
                        game.Title = "Updated frames " + start + "-" + track.RiderStates.Count;
                        game.InvalidateTrack();
                    }
                    using (_statesync.AcquireWrite())
                    {
                        if (_restart)
                            continue;
                        _running = false;
                        _overlay.Clear();
                        return;
                    }
                }
            }
            finally
            {
                _updatesync.Set();
            }
        }
        private void UpdateBuffer(Track track, int start, Rider[] changes)
        {
            for (int i = 0; i < changes.Length; i++)
            {
                track.RiderStates[start + i] = changes[i];
            }
        }
        private Rider[] SimulateChanges(Track track, int start)
        {
            Rider[] states = new Rider[track.RiderStates.Count - start];
            // we have to regenerate the frame at start using the frame before it
            Rider state = track.RiderStates[start - 1];

            for (int i = 0; i < states.Length; i++)
            {
                if (_restart)
                    return null;
                //todo hit test
                state = state.Simulate(track, null);
                states[i] = state;
            }
            return states;
        }
        private bool CheckInteraction(Track track, int frame)
        {
            // even though its this frame that may need changing, we have to regenerate it using
            // the previous frame.
            var prev = track.RiderStates[frame - 1];
            var overlaysimulated = prev.Simulate(_overlay, track.Bones, null, null);
            var newsimulated = prev.Simulate(track.Grid, track.Bones, null, null);
            for (int i = 0; i < overlaysimulated.Body.Length; i++)
            {
                if (overlaysimulated.Body[i] != newsimulated.Body[i])
                {
                    return true;
                }
            }
            return false;
        }


        private int CalculateFirstInteraction(Track track, RectLRTB changebounds)
        {
            RectLRTB riderbounds;
            int statecount = track.RiderStates.Count;
            for (int frame = 1; frame < statecount; frame++)
            {
                using (_statesync.AcquireRead())
                {
                    if (_restart)
                        return Aborted;
                    riderbounds = track.RiderStates[frame].PhysicsBounds;
                    if (!changebounds.Intersects(riderbounds))
                        continue;
                    foreach (var change in _overlay.Overlay)
                    {
                        if (riderbounds.ContainsPoint(change.Key))
                        {
                            if (CheckInteraction(track, frame))
                                return frame;
                            // we dont have to check this rider more than once!
                            break;
                        }
                    }
                }
            }
            return -1;
        }
        private int FindUpdateStart(Track track)
        {
            List<InteractionTestLine> lines = new List<InteractionTestLine>();
            RectLRTB changebounds = new RectLRTB();
            bool hassetfirst = false;
            using (_statesync.AcquireRead())
            {
                foreach (var cell in _overlay.Overlay)
                {
                    if (!hassetfirst)
                    {
                        changebounds = new RectLRTB(cell.Key);
                        hassetfirst = true;
                        continue;
                    }
                    changebounds.left = Math.Min(cell.Key.X, changebounds.left);
                    changebounds.top = Math.Min(cell.Key.Y, changebounds.top);
                    changebounds.right = Math.Max(cell.Key.X, changebounds.right);
                    changebounds.bottom = Math.Max(cell.Key.Y, changebounds.bottom);
                }
            }
            using (game.Track.CreatePlaybackReader())
            {
                return CalculateFirstInteraction(track, changebounds);
            }
        }
    }
}
