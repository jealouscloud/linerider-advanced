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
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Audio;
using linerider.Game;
using OpenTK.Graphics;
using linerider.Utils;
using OpenTK;

namespace linerider.UI
{
    class SelectedLineWindow : WindowControl
    {
        private MainWindow game;
        private bool _closing = false;
        private bool _linechanged = false;
        // todo owner should be saved by id because we might make modifications
        // this is acceptable currently, breaking only inv change + multiline
        // but we want more features soon
        private GameLine _ownerline;
        private const string DefaultTitle = "Line Properties";
        public SelectedLineWindow(Gwen.Controls.ControlBase parent, MainWindow glgame, GameLine line) : base(parent, DefaultTitle)
        {
            game = glgame;
            _ownerline = line;
            this.MakeModal(true);
            this.Height = 190;
            this.Width = 150;
            ControlBase container1 = new ControlBase(this);
            container1.Dock = Gwen.Pos.Fill;
            if (line.Type != LineType.Scenery)
            {
                var stl = (StandardLine)line;
                LabeledCheckBox btn = new LabeledCheckBox(container1);
                btn.Dock = Gwen.Pos.Top;
                btn.Text = "Inverse";
                btn.IsChecked = stl.inv;
                btn.CheckChanged += (o, e) =>
                {
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        LineChanging();
                        var copy = (StandardLine)stl.Clone();
                        var caller = (LabeledCheckBox)o;
                        copy.inv = caller.IsChecked;
                        copy.CalculateConstants();
                        trk.ReplaceLine(stl, copy);
                        game.Track.NotifyTrackChanged();
                        game.Invalidate();
                    }
                };
                LineTrigger tr = (LineTrigger)stl.Trigger ?? new LineTrigger();

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
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        LineChanging();
                        trk.DisableExtensionUpdating();
                        var copy = (StandardLine)stl.Clone();
                        if (enabled.Value == "1")
                        {
                            tr.Zoomtrigger = true;
                            tr.ZoomTarget = float.Parse(((PropertyRow)container1.FindChildByName("Zoom", true)).Value);
                            tr.ZoomFrames = int.Parse(((PropertyRow)container1.FindChildByName("ZoomFrames", true)).Value);
                            copy.Trigger = tr;
                        }
                        else
                        {
                            tr.Zoomtrigger = false;
                            if (!tr.Enabled)
                            {
                                copy.Trigger = null;
                            }
                        }
                        trk.ReplaceLine(stl, copy);
                        game.Track.NotifyTrackChanged();
                    }
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

            if (line.Type == LineType.Red)
            {
                ControlBase optioncontainer = new ControlBase(container1);
                optioncontainer.Dock = Pos.Top;
                optioncontainer.Height = 30;
                var redstl = (RedLine)line;
                Height += 30;
                NoDecimalNUD nud = new NoDecimalNUD(optioncontainer);
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
                Label l = new Label(optioncontainer);
                l.Y = 5;
                l.Text = "Multiplier";
                Height += 25;

                optioncontainer = new ControlBase(container1);
                optioncontainer.Dock = Pos.Top;
                optioncontainer.Height = 30;
                nud = new NoDecimalNUD(optioncontainer);
                marg = nud.Margin;
                marg.Top = 5;
                marg.Left = marg.Right = 1;
                marg.Left = 70;
                marg.Bottom = 10;
                nud.Margin = marg;
                nud.Dock = Gwen.Pos.Top;
                nud.Min = 1;
                nud.Max = 9999;
                nud.Value = GetMultiLines(true).Count;
                nud.ValueChanged += (o, e) =>
                {
                    var val = (int)nud.Value;
                    if (val > 0)
                    {
                        Multiline(val);
                    }
                };
                nud.UserData = line;
                l = new Label(optioncontainer);
                l.Y = 5;
                l.Text = "Multilines";
            }
            Button saveandquit = new Button(container1);
            saveandquit.Name = "saveandquit";
            saveandquit.Disable();
            saveandquit.Dock = Pos.Bottom;
            saveandquit.Text = "Save changes";
            saveandquit.Clicked += (o, e) =>
            {
                if (!saveandquit.IsDisabled)
                {
                    if (!_closing)
                    {
                        _closing = true;
                        if (_linechanged)
                        {
                            game.Track.UndoManager.EndAction();
                        }
                        if (!this.IsHidden)
                            Close();
                    }
                }
            };
            RecurseLayout(Skin);
            this.MinimumSize = new System.Drawing.Point(Width, Height);
            game.Cursor = OpenTK.MouseCursor.Default;
        }
        protected override void CloseButtonPressed(ControlBase control, EventArgs args)
        {
            if (!_closing)
            {
                _closing = true;
                if (_linechanged)
                {
                    game.Track.UndoManager.CancelAction();
                }
            }
            base.CloseButtonPressed(control, args);
        }
        private void LineChanging()
        {
            if (!_linechanged)
            {
                _linechanged = true;
                var btn = (Button)FindChildByName("saveandquit", true);
                btn.Enable();
                btn.UpdateColors();
                Title = DefaultTitle + " *";
                game.Track.UndoManager.BeginAction();
            }
        }
        private SimulationCell GetMultiLines(bool includeowner)
        {
            SimulationCell redlines = new SimulationCell();
            using (var trk = game.Track.CreateTrackReader())
            {
                var owner = (StandardLine)_ownerline;
                var lines = trk.GetLinesInRect(new Utils.DoubleRect(owner.Position, new Vector2d(1, 1)), false);
                foreach (var red in lines)
                {
                    if (
                        red is RedLine stl &&
                        red.Position == owner.Position &&
                        red.Position2 == owner.Position2 &&
                        (includeowner || red.ID != owner.ID))
                    {
                        redlines.AddLine(stl);
                    }
                }
            }
            return redlines;
        }
        private void Multiline(int count)
        {
            SimulationCell redlines = GetMultiLines(false);
            using (var trk = game.Track.CreateTrackWriter())
            {
                var owner = (StandardLine)_ownerline;
                LineChanging();
                // owner line doesn't count, but our min bounds is 1
                var diff = (count - 1) - redlines.Count;
                if (diff < 0)
                {
                    for (int i = 0; i > diff; i--)
                    {
                        trk.RemoveLine(redlines.First());
                        redlines.RemoveLine(redlines.First().ID);
                    }
                }
                else if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        var red = new RedLine(owner.Position, owner.Position2, owner.inv) { Multiplier = ((RedLine)owner).Multiplier };
                        red.CalculateConstants();
                        trk.AddLine(red);
                        redlines.AddLine(red);
                    }
                }
            }
            game.Track.NotifyTrackChanged();
        }
        private void nud_redlinemultiplier_ValueChanged(ControlBase sender, EventArgs arguments)
        {
            var lines = GetMultiLines(true);
            LineChanging();
            using (var trk = game.Track.CreateTrackWriter())
            {
                var multiplier = (int)Math.Round(((NumericUpDown)sender).Value);
                foreach (var line in lines)
                {
                    var copy = (RedLine)line.Clone();
                    copy.Multiplier = multiplier;
                    copy.CalculateConstants();
                    trk.ReplaceLine(line, copy);
                }
                game.Track.NotifyTrackChanged();
            }
        }
        private class NoDecimalNUD : NumericUpDown
        {

            public NoDecimalNUD(ControlBase b) : base(b)
            {
            }

            protected override bool IsTextAllowed(string str)
            {
                return base.IsTextAllowed(str) && !str.Contains(".");
            }
        }
    }
}