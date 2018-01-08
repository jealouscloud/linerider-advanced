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

namespace linerider
{
    public class LineAdjustTool : Tool
    {
        #region Fields

        public bool LifeLock = false;

        private Joint _joint = 0;

        private StandardLine _line;

        private StandardLine _next;

        private Line _nonphysicalline;

        private Vector2d _originalPos1;

        private Vector2d _originalPos2;

        private StandardLine _prev;

        private Joint _snapjoint = 0;

        private Line _snappedline;

        private StandardLine _snext;

        private StandardLine _sprev;

        private bool _started;

        private Vector2d _startPos;

        private Vector2d _snaporiginalpos1;

        private Vector2d _snaporiginalpos2;

        private WindowControl selectionwindow;

        #endregion Fields

        #region Properties

        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }

        public bool Started
        {
            get
            {
                return _started;
            }
        }

        #endregion Properties

        #region Constructors

        public LineAdjustTool()
        {
        }

        #endregion Constructors

        #region Methods

        public void Deselect()
        {
        }

        public void MoveLine(Vector2d pos)
        {
            if (_started)
            {
                var keyboard = Keyboard.GetState();
                var gamepos = MouseCoordsToGame(pos);
                var joint = CompliantJoint(_line, _joint);
                bool ispaired = _snappedline != null && !joint.HasFlag(Joint.Both);
                var oldspos = _snappedline?.Position;
                var oldspos2 = _snappedline?.Position2;
                bool updatetrack = false;
                using (game.Track.EnterTrackReadWrite())
                {
                    if (_nonphysicalline != null)
                    {

                        game.Track.RemoveLineFromGrid(_nonphysicalline);
                        if (_snappedline != null)
                            game.Track.RemoveLineFromGrid(_snappedline);
                        if (_joint.HasFlag(Joint.Left))
                        {
                            _nonphysicalline.Position = _originalPos1 + (gamepos - _startPos);
                            if (_joint != Joint.Both && game.ShouldXySnap())
                                _nonphysicalline.Position = SnapXY(_nonphysicalline.Position2, _nonphysicalline.Position);
                            if (keyboard[Key.ShiftLeft] || keyboard[Key.ShiftRight])
                                _nonphysicalline.Position = AngleLock(_nonphysicalline.Position,
                                    _nonphysicalline.Position2);
                            if (ispaired)
                            {
                                SetSnapLinePosition(_nonphysicalline.Position);
                            }
                        }
                        if (_joint.HasFlag(Joint.Right))
                        {
                            _nonphysicalline.Position2 = _originalPos2 + (gamepos - _startPos);
                            if (_joint != Joint.Both && game.ShouldXySnap())
                                _nonphysicalline.Position2 = SnapXY(_nonphysicalline.Position,
                                    _nonphysicalline.Position2);
                            if (keyboard[Key.ShiftLeft] || keyboard[Key.ShiftRight])
                                _nonphysicalline.Position2 = AngleLock(_nonphysicalline.Position2,
                                    _nonphysicalline.Position);

                            if (ispaired)
                            {
                                SetSnapLinePosition(_nonphysicalline.Position2);
                            }
                        }
                        if (_snappedline != null)
                        {
                            _snappedline.diff = _snappedline.Position2 - _snappedline.Position;
                            game.Track.AddLineToGrid(_snappedline);
                            game.Track.LineChanged(_snappedline);
                            if (_snappedline != null && _snappedline is StandardLine)
                            {
                                _snappedline.CalculateConstants();
                                game.Track.ChangeMade(_snappedline.Position, _snappedline.Position2);
                                game.Track.ChangeMade(oldspos.Value, oldspos2.Value);
                            }
                        }
                        _nonphysicalline.diff = _nonphysicalline.Position2 - _nonphysicalline.Position;
                        game.Track.AddLineToGrid(_nonphysicalline);
                        game.Track.LineChanged(_nonphysicalline);
                        game.Invalidate();
                        return;
                    }
                    var oldpos = _line.Position;
                    var oldpos2 = _line.Position2;
                    if (DoLifelock())
                    {
                        Stop();
                        game.Canvas.RemoveTooltip(null);
                        return;
                    }
                    game.Track.RemoveLineFromGrid(_line);
                    if (ispaired)
                    {
                        game.Track.RemoveLineFromGrid(_snappedline);
                    }
                    var snapstl = _snappedline as StandardLine;
                    if (joint.HasFlag(Joint.Left))
                    {
                        _line.Start = _originalPos1 + (gamepos - _startPos);
                        if (_joint != Joint.Both && game.ShouldXySnap())
                            _line.Start = SnapXY(_line.End, _line.Start);
                        if (keyboard[Key.Tab])
                            _line.Start = LengthLock(_line.End, _line.Start);
                        else if (keyboard[Key.ShiftLeft] || keyboard[Key.ShiftRight])
                            _line.Start = AngleLock(_line.Start, _line.End);
                        if (ispaired)
                        {
                            SetSnapLinePosition(_line.Start);
                            snapstl.CalculateConstants();
                        }
                        _line.CalculateConstants();
                    }
                    if (joint.HasFlag(Joint.Right))
                    {
                        _line.End = _originalPos2 + (gamepos - _startPos);
                        if (_joint != Joint.Both && game.ShouldXySnap())
                            _line.End = SnapXY(_line.Start, _line.End);

                        if (keyboard[Key.Tab])
                            _line.End = LengthLock(_line.Start, _line.End);
                        else if (keyboard[Key.ShiftLeft] || keyboard[Key.ShiftRight])
                            _line.End = AngleLock(_line.End, _line.Start);

                        if (ispaired)
                        {
                            SetSnapLinePosition(_line.End);
                            snapstl.CalculateConstants();
                        }
                        _line.CalculateConstants();
                    }
                    if (ispaired)
                    {
                        game.Track.AddLineToGrid(_snappedline);
                    }
                    game.Track.AddLineToGrid(_line);
                    if ((Settings.LiveAdjustment || LifeLock) && game.Track.Animating)
                    {
                        if (_snappedline != null && _snappedline is StandardLine)
                        {
                            game.Track.TryConnectLines(_line as StandardLine, _snappedline as StandardLine);
                            game.Track.ChangeMade(_snappedline.Position, _snappedline.Position2);
                            game.Track.ChangeMade(oldspos.Value, oldspos2.Value);
                        }
                        game.Track.ChangeMade(_line.Position, _line.Position2);
                        game.Track.ChangeMade(oldpos, oldpos2);
                        updatetrack = true;
                    }
                    game.Track.LineChanged(_line);
                    if (ispaired)
                    {
                        game.Track.LineChanged(_snappedline);
                    }
                    game.Invalidate();
                }
                if (updatetrack)
                {
                    game.Track.TrackUpdated();
                }
            }
        }

