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
using linerider.Game;
namespace linerider
{
    public class UndoManager : GameService
    {
        private class act : GameService
        {
            public List<GameLine> States;
            public act()
            {
                States = new List<GameLine>();
            }
            private bool DoAction(TrackWriter track, GameLine beforeact, GameLine afteract)
            {
                if (beforeact == null && afteract == null)
                    throw new ArgumentNullException(
                        "undo action with no values");
                // remove line
                if (afteract == null)
                {
                    track.RemoveLine(beforeact);
                }
                // add line
                else if (beforeact == null)
                {
                    track.AddLine(afteract.Clone());
                }
                //move action
                else
                {
                    track.ReplaceLine(beforeact, afteract.Clone());
                }
                return !(beforeact is SceneryLine);

            }
            /// <summary>
            /// undo previous action, returns true if physics are changed
            /// </summary>
            public void Undo(TrackWriter track)
            {
                bool physchanged = false;
                for (int i = States.Count - 1; i > 0; i -= 2)
                {
                    physchanged |= DoAction(track, States[i], States[i - 1]);
                }

                if (physchanged)
                    track.NotifyTrackChanged();
            }

            public void Redo(TrackWriter track)
            {
                bool physchanged = false;
                for (int i = 0; i < States.Count - 1; i += 2)
                {
                    physchanged |= DoAction(track, States[i], States[i + 1]);
                }
                if (physchanged)
                    track.NotifyTrackChanged();
            }
            public override string ToString()
            {
                if (States != null && States.Count != 0)
                {
                    string ret = "";
                    foreach (var state in States)
                    {
                        ret += state.ToString() + "|";
                    }
                    return ret;
                }
                return base.ToString();
            }
        }
        public int ActionPosition { get; private set; }
        private List<act> _actions = new List<act>();
        private act _currentaction;
        const int MaximumBufferSize = 10000;
        public UndoManager()
        {
        }
        /// <summary>
        /// After calling beginaction the current state will be added tothe action
        /// </summary>
        public void AddChange(GameLine before, GameLine after)
        {
            if (_currentaction == null)
                throw new Exception("UndoManager current action null");
            _currentaction.States.Add(before?.Clone());
            _currentaction.States.Add(after?.Clone());
        }
        public void BeginAction()
        {
            if (_currentaction != null)
                throw new Exception("Attempt to overwrite current undo state");
            _currentaction = new act();
        }
        public void CancelAction()
        {
            if (_currentaction == null)
                throw new Exception("UndoManager current action null");
            DoAction(_currentaction, true);
            _currentaction = null;
        }
        public void EndAction()
        {
            if (_currentaction == null)
                throw new Exception("UndoManager current action null");
            if (ActionPosition != _actions.Count)
            {
                if (ActionPosition < 0)
                    ActionPosition = 0;
                _actions.RemoveRange(ActionPosition, _actions.Count - ActionPosition);
            }
            if (_actions.Count > MaximumBufferSize)
            {
                _actions.RemoveRange(0, _actions.Count - (MaximumBufferSize / 2));
            }
            _actions.Add(_currentaction);
            ActionPosition = _actions.Count;
            _currentaction = null;
        }

        public void Undo()
        {
            if (_actions.Count > 0 && ActionPosition > 0)
            {
                ActionPosition--;
                var action = _actions[ActionPosition];
                DoAction(action, true);
            }
        }

        public void Redo()
        {
            if (_actions.Count > 0 && ActionPosition < _actions.Count)
            {
                if (ActionPosition < 0)
                    ActionPosition = 0;
                var action = _actions[ActionPosition];
                ActionPosition++;
                DoAction(action, false);
            }
        }
        private void DoAction(act action, bool undo)
        {
            using (var trk = game.Track.CreateTrackWriter())
            {
                trk.DisableUndo();
                trk.DisableExtensionUpdating();
                if (undo)
                    action.Undo(trk);
                else
                    action.Redo(trk);
            }
            game.Track.NotifyTrackChanged();
            game.Track.Invalidate();
        }
    }
}