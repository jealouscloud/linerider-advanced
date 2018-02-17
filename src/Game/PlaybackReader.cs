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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using OpenTK;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using linerider.Tools;
using linerider.Rendering;
using linerider.Game;
using linerider.Utils;
using linerider.Lines;
namespace linerider.Utils
{
    public class PlaybackReader : GameService, IDisposable
    {
        protected ResourceSync.ResourceLock _sync;
        protected Track _track;
        private Track Track
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("TrackWriter");
                return _track;
            }
        }
        public string Name
        {
            get { return Track.Name; }
        }
        public int Frames
        {
            get
            {
                return _track.RiderStates.Count;
            }
        }
        private bool _disposed = false;
        protected PlaybackReader(ResourceSync.ResourceLock sync, Track track)
        {
            _track = track;
            _sync = sync;
        }
        public static PlaybackReader AcquireRead(ResourceSync sync, Track track)
        {
            return new PlaybackReader(sync.AcquireRead(), track);
        }
        public Rider GetRider(int frame)
        {
            return _track.RiderStates[frame];
        }
        /// <summary>
        /// Ticks the rider in the simulation ignoring scarf etc.
        /// </summary>
        public Rider QuickSimulate(
            Rider state,
            out HashSet<int> collisions,
            int maxiteration = 6)
        {
            collisions = new HashSet<int>();
            return state.Simulate(_track.Grid, 
            _track.Bones, 
            null,
            collisions, 
            maxiteration, 
            false);
        }
        public List<int> Diagnose(Rider state, int maxiteration = 6)
        {
            return state.Diagnose(Track, null, maxiteration);
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _sync.Dispose();
                _track = null;
                _disposed = true;
            }
        }
    }
}