        public override bool OnKeyDown(Key k)
        {
            if (_started)
            {
                var state = Mouse.GetCursorState();
                var pos = new Vector2(state.X, state.Y);
                const float change = 1;
                switch (k)
                {
                    case Key.Up:
                        pos.Y -= change;
                        break;

                    case Key.Down:
                        pos.Y += change;
                        break;

                    case Key.Right:
                        pos.X += change;
                        break;

                    case Key.Left:
                        pos.X -= change;
                        break;

                    default:
                        return false;
                }
                Mouse.SetPosition(pos.X, pos.Y);
                return true;
            }
            return base.OnKeyDown(k);
        }

        public override void OnMouseDown(Vector2d pos)
        {
            SelectLine(pos);
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                MoveLine(pos);
                if (_started)//moveline can call stop, so check again
                {
                    game.Canvas.RemoveTooltip(null);
                    UpdateTooltip();
                }
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            var gamepos = MouseCoordsToGame(pos);
            var ssnap = Snap(gamepos);
            var snap = ssnap as StandardLine;
            if (snap != null)
            {
                Vector2d knobpos = Vector2d.Zero;
                if ((snap.Position - gamepos).Length < (snap.Position2 - gamepos).Length)
                {
                    knobpos = snap.Position;
                }
                else
                {
                    knobpos = snap.Position2;
                }
                Select(snap, pos);
            }
            else
            {
                snap = SnapLine(gamepos) as StandardLine;
                if (snap != null)
                {
                    Select(snap, pos);
                }
            }
            base.OnMouseRightDown(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            base.OnMouseUp(pos);
            if (_started)
            {
                MoveLine(pos);
                if (_started)//moveline can call stop
                {
                    if (!Settings.LiveAdjustment)//if moveline didn't call trackupdated we should
                    {
                        game.Track.ChangeMade(_line.Position, _line.Position2);
                        Stop();
                        game.Track.TrackUpdated();
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        public void Select(StandardLine line, Vector2d position)
        {
            if (selectionwindow != null)
            {
                if (selectionwindow.UserData != line)
                {
                    selectionwindow.Close();
                    selectionwindow = null;
                }
            }
            if (selectionwindow == null)
            {
                selectionwindow = new WindowControl(game.Canvas, "Line Settings", false);
                selectionwindow.MakeModal(true);
                selectionwindow.UserData = line;
                selectionwindow.DeleteOnClose = true;
                selectionwindow.DisableResizing();
                selectionwindow.Height = 170;
                selectionwindow.Width = 150;

                ControlBase container1 = new ControlBase(selectionwindow);
                container1.Dock = Gwen.Pos.Fill;
                if (line.GetLineType() != LineType.Scenery)
                {
                    LabeledCheckBox btn = new LabeledCheckBox(container1);
                    btn.Dock = Gwen.Pos.Top;
                    btn.Text = "Inverse";
                    btn.IsChecked = line.inv;
                    btn.CheckChanged += (o, e) =>
                    {
                        var caller = (LabeledCheckBox)o;
                        line.inv = caller.IsChecked;
                        line.CalculateConstants();
                        game.Track.TrackUpdated();
                        game.Invalidate();
                    };
                    LineTrigger tr = (LineTrigger)line.Trigger ?? new LineTrigger();

                    var gb = new PropertyTree(container1);
                    gb.Height = 110;
                    gb.Dock = Gwen.Pos.Top;

                    PropertyTree table = new PropertyTree(gb);

                    table.Name = "triggers";
                    table.Dock = Gwen.Pos.Fill;
                    table.Height = 100;

                    var row = table.Add("Zoom Trigger");
                    var enabled = row.Add("Enabled", new Gwen.Controls.Property.Check(table));
                    enabled.Value = tr.Zoomtrigger ? "1" : "0";
                    enabled.ValueChanged += (o, e) =>
                    {
                        if (enabled.Value == "1")
                        {
                            tr.Zoomtrigger = true;
                            tr.ZoomTarget = float.Parse(((PropertyRow)container1.FindChildByName("Zoom", true)).Value);
                            tr.ZoomFrames = int.Parse(((PropertyRow)container1.FindChildByName("ZoomFrames", true)).Value);
                            line.Trigger = tr;
                        }
                        else
                        {
                            tr.Zoomtrigger = false;
                            if (!tr.Enabled)
                            {
                                line.Trigger = null;
                            }
                        }
                        game.Track.LineChanged(line);
                    };
                    var prop = row.Add("Zoom");
                    prop.Name = "Zoom";
                    prop.Value = (enabled.Value == "1" ? tr.ZoomTarget : 1).ToString();
                    prop.ValueChanged += (o, e) =>
                    {
                        var caller = (PropertyRow)o;
                        float val = 0;
                        if (float.TryParse(caller.Value, out val) && val >= 0.1 && val <= 24)
                        {
                            caller.LabelColor = System.Drawing.Color.Black;
                            tr.ZoomTarget = val;
                        }
                        else
                        {
                            caller.LabelColor = System.Drawing.Color.Red;
                        }
                    };
                    prop = row.Add("Frames");
                    prop.Name = "ZoomFrames";
                    prop.Value = (enabled.Value == "1" ? tr.ZoomFrames : 40).ToString();
                    prop.ValueChanged += (o, e) =>
                    {
                        var caller = (PropertyRow)o;
                        int val = 0;
                        if (int.TryParse(caller.Value, out val) && val >= 1 && val < 10000)
                        {
                            caller.LabelColor = System.Drawing.Color.Black;
                            tr.ZoomFrames = val;
                        }
                        else
                        {
                            caller.LabelColor = System.Drawing.Color.Red;
                        }
                    };
                }

                if (line.GetLineType() == LineType.Red)
                {
                    selectionwindow.Height += 30;
                    NoDecimalNUD nud = new NoDecimalNUD(container1);
                    var marg = nud.Margin;
                    marg.Top = 5;
                    marg.Left = marg.Right = 1;
                    marg.Left = 70;
                    marg.Bottom = 10;
                    nud.Margin = marg;
                    nud.Dock = Gwen.Pos.Top;
                    nud.Min = 1;
                    nud.Max = 3;
                    nud.Value = (line as RedLine).Multiplier;
                    nud.ValueChanged += nud_redlinemultiplier_ValueChanged;
                    nud.UserData = line;
                    Label l = new Label(container1);
                    l.Y = 137;
                    l.Text = "Multiplier";
                    selectionwindow.Height += 25;


                    nud = new NoDecimalNUD(container1);
                    marg = nud.Margin;
                    marg.Top = 5;
                    marg.Left = marg.Right = 1;
                    marg.Left = 70;
                    marg.Bottom = 10;
                    nud.Margin = marg;
                    nud.Dock = Gwen.Pos.Top;
                    var lines = game.Track.GetLinesInRect(new FloatRect((Vector2)line.Position, new Vector2(1, 1)), false);
                    List<Line> redlines = new List<Line>();
                    foreach (var red in lines)
                    {
                        if (red.Position == line.Position && red.Position2 == line.Position2)
                        {
                            redlines.Add(red);
                        }
                    }
                    nud.Min = 1;
                    nud.Max = 9999;
                    redlines.Sort(new Linecomparer());
                    nud.Value = redlines.Count;
                    nud.ValueChanged += (o, e) =>
                    {
                        var diff = nud.Value - redlines.Count;
                        if (diff < 0)
                        {
                            for (int i = 0; i > diff; i--)
                            {
                                game.Track.RemoveLine(redlines[(int)((redlines.Count - 1))]);
                                redlines.RemoveAt(redlines.Count - 1);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < diff; i++)
                            {
                                var red = new RedLine(line.Position, line.Position2, line.inv) { Multiplier = ((RedLine)line).Multiplier };
                                game.Track.AddLine(red);
                                redlines.Add(red);
                            }
                        }
                        game.Track.TrackUpdated();
                    };
                    nud.UserData = line;
                    l = new Label(container1);
                    l.Y = 137 + 35;
                    l.Text = "Multilines";
                }
                selectionwindow.IsHiddenChanged += Selectionwindow_IsHiddenChanged;
                selectionwindow.Show();
                selectionwindow.X = (int)position.X;
                selectionwindow.Y = (int)position.Y;
                game.Cursor = game.Cursors["default"];
            }
        }

        public void SelectLine(Vector2d pos)
        {
            _started = true;
            var gamepos = MouseCoordsToGame(pos);
            var ssnap = Snap(gamepos);
            var snap = ssnap as StandardLine;
            var keyboard = Keyboard.GetState();
            if (snap == null)
            {
                if (ssnap == null)
                {
                    _started = false;
                    return;
                }
                else//scenery
                {
                    if ((ssnap.Position - gamepos).Length < (ssnap.Position2 - gamepos).Length)
                    {
                        _joint = Joint.Left;
                        _startPos = ssnap.Position;
                    }
                    else
                    {
                        _joint = Joint.Right;
                        _startPos = ssnap.Position2;
                    }
                    _snappedline = Snap(_startPos, ssnap);
                    if (_snappedline is StandardLine)
                        _snappedline = null;
                    if (_snappedline != null)
                    {
                        if ((_snappedline.Position - _startPos).Length < (_snappedline.Position2 - _startPos).Length)
                        {
                            _snapjoint = Joint.Left;
                        }
                        else
                        {
                            _snapjoint = Joint.Right;
                        }
                        _snaporiginalpos1 = _snappedline.Position;
                        _snaporiginalpos2 = _snappedline.Position2;
                    }
                    _nonphysicalline = ssnap;
                    _originalPos1 = _nonphysicalline.Position;
                    _originalPos2 = _nonphysicalline.Position2;
                    UpdateTooltip();
                    game.Invalidate();
                    if (keyboard[Key.ControlLeft] || keyboard[Key.ControlRight])
                    {
                        _joint = Joint.Both;
                        _snappedline = null;
                    }
                    return;
                }
            }
            if (keyboard[Key.AltLeft] || keyboard[Key.AltRight])
            {
                if (game.Track.Animating)
                {
                    using (game.Track.EnterPlayback())
                    {
                        if (!DoLifelock())
                            LifeLock = true;
                        else
                        {
                            _started = false;
                            return;
                        }
                    }
                }
            }
            if ((snap.Position - gamepos).Length < (snap.Position2 - gamepos).Length)
            {
                _joint = Joint.Left;
                _startPos = snap.Position;
            }
            else
            {
                _joint = Joint.Right;
                _startPos = snap.Position2;
            }

            _snappedline = Snap(_startPos, ssnap);

            if (!(_snappedline is StandardLine))
                _snappedline = null;
            else
            {
                if ((_snappedline.Position - _startPos).Length < (_snappedline.Position2 - _startPos).Length)
                {
                    _snapjoint = Joint.Left;
                }
                else
                {
                    _snapjoint = Joint.Right;
                }
                _snaporiginalpos1 = _snappedline.Position;
                _snaporiginalpos2 = _snappedline.Position2;
            }
            if (keyboard[Key.ControlLeft] || keyboard[Key.ControlRight])
            {
                _joint = Joint.Both;
                _snappedline = null;
            }
            _line = snap;
            UpdateTooltip();
            _originalPos1 = _line.Start;
            _originalPos2 = _line.End;
            if (_line.Prev != null)
            {
                _prev = _line.Prev;
                _line.Prev.Next = null;
                _line.Prev.RemoveExtension(StandardLine.ExtensionDirection.Right);
                _line.RemoveExtension(StandardLine.ExtensionDirection.Left);
                _line.Prev = null;
            }
            else
            {
                _prev = null;
            }
            if (_line.Next != null)
            {
                _next = _line.Next;
                _line.Next.Prev = null;
                _line.Next.RemoveExtension(StandardLine.ExtensionDirection.Left);
                _line.RemoveExtension(StandardLine.ExtensionDirection.Right);
                _line.Next = null;
            }
            else
            {
                _next = null;
            }
            _snext = null;
            _sprev = null;
            if (_snappedline is StandardLine)
            {
                var snl = _snappedline as StandardLine;
                if (snl.Next != null)
                {
                    _snext = snl.Next;
                    snl.Next.Prev = null;
                    snl.Next.RemoveExtension(StandardLine.ExtensionDirection.Left);
                    snl.RemoveExtension(StandardLine.ExtensionDirection.Right);
                    snl.Next = null;
                }
                if (snl.Prev != null)
                {
                    _sprev = snl.Prev;
                    snl.Prev.Next = null;
                    snl.Prev.RemoveExtension(StandardLine.ExtensionDirection.Right);
                    snl.RemoveExtension(StandardLine.ExtensionDirection.Left);
                    snl.Prev = null;
                }
            }
            game.Track.ChangeMade(_originalPos1, _originalPos2);
            game.Invalidate();
        }

        public override void Stop()
        {
            if (_started)
            {
                _started = false;
                LifeLock = false;
                var undoline = _line ?? _nonphysicalline;
                var stl = undoline as StandardLine;

                if (stl != null)
                    game.Track.UndoManager.AddLineAdjustment(undoline, _snappedline, _originalPos1, _originalPos2, stl.Start, stl.End,
                        _snaporiginalpos1, _snaporiginalpos2, (_snappedline?.Position).GetValueOrDefault(), (_snappedline?.Position).GetValueOrDefault());
                else
                    game.Track.UndoManager.AddLineAdjustment(undoline, _snappedline, _originalPos1, _originalPos2, undoline.Position, undoline.Position2,
                        _snaporiginalpos1, _snaporiginalpos2, (_snappedline?.Position).GetValueOrDefault(), (_snappedline?.Position).GetValueOrDefault());
                if (_snappedline != null)
                {
                    game.Track.TryConnectLines(_line as StandardLine, _snappedline as StandardLine);
                }
                _nonphysicalline = null;
                if (_line != null)
                {
                    //remove the extensions so we dont have to deal with that shit
                    if (_prev != null)
                    {
                        game.Track.UndoManager.AddExtensionChange(_prev, _line, false);
                        _prev = null;
                    }
                    if (_next != null)
                    {
                        game.Track.UndoManager.AddExtensionChange(_line, _next, false);
                        _next = null;
                    }
                }
                if (_snappedline is StandardLine)
                {
                    var snl = _snappedline as StandardLine;
                    if (_sprev != null)
                    {
                        game.Track.UndoManager.AddExtensionChange(_sprev, snl, false);
                        game.Track.UndoManager.AddExtensionChange(_sprev, snl, false);
                    }
                    if (_snext != null)
                    {
                        game.Track.UndoManager.AddExtensionChange(snl, _snext, false);
                    }
                }
                _line = null;
                _snappedline = null;
                _snext = null;
                _sprev = null;
                game.Canvas.RemoveTooltip(null);
                game.Invalidate();
            }
        }

        private Vector2d AngleLock(Vector2d pt, Vector2d pos)
        {
            var diff = _originalPos2 - _originalPos1;
            var angle = Math.Atan2(diff.Y, diff.X);
            var delta = pt - pos;
            var ret = new Vector2d(Math.Cos(angle), Math.Sin(angle));
            return (new Vector2d(ret.X, ret.Y) * Vector2d.Dot(delta, ret)) + pos;
        }

        private void Cb_ItemSelected(ControlBase sender, ItemSelectedEventArgs arguments)
        {
            if (arguments.SelectedItem == null)
                return;
            if (arguments.SelectedItem.Name == "Zoom")
            {
                var gb = sender.UserData as GroupBox;
                var gb1 = new GroupBox(sender.Parent);
                gb1.X = gb.X;
                gb1.Y = gb.Y;
                gb1.Height = gb.Height;
                gb1.Width = gb.Width;
                gb1.Text = "Trigger Data";
                gb.Parent.RemoveChild(gb, true);
                NumericUpDown nud = new NumericUpDown(gb);
                nud.SetPosition(80, 15);
                nud.Width = 45;
                Label label = new Label(gb);
                label.SetPosition(0, 15);
                label.Text = "Zoom";
                nud = new NumericUpDown(gb);
                nud.SetPosition(80, 35);
                nud.Width = 45;
                label = new Label(gb);
                label.SetPosition(0, 35);
                label.Text = "Frames";
            }
        }

        private Joint CompliantJoint(Line l, Joint j)
        {
            var stl = l as StandardLine;
            if (stl == null || j == Joint.Both)
                return j;

            return stl.inv ? (j == Joint.Left) ? Joint.Right : Joint.Left : j;
        }

        private bool DoLifelock(bool ignoresetting = false)
        {
            if (LifeLock || ignoresetting)
            {
                return game.Track.DoLifelock(ignoresetting, _line);
            }
            return false;
        }

        private Vector2d LengthLock(Vector2d p1, Vector2d p2)
        {
            var diff = _originalPos2 - _originalPos1;
            var diff2 = p2 - p1;
            if (diff.Length != diff2.Length)
            {
                var angle = Math.Atan2(diff2.Y, diff2.X);
                Drawing.Turtle turtle = new Drawing.Turtle(p1);
                turtle.Move(Tools.Angle.FromRadians(angle).Degrees, diff.Length);
                return turtle.Point;
            }
            return p2;
        }

        private void nud_redlinemultiplier_ValueChanged(ControlBase sender, EventArgs arguments)
        {
            var l = (StandardLine)sender.UserData;
            if (l is RedLine)
            {
                var rl = (RedLine)l;
                rl.Multiplier = (int)Math.Round(((NumericUpDown)sender).Value);
                game.Track.LineChanged(rl);
                game.Track.TrackUpdated();
                game.Invalidate();
            }
        }

        private void Selectionwindow_IsHiddenChanged(ControlBase sender, EventArgs arguments)
        {
            if (sender.IsHidden)
            {
                selectionwindow = null;
            }
        }

        private void SetSnapLinePosition(Vector2d position)
        {
            if (_snappedline != null)
            {
                if (_snapjoint == Joint.Left)
                {
                    _snappedline.Position = position;
                }
                else if (_snapjoint == Joint.Right)
                {
                    _snappedline.Position2 = position;
                }
            }
        }

        private void UpdateTooltip()
        {
            var diff = _nonphysicalline != null ? _nonphysicalline.Position2 - _nonphysicalline.Position : _line.diff;
            var angle = MathHelper.RadiansToDegrees(Math.Atan2(diff.Y, diff.X)) + 90;
            if (angle < 0)
                angle += 360;
            game.Canvas.SetTooltip(null, Math.Round(diff.Length, 2) + " length\n" + Math.Round(angle, 2) + "°");
        }

        #endregion Methods

        #region Classes

        private class NoDecimalNUD : NumericUpDown
        {
            #region Constructors

            public NoDecimalNUD(ControlBase b) : base(b)
            {
            }

            #endregion Constructors

            #region Methods

            protected override bool IsTextAllowed(string str)
            {
                return base.IsTextAllowed(str) && !str.Contains(".");
            }

            #endregion Methods
        }

        #endregion Classes

        #region Enums

        private enum Joint
        {
            Left = 1,
            Right = 2,
            Both = 3
        }

        #endregion Enums
    }
}