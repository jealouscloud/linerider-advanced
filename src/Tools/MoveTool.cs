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
using System.Drawing;
using linerider.Game;
using linerider.UI;
using linerider.Utils;
using System.Diagnostics;
using linerider.Rendering;

namespace linerider.Tools
{
    public class MoveTool : Tool
    {
        public override string Tooltip
        {
            get
            {
                if (_active && _selection != null && _selection.line != null)
                {
                    var vec = _selection.line.GetVector();
                    var len = vec.Length;
                    var angle = Angle.FromVector(vec);
                    angle.Degrees += 90;
                    string tooltip = "length: " + Math.Round(len, 2) +
                    " \n" +
                    "angle: " + Math.Round(angle.Degrees, 2) + "° ";
                    if (_selection.line.Type != LineType.Scenery)
                    {
                        tooltip += "\n" +
                        "ID: " + _selection.line.ID + " ";
                    }
                    return tooltip;
                }
                return "";
            }
        }
        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }
        public bool CanLifelock => UI.InputUtils.Check(Hotkey.ToolLifeLock) &&
        CurrentTools.SelectedTool == this;
        private Vector2d _clickstart;
        private bool _lifelocking = false;
        private LineSelection _selection;
        private bool _active = false;
        private GameLine _hoverline = null;
        private bool _hoverknob = false;
        private bool _hoverknobjoint1;
        private Stopwatch _hovertime = new Stopwatch();
        private bool _hoverclick = false;
        public override bool Active
        {
            get
            {
                return _active;
            }
            protected set
            {
                Debug.Fail($"Cannot set MoveTool.Active, use {nameof(_active)} instead");
            }
        }
        public MoveTool()
        {
        }
        private void UpdatePlayback(GameLine line)
        {
            if (line is StandardLine && CanLifelock)
            {
                game.Track.NotifyTrackChanged();
                using (var trk = game.Track.CreateTrackReader())
                {
                    if (!LifeLock(trk, game.Track.Timeline, (StandardLine)line))
                    {
                        _lifelocking = true;
                    }
                    else if (_lifelocking)
                    {
                        DropLine();
                    }
                }

            }
            else
            {
                game.Track.NotifyTrackChanged();
            }
        }
        private bool SelectLine(Vector2d gamepos)
        {
            using (var trk = game.Track.CreateTrackReader())
            {
                var line = SelectLine(trk, gamepos, out bool knob);
                if (line != null)
                {
                    _clickstart = gamepos;
                    _active = true;
                    //is it a knob?
                    if (knob)
                    {
                        if (InputUtils.Check(Hotkey.ToolSelectBothJoints))
                        {
                            _selection = new LineSelection(line, bothjoints: true);
                        }
                        else
                        {
                            var knobpos = Utility.CloserPoint(
                                gamepos,
                                line.Position,
                                line.Position2);
                            _selection = new LineSelection(line, knobpos);
                            _hoverclick = true;
                            _hovertime.Restart();
                            foreach (var snap in LineEndsInRadius(trk, knobpos, 1))
                            {
                                if ((snap.Position == knobpos ||
                                    snap.Position2 == knobpos) &&
                                    snap != line)
                                {
                                    _selection.snapped.Add(new LineSelection(snap, knobpos));
                                }
                            }
                        }
                        return true;
                    }
                    else
                    {
                        _selection = new LineSelection(line, true);
                        return true;
                    }
                }
            }
            return false;
        }
        public void MoveSelection(Vector2d pos)
        {
            if (_selection != null)
            {
                var line = _selection.line;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    var joint1 = _selection.joint1
                        ? _selection.clone.Position + (pos - _clickstart)
                        : line.Position;
                    var joint2 = _selection.joint2
                        ? _selection.clone.Position2 + (pos - _clickstart)
                        : line.Position2;
                    ApplyModifiers(ref joint1, ref joint2);

                    trk.MoveLine(
                        line,
                        joint1,
                        joint2);

                    foreach (var sl in _selection.snapped)
                    {
                        var snap = sl.line;
                        var snapjoint = _selection.joint1 ? joint1 : joint2;
                        trk.MoveLine(
                            snap,
                            sl.joint1 ? snapjoint : snap.Position,
                            sl.joint2 ? snapjoint : snap.Position2);
                    }
                }
                UpdatePlayback(_selection.line);
            }
            game.Invalidate();
        }
        private void UpdateHoverline(Vector2d gamepos)
        {
            var oldhover = _hoverline;
            var oldhoverknob = _hoverknob;
            _hoverline = null;
            if (!_active)
            {
                using (var trk = game.Track.CreateTrackReader())
                {
                    var line = SelectLine(trk, gamepos, out bool knob);
                    if (line != null)
                    {
                        _hoverline = line;
                        if (knob)
                        {
                            var point = Utility.CloserPoint(
                                gamepos,
                                line.Position,
                                line.Position2);

                            var joint1 = point == line.Position;

                            if (_hoverline != oldhover ||
                            _hoverknobjoint1 != joint1 ||
                            _hoverknob != knob)
                            {
                                _hoverclick = false;
                                _hovertime.Restart();
                            }
                            _hoverknobjoint1 = joint1;
                        }
                        _hoverknob = knob;
                    }
                }
            }
        }

        public override void OnMouseDown(Vector2d mousepos)
        {
            base.OnMouseDown(mousepos);
            var gamepos = ScreenToGameCoords(mousepos);

            Stop();//double check
            if (!SelectLine(gamepos))
            {
                CurrentTools.SetTool(CurrentTools.SelectTool);
                CurrentTools.SelectTool.OnMouseDown(mousepos);
                IsMouseDown = false;
                _hoverline = null;
            }
            else
            {
                UpdateHoverline(gamepos);
            }
        }
        public override void OnMouseUp(Vector2d pos)
        {
            DropLine();
            UpdateHoverline(ScreenToGameCoords(pos));
            base.OnMouseUp(pos);
        }
        public override void OnMouseMoved(Vector2d pos)
        {
            UpdateHoverline(ScreenToGameCoords(pos));
            if (_active)
            {
                MoveSelection(ScreenToGameCoords(pos));
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            Stop();//double check
            var gamepos = ScreenToGameCoords(pos);
            using (var trk = game.Track.CreateTrackWriter())
            {
                var line = SelectLine(trk, gamepos, out bool knob);
                if (line != null && line.Type != LineType.Scenery)
                {
                    game.Canvas.ShowLineWindow(line, (int)pos.X, (int)pos.Y);
                }
            }
            base.OnMouseRightDown(pos);
        }
        public override void OnChangingTool()
        {
            Stop();
        }
        public override void Render()
        {
            if (_hoverline != null)
            {
                DrawHover(
                    _hoverline,
                     _hoverknob && _hoverknobjoint1,
                     _hoverknob && !_hoverknobjoint1,
                     false);
            }
            if (_active)
            {
                DrawHover(
                    _selection.line,
                    _selection.joint1,
                    _selection.joint2,
                    true);
            }
            base.Render();
        }
        private void DrawHover(GameLine line,
            bool knob1, bool knob2, bool selected = false)
        {
            var start = line.Position;
            var end = line.Position2;
            var width = line.Width;
            var elapsed = _hovertime.ElapsedMilliseconds;
            int animtime = 250;
            if (_hovertime.IsRunning)
            {
                if (elapsed > animtime * 2)
                {
                    if (_hoverclick)
                        _hovertime.Stop();
                    else
                        _hovertime.Stop();
                }
                game.Track.Invalidate();
            }
            float hoverratio;
            if (_hoverclick)
            {
                animtime = 75;
                elapsed += 75 / 4;
                hoverratio = Math.Min(animtime, elapsed) / (float)animtime;
            }
            else
            {
                hoverratio = Math.Min((Math.Min(animtime, elapsed) / (float)animtime), 0.5f);
            }
            var both = knob1 == knob2 == true;
            var linealpha = both ? 64 : 48;
            if (selected && both)
                linealpha += 16;
            GameRenderer.RenderRoundedLine(
                start,
                end,
                Color.FromArgb(linealpha, Color.FromArgb(127, 127, 127)),
                (width * 2));

            bool canlifelock = CanLifelock && line.Type != LineType.Scenery;
            GameRenderer.DrawKnob(start, knob1, canlifelock, width, hoverratio);
            GameRenderer.DrawKnob(end, knob2, canlifelock, width, hoverratio);

        }
        private void DropLine()
        {
            if (_active)
            {
                _hoverline = _selection.line;
                _hoverknob = !_selection.BothJoints;
                _hoverknobjoint1 = _selection.joint1;
            }
            _lifelocking = false;
            if (_active)
            {
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
                game.Invalidate();
            }
            _active = false;
            _selection = null;
        }
        public override void Cancel()
        {
            Stop();
        }
        public override void Stop()
        {
            DropLine();
            _hoverline = null;
            _hoverclick = false;
        }
        private void ApplyModifiers(ref Vector2d joint1, ref Vector2d joint2)
        {
            bool both = _selection.joint1 && _selection.joint2;
            if (both)
            {
                var axis = UI.InputUtils.CheckPressed(Hotkey.ToolAxisLock);
                var perpendicularaxis = UI.InputUtils.CheckPressed(Hotkey.ToolPerpendicularAxisLock);
                if (axis || perpendicularaxis)
                {
                    var angle = Angle.FromVector(_selection.clone.GetVector());
                    if (perpendicularaxis)
                    {
                        angle.Degrees -= 90;
                    }
                    joint1 = Utility.AngleLock(_selection.line.Position, joint1, angle);
                    joint2 = Utility.AngleLock(_selection.line.Position2, joint2, angle);
                }
            }
            else
            {
                var start = _selection.joint1 ? joint2 : joint1;
                var end = _selection.joint2 ? joint2 : joint1;
                if (UI.InputUtils.Check(Hotkey.ToolAngleLock))
                {
                    end = Utility.AngleLock(start, end, Angle.FromVector(_selection.clone.GetVector()));
                }
                if (UI.InputUtils.CheckPressed(Hotkey.ToolXYSnap))
                {
                    end = Utility.SnapToDegrees(start, end);
                }
                if (UI.InputUtils.Check(Hotkey.ToolLengthLock))
                {
                    var currentdelta = _selection.line.Position2 - _selection.line.Position;
                    end = Utility.LengthLock(start, end, currentdelta.Length);
                }
                if (_selection.joint2)
                    joint2 = end;
                else
                    joint1 = end;
            }
        }
    }
}