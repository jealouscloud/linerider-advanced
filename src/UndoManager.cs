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
namespace linerider
{
    public class UndoManager : GameService
    {
        private class act : GameService
        {
            public virtual bool Undo(Track track)
            {
                return false;
            }

            public virtual bool Redo(Track track)
            {
                return false;
            }
        }

        private class lineaction : act
        {
            public List<ExtensionAction> extensions = new List<ExtensionAction>();
            protected Line L;
        }

        private class adjustact : lineaction
        {
            public Vector2d OriginalPos1;
            public Vector2d OriginalPos2;
            public Vector2d NewPos1;
            public Vector2d NewPos2;
            private Vector2d SnapOriginalPos1;
            private Vector2d SnapOriginalPos2;
            private Vector2d SnapNewPos1;
            private Vector2d SnapNewPos2;
            private Line sl;
            private Line pairline;

            public adjustact(Line line, Line paired, Vector2d op1, Vector2d op2, Vector2d np1, Vector2d np2,
                Vector2d pop1, Vector2d pop2, Vector2d pnp1, Vector2d pnp2)
            {
                sl = line;
                pairline = paired;
                L = line;
                OriginalPos1 = op1;
                OriginalPos2 = op2;
                NewPos1 = np1;
                NewPos2 = np2;
                //calc pivot
                if (pairline != null)
                {
                    SnapOriginalPos1 = pop1;
                    SnapOriginalPos2 = pop2;
                    SnapNewPos1 = pnp1;
                    SnapNewPos2 = pnp2;
                }
            }

            public override bool Undo(Track track)
            {
                track.RemoveLineFromGrid(sl);
                if (L is StandardLine)
                {
                    var stl = L as StandardLine;
                    stl.Start = OriginalPos1;
                    stl.End = OriginalPos2;
                }
                else
                {
                    L.Position = OriginalPos1;
                    L.Position2 = OriginalPos2;
                }
                if (L is StandardLine)
                {
                    ((StandardLine)L).CalculateConstants();
                }
                track.AddLineToGrid(sl);
                game.Track.LineChanged(sl);
                if (pairline != null)
                {
                    track.RemoveLineFromGrid(pairline);
                    pairline.Position = SnapOriginalPos1;
                    pairline.Position2 = SnapOriginalPos2;
                    if (pairline is StandardLine)
                    {
                        ((StandardLine)pairline).CalculateConstants();
                        //StandardLine.TryConnectLines(track, (StandardLine)L, (StandardLine)pairline);
                    }
                    track.AddLineToGrid(pairline);
                    game.Track.LineChanged(pairline);
                }
                track.ChangeMade(NewPos1, NewPos2);
                track.ChangeMade(OriginalPos1, OriginalPos2);
                return true;
            }

            public override bool Redo(Track track)
            {
                track.RemoveLineFromGrid(sl);
                if (L is StandardLine)
                {
                    var stl = L as StandardLine;
                    stl.Start = NewPos1;
                    stl.End = NewPos2;
                }
                else
                {
                    L.Position = NewPos1;
                    L.Position2 = NewPos2;
                }
                if (L is StandardLine)
                {
                    ((StandardLine)L).CalculateConstants();
                }
                track.AddLineToGrid(sl);
				game.Track.LineChanged(sl);
                if (pairline != null)
                {
                    track.RemoveLineFromGrid(pairline);

                    pairline.Position = SnapNewPos1;
                    pairline.Position2 = SnapNewPos2;
                    if (pairline is StandardLine)
                    {
                        ((StandardLine)pairline).CalculateConstants();
                        // StandardLine.TryConnectLines(track, (StandardLine)L, (StandardLine)pairline);
                    }
                    track.AddLineToGrid(pairline);
                    game.Track.LineChanged(pairline);
                }
                track.ChangeMade(NewPos1, NewPos2);
                track.ChangeMade(OriginalPos1, OriginalPos2);
                return true;
            }
        }

        private class AddAction : lineaction
        {
            public AddAction(Line l)
            {
                L = l;
            }

