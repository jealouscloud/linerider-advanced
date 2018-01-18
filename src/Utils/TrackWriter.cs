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
using linerider.Tools;
using linerider.Drawing;
namespace linerider
{
    public class TrackWriter : TrackReader
    {
        private bool _disposed = false;
        private UndoManager _undo;
        private SimulationRenderer _renderer;
        public Track Track
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("TrackWriter");
                return _track;
            }
        }
        protected TrackWriter(ResourceSync.ResourceLock sync, Track track) : base(sync,track)
        {
            _track = track;
            _sync = sync;
        }
        /// <summary>
        /// a bizarre function for the undomanager to call so we dont add more actions when undoing
        /// </summary>
        public void DisableUndo()
        {
            _undo = null;
        }
        public static TrackWriter AcquireWrite(ResourceSync sync, Track track, SimulationRenderer renderer, UndoManager undo = null)
        {
            return new TrackWriter(sync.AcquireWrite(), track) { _undo = undo, _renderer = renderer };
        }
        public void AddLine(Line l)
        {
            Track.AddLine(l);
            _renderer.AddLine(l);
            if (_undo != null)
			{
				var state = l.GetState();
				state.Exists = false;
				_undo.AddChange(state); 
                state = l.GetState();
				state.Exists = true;
				_undo.AddChange(state);
			}
        }
        public void RemoveLine(Line l)
        {
            Track.RemoveLine(l);
			_renderer.RemoveLine(l);
			if (_undo != null)
			{
				var state = l.GetState();
                state.Exists = true;
				_undo.AddChange(state);
				state = l.GetState();
                state.Exists = false;
				_undo.AddChange(state);
			}
        }
        public void TryDisconnectLines(StandardLine l1, StandardLine l2)
        {
            if (l1 == null || l2 == null) return;
            Vector2d joint;
            if (l1.Position == l2.Position || l1.Position == l2.Position2)
                joint = l1.Position;
            else if (l1.Position2 == l2.Position || l1.Position2 == l2.Position2)
                joint = l1.Position2;
            else
                return;
            //var leftlink = (l1.CompliantPosition == joint && l2.CompliantPosition2 == joint);
            var rightlink = (l1.End == joint && l2.Start == joint);
            _undo?.AddChange(l1.GetState());
            if (rightlink)
            {
                l1.Next = null;
                l1.RemoveExtension(StandardLine.ExtensionDirection.Right);
                l2.Prev = null;
                l2.RemoveExtension(StandardLine.ExtensionDirection.Left);
            }
            else
            {
                l1.Prev = null;
                l1.RemoveExtension(StandardLine.ExtensionDirection.Left);
                l2.Next = null;
                l2.RemoveExtension(StandardLine.ExtensionDirection.Right);
            }
        }

        public void TryConnectLines(StandardLine l1, StandardLine l2)
        {
            if (l1 == null || l2 == null) return;
            Vector2d joint;
            if (l1.Position == l2.Position || l1.Position == l2.Position2)
                joint = l1.Position;
            else if (l1.Position2 == l2.Position || l1.Position2 == l2.Position2)
                joint = l1.Position2;
            else
                return;
            var leftlink = (l1.Start == joint && l2.End == joint);
            var rightlink = (l1.End == joint && l2.Start == joint);

            if (!leftlink && !rightlink) return;

            var diff1 = l2.End - l2.Start;
            var diff2 = l1.End - l1.Start;

            var angle1 = Angle.FromVector(diff1).Degrees;
            var angle2 = Angle.FromVector(diff2).Degrees;

            var anglediff1 = new Angle(angle1 - angle2).Degrees;
            var anglediff2 = new Angle(angle2 - angle1).Degrees;

            bool cmp1 = anglediff1 > 0 && anglediff1 <= 180;
            bool cmp2 = anglediff2 > 0 && anglediff2 <= 180;
            if ((rightlink) ? cmp2 : cmp1)
            {
                _undo?.AddChange(l1.GetState());
                if (rightlink)
                {
                    l1.Next = l2;
                    l1.AddExtension(StandardLine.ExtensionDirection.Right);
                    l2.Prev = l1;
                    l2.AddExtension(StandardLine.ExtensionDirection.Left);
                }
                else
                {
                    l1.Prev = l2;
                    l1.AddExtension(StandardLine.ExtensionDirection.Left);
                    l2.Next = l1;
                    l2.AddExtension(StandardLine.ExtensionDirection.Right);
                }
            }
        }
    }
}
