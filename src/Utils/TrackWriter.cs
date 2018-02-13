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
using linerider.Rendering;
using linerider.Lines;
using linerider.Utils;
namespace linerider
{
    public class TrackWriter : TrackReader
    {
        private bool _disposed = false;
        private UndoManager _undo;
        private PlaybackBufferManager _buffermanager;
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
        protected TrackWriter(ResourceSync.ResourceLock sync, Track track) : base(sync, track)
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
        public static TrackWriter AcquireWrite(ResourceSync sync, Track track, SimulationRenderer renderer, UndoManager undo, PlaybackBufferManager manager)
        {
            return new TrackWriter(sync.AcquireWrite(), track) { _undo = undo, _renderer = renderer, _buffermanager = manager };
        }
        /// <summary>
        /// state a change to the undo manager
        /// always needs to be in PAIRS with a before and after
        /// </summary>
        private void RegisterUndoAction(Line before, Line after)
        {
                _undo?.AddChange(before, after);
        }
        /// <summary>
        /// State a change to the buffer manager
        /// call this before making the change, as the buffer manager
        /// backs up cells and compares their output to the new cells
        /// </summary>
        /// <param name="linestart">line.Position</param>
        /// <param name="lineend">line.Position2</param>
        private void SaveCells(Vector2d linestart, Vector2d lineend)
        {
            _buffermanager.SaveCells(linestart, lineend);
        }
        /// <summary>
        /// Tells the buffer manager to halt updating
        /// until the resource is disposed.
        /// 
        /// for example:
        /// call this when you need to change a line and then extensions
        /// </summary>
        public ResourceSync.ResourceLock AcquireBufferUpdateSync()
        {
            return _buffermanager.BeginTransaction();
        }
        /// <summary>
        /// Moves the line in the track, grid, and renderer. Is naive to extensions, and notifies the undo/buffer managers
        /// All normal uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void MoveLine(Line line, Vector2d pos1, Vector2d pos2)
        {
            if (line.Position != pos1 || line.Position2 != pos2)
            {
                var clone = line.Clone();

                if (line is StandardLine)
                {
                    SaveCells(line.Position, line.Position2);
                    SaveCells(pos1, pos2);
                }

                Track.RemoveLineFromGrid(line);
                line.Position = pos1;
                line.Position2 = pos2;
                line.CalculateConstants();
                Track.AddLineToGrid(line);
                RegisterUndoAction(clone, line);
                _renderer.RedrawLine(line);
            }
        }
        public void ReplaceLine(Line oldline, Line newline)
        {
            if (oldline.ID != newline.ID)
                throw new Exception("can only replace lines with the same id");
            RegisterUndoAction(oldline, newline);

            if (oldline is StandardLine)
            {
                SaveCells(oldline.Position, oldline.Position2);
                SaveCells(newline.Position, newline.Position2);
            }
            Track.RemoveLineFromGrid(oldline);
            Track.AddLineToGrid(newline);
            _renderer.RedrawLine(newline);
        }
        /// <summary>
        /// Adds the line to the track, grid, and renderer. Is naive to extensions, and notifies the undo/buffer managers
        /// All normal uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void AddLine(Line line)
        {
            if (line is StandardLine)
                SaveCells(line.Position, line.Position2);

            Track.AddLine(line);
            _renderer.AddLine(line);
            RegisterUndoAction(null, line);
        }
        /// <summary>
        /// Removes the line from the track, grid, and renderer, updates extensions, and notifies undo/buffer managers.
        /// All normal uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void RemoveLine(Line line)
        {
            RegisterUndoAction(line, null);

            if (line is StandardLine)
            {
                using (AcquireBufferUpdateSync())
                {
                    SaveCells(line.Position, line.Position2);
                    var st = line as StandardLine;
                    TryDisconnectLines(st, st.Next);
                    TryDisconnectLines(st, st.Prev);
                }
            }
            Track.RemoveLine(line);
            _renderer.RemoveLine(line);
        }
        /// <summary>
        /// Tries to disconnect two lines that are currently on the grid, updating extensions
        /// All normal uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
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

            if (l1 is StandardLine)
                SaveCells(l1.Position, l1.Position2);
            if (l2 is StandardLine)
                SaveCells(l2.Position, l2.Position2);

            var rightlink = (l1.End == joint && l2.Start == joint);
            var l1clone = l1.Clone();
            var l2clone = l2.Clone();
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
            RegisterUndoAction(l1clone, l1);
            RegisterUndoAction(l2clone, l2);
        }

        /// <summary>
        /// Tries to connect two lines that are currently on the grid, updating extensions
        /// All normal uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
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
                if (l1 is StandardLine)
                    SaveCells(l1.Position, l1.Position2);
                if (l2 is StandardLine)
                    SaveCells(l2.Position, l2.Position2);
                var l1clone = l1.Clone();
                var l2clone = l2.Clone();
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
                RegisterUndoAction(l1clone, l1);
                RegisterUndoAction(l2clone, l2);
            }
        }
    }
}
