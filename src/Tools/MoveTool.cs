//
//  LineAdjustTool.cs
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

using Gwen.Controls;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using linerider.Lines;
using linerider.UI;
using linerider.Utils;
using System.Diagnostics;

namespace linerider.Tools
{
    public class MoveTool : Tool
    {
        class SelectInfo
        {
            public GameLine line;
            public GameLine clone;
            public bool joint1;
            public bool joint2;
            public List<SelectInfo> snapped;

        }
        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }
        public bool CanLifelock => UI.InputUtils.Check(Hotkey.ToolLifeLock, false);
        private SelectInfo _selection = null;
        private Vector2d _clickstart;
        private bool _lifelocking = false;

        public bool Started
        {
            get
            {
                return _selection != null;
            }
        }


        public MoveTool()
        {
        }


        public void Deselect()
        {
        }

        private void UpdatePlayback(GameLine line)
        {
            //todo does not handle snapped lines for lifelock
            if (line is StandardLine && CanLifelock)
            {
                bool wasdead = game.Track.RenderRider.Crashed || _lifelocking;
                game.Track.NotifyTrackChanged();
                if (wasdead)
                {
                    _lifelocking = true;
                    using (var trk = game.Track.CreateTrackReader())
                    {
                        if (LifeLock(trk, game.Track.Timeline, (StandardLine)line))
                        {
                            Stop();
                        }
                    }
                }
            }
            else
            {
                game.Track.NotifyTrackChanged();
            }
        }
        public void MoveSelection(Vector2d pos)
        {
            if (_selection != null)
            {
                var line = _selection.line;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    var joint1 = line.Position;
                    var joint2 = line.Position2;
                    if (_selection.joint1)
                        joint1 =
                            _selection.clone.Position + (pos - _clickstart);
                    if (_selection.joint2)
                        joint2 =
                            _selection.clone.Position2 + (pos - _clickstart);

                    if (_selection.joint1 != _selection.joint2)
                    {
                        ApplyModifiers(ref joint1, ref joint2);
                    }

                    trk.MoveLine(
                        line,
                        joint1,
                        joint2);

                    foreach (var sl in _selection.snapped)
                    {
                        var snap = sl.line;
                        var snapjoint1 = snap.Position;
                        var snapjoint2 = snap.Position2;
                        if (sl.joint1)
                            snapjoint1 = _selection.joint1 ? joint1 : joint2;
                        if (sl.joint2)
                            snapjoint2 = _selection.joint1 ? joint1 : joint2;

                        trk.MoveLine(
                            snap,
                            snapjoint1,
                            snapjoint2);
                    }
                }
                UpdatePlayback(line);
            }
            game.Invalidate();
        }

        public override void OnMouseDown(Vector2d mousepos)
        {
            Stop();//double check
            var gamepos = ScreenToGameCoords(mousepos);
            using (var trk = game.Track.CreateTrackWriter())
            {
                var line = SelectLine(trk, gamepos);
                if (line != null)
                {
                    var point = Utility.CloserPoint(
                        gamepos,
                        line.Position,
                        line.Position2);
                    //is it a knob?
                    if ((gamepos - point).Length <= line.Width)
                    {
                        _selection = new SelectInfo();
                        _selection.snapped = new List<SelectInfo>();
                        _selection.line = line;
                        _selection.clone = line.Clone();
                        _clickstart = gamepos;
                        if (InputUtils.Check(Hotkey.ToolSelectBothJoints))
                        {
                            _selection.joint1 = _selection.joint2 = true;
                        }
                        else
                        {

                            if (line.Position == point)
                            {
                                _selection.joint1 = true;
                            }
                            else
                            {
                                _selection.joint2 = true;
                                Debug.Assert(
                                    line.Position2 == point,
                                    "Right joint didn't match but was assigned");
                            }
                            var snapcandidates =
                                this.LineEndsInRadius(trk, point, 1);

                            foreach (var v in snapcandidates)
                            {
                                if (v == line)
                                    continue;
                                if (v.Position == point || v.Position2 == point)
                                {
                                    _selection.snapped.Add(new SelectInfo()
                                    {
                                        joint1 = v.Position == point,
                                        joint2 = v.Position2 == point,
                                        line = v,
                                        clone = v.Clone()
                                    });
                                }
                            }
                        }
                        Active = true;
                    }
                    else
                    {
                        //select whole line
                    }
                }
            }
            base.OnMouseDown(gamepos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            Stop();
            base.OnMouseUp(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (Started)
            {
                MoveSelection(ScreenToGameCoords(pos));
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            base.OnMouseRightDown(pos);
        }


        public override void Stop()
        {
            Active = false;
            _lifelocking = false;
            if (_selection != null)
            {
                game.Track.UndoManager.BeginAction();
                game.Track.UndoManager.AddChange(_selection.clone, _selection.line);
                foreach (var s in _selection.snapped)
                {
                    game.Track.UndoManager.AddChange(s.clone, s.line);
                }
                game.Track.UndoManager.EndAction();
            }
            _selection = null;
        }
        private void ApplyModifiers(ref Vector2d joint1, ref Vector2d joint2)
        {
            var start = _selection.joint1 ? joint2 : joint1;
            var end = _selection.joint2 ? joint2 : joint1;
            var currentdelta = _selection.line.Position2 - _selection.line.Position;
            if (UI.InputUtils.Check(Hotkey.ToolAngleLock))
            {
                end = Utility.AngleLock(start, end, Angle.FromVector(currentdelta));
            }
            if (UI.InputUtils.Check(Hotkey.ToolXYSnap))
            {
                end = Utility.SnapToDegrees(start, end);
            }
            if (UI.InputUtils.Check(Hotkey.ToolLengthLock))
            {
                end = Utility.LengthLock(start, end, currentdelta.Length);
            }
            if (_selection.joint2)
                joint2 = end;
            else
                joint1 = end;
        }
    }
}