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
    public sealed class PlaybackBufferManager : GameService
    {
        private HashSet<GridPoint> _changedcells = new HashSet<GridPoint>();
        private SimulationGrid _grid;
        private ResourceSync _statesync;
        private ManualResetEvent _updatesync = new ManualResetEvent(false);
        private ManualResetEvent _updatefinishsync = new ManualResetEvent(false);
        private bool _restart = false;
        private bool _running = false;
        private Thread _updatethread;
        private int _changes = 0;
        public PlaybackBufferManager(SimulationGrid grid)
        {
            _statesync = new ResourceSync();
            Reset(grid);
            _updatethread = new Thread(UpdateRunnerThreadProc)
            { IsBackground = true };
            _updatethread.Start();
        }
        public void SaveCells(Vector2d start, Vector2d end)
        {
            using (_statesync.AcquireWrite())
            {
                var positions = SimulationGrid.GetGridPositions(start, end, _grid.GridVersion);

                foreach (var cellpos in positions)
                {
                    _changedcells.Add(cellpos.Point);
                }
                _changes += 1;
            }
        }
        public void UpdateOnThisThread()
        {
            using (_statesync.AcquireWrite())
            {
                if (!Volatile.Read(ref _running))
                {
                    Volatile.Write(ref _running, true);
                    Recompute();
                    return;
                }
            }
            _updatefinishsync.WaitOne();
        }
        private int _updatedelay = 0;
        public void Update()
        {
            Debug.Assert(_updatethread.IsAlive, "Playback buffer thread is dead!");
            if (_changes > 10)
            {
                if (Interlocked.CompareExchange(ref _updatedelay, 1, 0) == 0)
                {
                    ThreadPool.QueueUserWorkItem(DelayedUpdate);
                }
                return;
            }
            using (_statesync.AcquireWrite())
            {
                Volatile.Write(ref _restart, true);
                if (!Volatile.Read(ref _running))
                {
                    Volatile.Write(ref _running, true);
                    _updatefinishsync.Reset();
                    _updatesync.Set();
                }
            }
        }
        private void DelayedUpdate(object state)
        {
            Thread.Sleep(30);
            Volatile.Write(ref _updatedelay,0);
            using (_statesync.AcquireWrite())
            {
                Volatile.Write(ref _restart, true);
                if (!Volatile.Read(ref _running))
                {
                    Volatile.Write(ref _running, true);
                    _updatefinishsync.Reset();
                    _updatesync.Set();
                }
            }
        }
        public void Reset(SimulationGrid grid)
        {
            using (_statesync.AcquireWrite())
            {
                _grid = grid;
                _changedcells.Clear();
                _restart = true;
            }
        }
        private void UpdateRunnerThreadProc()
        {
            try
            {
                while (true)
                {
                    _updatesync.WaitOne();
                    Recompute();
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                Program.Crash(e);
            }
        }
        private void Recompute()
        {
            game.Title = Program.WindowTitle;
            var exit = false;
            do
            {
                try
                {
                    using (_statesync.AcquireWrite())
                    {
                        _restart = false;
                        if (_changedcells.Count == 0 ||
                        game.Track.EndFrameID == 0)
                        {
                            continue;
                        }
                    }
                    Track track;
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        track = trk.Track;//hacky way to grab onto the track
                                          //be careful here about thread safety
                    }
                    int start = FindUpdateStart(track);
                    if (start == -1)
                        continue;
                    if (!PerformUpdate(track, start))
                        continue;
                    game.Track.UpdateRenderRider();
                    game.InvalidateTrack();
                    game.Title = "Updated frames " + start + "-" + track.RiderStates.Count;
                }
                finally
                {
                    using (_statesync.AcquireWrite())
                    {
                        bool restart = Volatile.Read(ref _restart);
                        if (!restart)
                        {
                            exit = true;
                            Volatile.Write(ref _running, false);
                            _changedcells.Clear();
                            _updatesync.Reset();
                            Volatile.Write(ref _changes, 0);
                            // release threads waiting on us
                            _updatefinishsync.Set();
                        }
                    }
                }
            } while (!exit);
            Debug.Assert(exit, "Thread exiting without permission");
        }
        private int FindUpdateStart(Track track)
        {
            RectLRTB changebounds = new RectLRTB();
            bool hassetfirst = false;
            using (_statesync.AcquireRead())
            {
                foreach (var cell in _changedcells)
                {
                    if (!hassetfirst)
                    {
                        changebounds = new RectLRTB(cell);
                        hassetfirst = true;
                        continue;
                    }
                    changebounds.left = Math.Min(cell.X, changebounds.left);
                    changebounds.top = Math.Min(cell.Y, changebounds.top);
                    changebounds.right = Math.Max(cell.X, changebounds.right);
                    changebounds.bottom = Math.Max(cell.Y, changebounds.bottom);
                }
            }
            using (game.Track.CreatePlaybackReader())
            {
                return CalculateFirstInteraction(track, changebounds);
            }
        }
        private int CalculateFirstInteraction(
            Track track,
            RectLRTB changebounds)
        {
            int statecount = track.RiderStates.Count;
            GridPoint[] overlay;

            using (_statesync.AcquireRead())
            {
                overlay = _changedcells.ToArray();
            }
            for (int frame = 1; frame < statecount; frame++)
            {
                if (_restart)
                    return -1;
                if (!changebounds.Intersects(track.RiderStates[frame].PhysicsBounds))
                    continue;
                foreach (var change in overlay)
                {
                    if (track.RiderStates[frame].PhysicsBounds.ContainsPoint(change))
                    {
                        if (CheckInteraction(track, frame))
                            return frame;
                        // we dont have to check this rider more than once!
                        break;
                    }

                }
            }
            return -1;
        }
        private bool PerformUpdate(Track track, int start)
        {
            Debug.Assert(start > 0, "start is invalid for simulatechanges");
            using (var rw = game.Track.CreatePlaybackUpgradableReader())
            {
                Rider[] states = SimulateChanges(
                    track,
                    track.RiderStates[start - 1],
                    start,
                    track.RiderStates.Count - start);
                using (_statesync.AcquireWrite())
                {
                    if (Volatile.Read(ref _restart))
                        return false;
                    rw.UpgradeToWriter();
                    UpdateBuffer(track, start, states);
                    _changedcells.Clear();
                }
            }
            return true;
        }

        private Rider[] SimulateChanges(Track track,
        Rider prevstate,
        int start,
        int count)
        {
            Debug.Assert(count > 0, "simulating 0 changes");
            Rider[] states = new Rider[count];
            // we have to regenerate the frame at start using the frame before it
            states[0] = prevstate.Simulate(track, null);
            int statelen = states.Length;
            for (int i = 1; i < statelen; i++)
            {
                if (_restart)
                    return null;
                //todo hit test
                states[i] = states[i - 1].Simulate(track, null);
            }
            return states;
        }
        private void UpdateBuffer(Track track, int start, Rider[] changes)
        {
            for (int i = 0; i < changes.Length; i++)
            {
                track.RiderStates[start + i] = changes[i];
            }
        }
        private bool CheckInteraction(Track track, int frame)
        {
            // even though its this frame that may need changing, we have to regenerate it using
            // the previous frame.
            //   var overlaysimulated = prev.Simulate(_overlay, track.Bones, null, null);
            var newsimulated = track.RiderStates[frame - 1].Simulate(
                track.Grid,
                track.Bones,
                null,
                null,
                6,
                false);
            if (!newsimulated.Body.CompareTo(track.RiderStates[frame].Body))
            {
                return true;
            }

            return false;
        }
    }
}
