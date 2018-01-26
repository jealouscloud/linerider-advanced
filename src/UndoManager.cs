//
//  UndoManager.cs
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

using System.Collections.Generic;
using OpenTK;
using System;
using linerider.Lines;
namespace linerider
{
    public class UndoManager : GameService
    {
        private class act : GameService
        {
            public List<LineState> States;
            public act()
            {
                States = new List<LineState>();
            }
            private bool DoAction(TrackWriter track, LineState beforeact, LineState afteract)
            {
                var parent = beforeact.Parent;


                // remove line
                if (beforeact.Exists && !afteract.Exists)
                {
                    track.RemoveLine(parent);
                    parent.Position = afteract.Pos1;
                    parent.Position2 = afteract.Pos2;
                }
                // add line
                else if (afteract.Exists && !beforeact.Exists)
                {
                    parent.Position = afteract.Pos1;
                    parent.Position2 = afteract.Pos2;
                    track.AddLine(parent);
                }
                else if (parent.Position != afteract.Pos1 || parent.Position2 != afteract.Pos2)//adjust act
                {
                    track.Track.RemoveLineFromGrid(parent);
                    parent.Position = afteract.Pos1;
                    parent.Position2 = afteract.Pos2;
                    parent.CalculateConstants();
                    track.Track.AddLineToGrid(parent);
                }
                var std = parent as StandardLine;
                //do extensions
                if (std != null)
                {
                    if (beforeact.Prev != afteract.Prev)
                    {
                        var oldprev = beforeact.Prev as StandardLine;
                        var newprev = afteract.Prev as StandardLine;
                        if (oldprev != null)
                        {
                            oldprev.RemoveExtension(StandardLine.ExtensionDirection.Right);
                            oldprev.Next = null;
                        }
                        if (newprev != null)
                        {
                            newprev.AddExtension(StandardLine.ExtensionDirection.Right);
                            newprev.Next = std;
                        }
                        std.Prev = newprev;
                    }
                    if (beforeact.Next != afteract.Next)
                    {
                        var oldnext = beforeact.Next as StandardLine;
                        var newnext = afteract.Next as StandardLine;
                        if (oldnext != null)
                        {
                            oldnext.RemoveExtension(StandardLine.ExtensionDirection.Left);
                            oldnext.Prev = null;
                        }
                        if (newnext != null)
                        {
                            newnext.AddExtension(StandardLine.ExtensionDirection.Left);
                            newnext.Prev = std;
                        }
                        std.Next = newnext;
                    }
                    std.SetExtension(afteract.extension);
                }
                return !(parent is SceneryLine);

            }
            /// <summary>
            /// undo previous action, returns true if physics are changed
            /// </summary>
            public virtual bool Undo(TrackWriter track)
            {
                bool ret = false;
                for (int i = States.Count - 1; i > 0; i -= 2)
                {
                    ret |= DoAction(track, States[i], States[i - 1]);
                }
                return ret;
            }

            public virtual bool Redo(TrackWriter track)
            {
                bool ret = false;
                for (int i = 0; i < States.Count - 1; i += 2)
                {
                    ret |= DoAction(track, States[i], States[i + 1]);
                }
                return ret;
            }
        }
        private int pos;
        private List<act> _actions = new List<act>();
        private act _currentaction;
        public UndoManager()
        {
        }
        /// <summary>
        /// After calling beginaction the current state will be added tothe action
        /// </summary>
        /// <param name="state">the new state of the line</param>
        public void AddChange(LineState state)
        {
            if (_currentaction == null)
                throw new Exception("UndoManager current action null");
            _currentaction.States.Add(state);
        }
        public void BeginAction()
        {
            if (_currentaction != null)
                throw new Exception("Attempt to overwrite current undo state");
            _currentaction = new act();
        }
        public void EndAction()
        {
            if (_currentaction == null)
                throw new Exception("UndoManager current action null");
            if (pos != _actions.Count)
            {
                if (pos < 0)
                    pos = 0;
                _actions.RemoveRange(pos, _actions.Count - pos);
            }
            _actions.Add(_currentaction);
            pos = _actions.Count;
            _currentaction = null;
        }

        public bool Undo()
        {
            var needsupdate = false;
            if (_actions.Count > 0 && pos > 0)
            {
                pos--;
                var action = _actions[pos];
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    needsupdate = action.Undo(trk);
                }
                game.InvalidateTrack();
            }
            return needsupdate;
        }

        public bool Redo()
        {
            var needsupdate = false;
            if (_actions.Count > 0 && pos < _actions.Count)
            {
                if (pos < 0)
                    pos = 0;
                var action = _actions[pos];
                pos++;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    needsupdate = action.Redo(trk);
                }
                game.InvalidateTrack();
            }
            return needsupdate;
        }
    }
}