            public override bool Undo(Track track)
            {
                game.Track.RemoveLine(L);
                return L.GetLineType() != LineType.Scenery;
            }

            public override bool Redo(Track track)
            {
                game.Track.AddLine(L);
                return L.GetLineType() != LineType.Scenery;
            }
        }

        private class RemoveAction : lineaction
        {
            public RemoveAction(Line l)
            {
                L = l;
            }

            public override bool Redo(Track track)
            {
				game.Track.RemoveLine(L);
                return L.GetLineType() != LineType.Scenery;
            }

            public override bool Undo(Track track)
            {
                game.Track.AddLine(L);
                return L.GetLineType() != LineType.Scenery;
            }
        }

        private class ExtensionAction : act
        {
            private StandardLine L;
            private StandardLine L2;
            private bool Add;

            public ExtensionAction(StandardLine l, StandardLine l2, bool set)
            {
                L = l;
                L2 = l2;
                Add = set;
            }

            public override bool Redo(Track track)
            {
                if (Add)
                {
                    game.Track.TryConnectLines(L, L2, false);
                }
                else
                {
                    game.Track.TryDisconnectLines(L, L2, false);
                }
                return true;
            }

            public override bool Undo(Track track)
            {
                if (Add)
                {
                    game.Track.TryDisconnectLines(L, L2, false);
                }
                else
                {
                    game.Track.TryConnectLines(L, L2, false);
                }
                return true;
            }
        }

        private int pos;
        private List<act> _actions = new List<act>();
        private bool _working;
        private Track _track;
        public UndoManager(Track track)
        {
            if (track == null)
                throw new System.NullReferenceException();
            _track = track;
        }

        public void AddLineAdjustment(Line l, Line paired, Vector2d op1, Vector2d op2, Vector2d np1, Vector2d np2,
            Vector2d pop1, Vector2d pop2, Vector2d pnp1, Vector2d pnp2)
        {
            if (!_working)
            {
                var act = new adjustact(l, paired, op1, op2, np1, np2, pop1, pop2, pnp1, pnp2);
                if (pos != _actions.Count)
                {
                    if (pos < 0)
                        pos = 0;
                    _actions.RemoveRange(pos, _actions.Count - pos);
                }
                _actions.Add(act);
                pos = _actions.Count;
            }
        }

        public void AddExtensionChange(StandardLine l1, StandardLine l2, bool add)
        {
            if (!_working)
            {
                if (_actions.Count == 0)
                    return;
                var ac = new ExtensionAction(l1, l2, add);
                var act = (lineaction)_actions[_actions.Count - 1];
                act.extensions.Add(ac);
            }
        }

        public void AddLine(Line l)
        {
            if (!_working)
            {
                var ac = new AddAction(l);
                if (pos != _actions.Count)
                {
                    if (pos < 0)
                        pos = 0;
                    _actions.RemoveRange(pos, _actions.Count - pos);
                }
                _actions.Add(ac);
                pos = _actions.Count;
            }
        }

        public void RemoveLine(Line l)
        {
            if (!_working)
            {
                var ac = new RemoveAction(l);
                if (pos != _actions.Count)
                {
                    if (pos < 0)
                        pos = 0;
                    _actions.RemoveRange(pos, _actions.Count - pos);
                }
                _actions.Add(ac);
                pos = _actions.Count;
            }
        }

        public bool Undo()
        {
            var needsupdate = false;
            if (_actions.Count > 0 && pos > 0)
            {
                _working = true;
                pos--;
                var action = _actions[pos];
                if (action.Undo(_track))
                    needsupdate = true;
                var la = action as lineaction;
                if (la != null)
                {
                    foreach (var ext in la.extensions)
                    {
                        ext.Undo(_track);
                        needsupdate = true;
                    }
                }
                _working = false;
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
                _working = true;
                if (action.Redo(_track))
                    needsupdate = true;
                var la = action as lineaction;
                if (la != null)
                {
                    foreach (var ext in la.extensions)
                    {
                        ext.Redo(_track);
                        needsupdate = true;
                    }
                }
                _working = false;
            }
            return needsupdate;
        }
    }
}