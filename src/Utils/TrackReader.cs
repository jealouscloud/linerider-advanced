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
using linerider.Drawing;
using linerider.Game;
using linerider.Utils;
namespace linerider
{
    public class TrackReader : IDisposable
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
        private bool _disposed = false;
        protected TrackReader(ResourceSync.ResourceLock sync, Track track)
        {
            _track = track;
            _sync = sync;
        }
        public static TrackReader AcquireRead(ResourceSync sync, Track track)
        {
            return new TrackReader(sync.AcquireRead(), track);
        }

        public Line GetLastLine()
        {
            if (Track.Lines.Count == 0)
                return null;
            return Track.Lines[Track.Lines.Count - 1];
        }

        public Line GetFirstLine()
        {
            if (Track.Lines.Count == 0)
                return null;
            return Track.Lines[0];
        }
        //todo this function does not prevent Line data from being written to
        public List<Line> GetLinesInRect(FloatRect rect, bool precise)
        {
            return Track.GetLinesInRect(rect, precise);
        }

        public Rider Tick(Rider state)
        {
            var ret = Track.Tick(state);
            return ret;
        }

        public HashSet<int> Diagnose(Rider state, int maxiteration = 6)
        {
            return Track.Diagnose(state, maxiteration);
        }
        public void SaveTrackAsSol()
        {
            TrackLoader.SaveTrackSol(_track);
        }
        public void SaveTrackTrk(string savename, string songdata)
        {
            TrackLoader.SaveTrackTrk(_track, savename, songdata);
        }
        public Dictionary<string, bool> GetFeatures()
        {
            return TrackLoader.TrackFeatures(Track);
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
