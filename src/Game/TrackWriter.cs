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
using System.Collections.Generic;
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
using linerider.Game;
namespace linerider
{
    public class TrackWriter : TrackReader
    {
        private bool _disposed = false;
        private UndoManager _undo;
        private Timeline _timeline;
        private SimulationRenderer _renderer;
        /// <summary>
        /// Gets or sets the track name
        /// </summary>
        public override string Name
        {
            get { return Track.Name; }
            set { Track.Name = value; }
        }
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
        public void NotifyTrackChanged()
        {
            game.Track.NotifyTrackChanged();
        }
        /// <summary>
        /// Disables saving changes to the undo buffer.
        /// </summary>
        public void DisableUndo()
        {
            _undo = null;
        }
        public static TrackWriter AcquireWrite(ResourceSync sync, Track track, SimulationRenderer renderer, UndoManager undo, Timeline timeline)
        {
            return new TrackWriter(sync.AcquireWrite(), track) { _undo = undo, _renderer = renderer, _timeline = timeline };
        }
        /// <summary>
        /// state a change to the undo manager
        /// always needs to be in PAIRS with a before and after
        /// </summary>
        private void RegisterUndoAction(GameLine before, GameLine after)
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
            _timeline.SaveCells(linestart, lineend);
        }
        /// <summary>
        /// Adds the line to the track, grid, and renderer. 
        /// Updates extensions
        /// Notifies the undo/buffer managers
        /// Most uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void AddLine(GameLine line, bool updateextensions = true)
        {
            if (line is StandardLine)
                SaveCells(line.Position, line.Position2);

            Track.AddLine(line);
            _renderer.AddLine(line);
            RegisterUndoAction(null, line);
            if (updateextensions && line is StandardLine stl)
                AddExtensions(stl);
        }
        /// <summary>
        /// Moves the line in the track, grid, and renderer. 
        /// Updates extensions
        /// Notifies the undo/buffer managers
        /// Most uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void MoveLine(GameLine line, Vector2d pos1, Vector2d pos2, bool updateextensions = true)
        {
            if (line.Position != pos1 || line.Position2 != pos2)
            {
                var clone = line.Clone();
                var std = line as StandardLine;
                if (std != null)
                {
                    SaveCells(line.Position, line.Position2);
                    SaveCells(pos1, pos2);
                    if (updateextensions)
                        RemoveExtensions(std);
                    line.Position = pos1;
                    line.Position2 = pos2;
                    std.CalculateConstants();
                    Track.Grid.MoveLine(clone.Position, clone.Position2, std);
                    if (updateextensions)
                        AddExtensions(std);
                }
                else
                {
                    line.Position = pos1;
                    line.Position2 = pos2;
                }
                Track.RemoveLineFromGrid(clone, false);
                Track.AddLineToGrid(line, false);

                RegisterUndoAction(clone, line);
                _renderer.RedrawLine(line);
            }
        }
        /// <summary>
        /// Replaces the line in the track, grid, and renderer. 
        /// Updates extensions
        /// Notifies the undo/buffer managers
        /// Most uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void ReplaceLine(GameLine oldline, GameLine newline, bool updateextensions = true)
        {
            if (oldline.ID != newline.ID)
                throw new Exception("can only replace lines with the same id");
            RegisterUndoAction(oldline, newline);

            var std = oldline as StandardLine;
            if (std != null)
            {
                SaveCells(oldline.Position, oldline.Position2);
                SaveCells(newline.Position, newline.Position2);
                if (updateextensions)
                    RemoveExtensions(std);
            }
            using (Track.Grid.Sync.AcquireWrite())
            {
                Track.RemoveLineFromGrid(oldline);
                Track.AddLineToGrid(newline);
            }
            Track.LineLookup[newline.ID] = newline;
            if (updateextensions && newline is StandardLine stl)
                AddExtensions(stl);
            _renderer.RedrawLine(newline);
        }

        /// <summary>
        /// Removes the line from the track, grid, and renderer. 
        /// Updates extensions
        /// Notifies the undo/buffer managers
        /// Most uses should be wrapped in UndoManager.BeginAction / EndAction
        /// </summary>
        public void RemoveLine(GameLine line, bool updateextensions = true)
        {
            if (line is StandardLine std)
            {
                SaveCells(line.Position, line.Position2);
                if (updateextensions)
                    RemoveExtensions(std);
            }
            RegisterUndoAction(line, null);
            Track.RemoveLine(line);
            _renderer.RemoveLine(line);
        }
        private SimulationCell GetPairs(StandardLine input)
        {
            var list = new SimulationCell();
            var c1 = Track.QuickGrid.GetCellFromPoint(input.Position);
            var c2 = Track.QuickGrid.GetCellFromPoint(input.Position2);
            var cells = new SimulationCell<GameLine>[]
            {
                c1,
                c1 == c2
                ? null
                : c2 };
            foreach (var cell in cells)
            {
                if (cell == null)
                    continue;
                foreach (var line in cell)
                {
                    if (line.Type == LineType.Scenery)
                        continue;
                    if (line.ID != input.ID)
                    {
                        if (
                            line.End == input.Start ||
                            line.Start == input.End)
                        {
                            list.AddLine((StandardLine)line);
                        }
                    }
                }
            }
            return list;
        }
        public void AddExtensions(StandardLine input)
        {
            UpdateExtensions(input, true);
        }
        public void RemoveExtensions(StandardLine input)
        {
            UpdateExtensions(input, false);
        }
        /// <summary>
        /// Checks every line with the input line's ends if its extensions
        /// pair with input and adds or removes them
        /// </summary>
        /// <param name="input">the input line</param>
        /// <param name="add">should extensions be added or removed</param>
        private void UpdateExtensions(StandardLine input, bool add)
        {
            //todo this method could be faster. its now called on every moveline
            //etc
            Debug.Assert(
                input != null,
                "passed null line to update extensions");

            var list = GetPairs(input);
            var inputangle = Angle.FromVector(input.End - input.Start).Degrees;
            var inputclone = input.Clone();
            bool changemade = false;
            foreach (var connected in list)
            {
                var angle2 = Angle.FromVector(
                    connected.End - connected.Start)
                    .Degrees;
                bool startlink = input.Start == connected.End;
                var diff = startlink
                ? new Angle(angle2 - inputangle).Degrees
                : new Angle(inputangle - angle2).Degrees;
                if (diff > 0 && diff <= 180)
                {
                    var clone = connected.Clone();
                    if (!changemade)
                    {
                        SaveCells(input.Position, input.Position);
                        changemade = true;
                    }
                    SaveCells(connected.Position, connected.Position);
                    if (add)
                    {
                        input.Extension |= startlink
                        ? StandardLine.Ext.Left
                        : StandardLine.Ext.Right;

                        connected.Extension |= startlink
                        ? StandardLine.Ext.Right
                        : StandardLine.Ext.Left;
                    }
                    else
                    {
                        input.Extension &= ~(startlink
                        ? StandardLine.Ext.Left
                        : StandardLine.Ext.Right);

                        connected.Extension &= ~(startlink
                        ? StandardLine.Ext.Right
                        : StandardLine.Ext.Left);
                    }
                    RegisterUndoAction(clone, connected);
                }
            }
            //we do this outside of loop in case we lose both extensions!
            if (changemade)
                RegisterUndoAction(inputclone, input);
        }
    }
}
