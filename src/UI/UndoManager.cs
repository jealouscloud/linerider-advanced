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
                    track.AddLine(afteract);
                }
                //move action
                else
                {
                    track.ReplaceLine(beforeact, afteract);
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
        }
        private int pos;
        private List<act> _actions = new List<act>();
        private act _currentaction;
        const int MaximumBufferSize = 10000;
        /// <summary>
        /// Returns true if any changes have currently been made to the track
        /// Basically for autosave.
        /// </summary>
        public bool HasChanges => (pos != 0 || _actions.Count > 100);
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
            if (_actions.Count > MaximumBufferSize)
            {
                _actions.RemoveRange(0, _actions.Count - (MaximumBufferSize / 2));
            }
            _actions.Add(_currentaction);
            pos = _actions.Count;
            _currentaction = null;
        }

        public void Undo()
        {
            if (_actions.Count > 0 && pos > 0)
            {
                pos--;
                var action = _actions[pos];
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    trk.DisableExtensionUpdating();
                    action.Undo(trk);
                }
                game.Track.NotifyTrackChanged();
                game.Track.Invalidate();
            }
        }

        public void Redo()
        {
            if (_actions.Count > 0 && pos < _actions.Count)
            {
                if (pos < 0)
                    pos = 0;
                var action = _actions[pos];
                pos++;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    trk.DisableExtensionUpdating();
                    action.Redo(trk);
                }
                game.Track.Invalidate();
            }
        }
    }
}