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
using linerider.Lines;
using OpenTK.Graphics;
using linerider.Utils;
using OpenTK;

namespace linerider.UI
{
    class SelectedLineWindow : WindowControl
    {
        private MainWindow game;
        public SelectedLineWindow(Gwen.Controls.ControlBase parent, MainWindow glgame, GameLine line) : base(parent, "Line Properties")
        {
            game = glgame;
            this.MakeModal(true);
            this.Height = 170;
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
                        game.Track.UndoManager.BeginAction();
                        var copy = (StandardLine)stl.Clone();
                        var caller = (LabeledCheckBox)o;
                        copy.inv = caller.IsChecked;
                        copy.CalculateConstants();
                        trk.ReplaceLine(stl, copy);
                        game.Track.UndoManager.EndAction();
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
                        game.Track.UndoManager.BeginAction();
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
                        game.Track.UndoManager.EndAction();
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
                var redstl = (RedLine)line;
                Height += 30;
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
                Height += 25;


                nud = new NoDecimalNUD(container1);
                marg = nud.Margin;
                marg.Top = 5;
                marg.Left = marg.Right = 1;
                marg.Left = 70;
                marg.Bottom = 10;
                nud.Margin = marg;
                nud.Dock = Gwen.Pos.Top;
                SimulationCell redlines = new SimulationCell();
                using (var trk = game.Track.CreateTrackReader())
                {
                    var lines = trk.GetLinesInRect(new Utils.DoubleRect(line.Position, new Vector2d(1, 1)), false);
                    foreach (var red in lines)
                    {
                        if (
                            red is RedLine stl &&
                            red.Position == line.Position &&
                            red.Position2 == line.Position2)
                        {
                            redlines.AddLine(stl);
                        }
                    }
                }
                nud.Min = 1;
                nud.Max = 9999;
                nud.Value = redlines.Count;
                nud.ValueChanged += (o, e) =>
                {
                    var diff = nud.Value - redlines.Count;
                    if (diff != 0)
                    {
                        using (var trk = game.Track.CreateTrackWriter())
                        {
                            game.Track.UndoManager.BeginAction();
                            if (diff < 0)
                            {
                                for (int i = 0; i > diff; i--)
                                {
                                    trk.RemoveLine(redlines.First());
                                    redlines.RemoveLine(redlines.First().ID);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < diff; i++)
                                {
                                    var red = new RedLine(line.Position, line.Position2, redstl.inv) { Multiplier = ((RedLine)line).Multiplier };
                                    red.CalculateConstants();
                                    trk.AddLine(red);
                                    redlines.AddLine(red);
                                }
                            }
                            game.Track.UndoManager.EndAction();
                        }
                        game.Track.NotifyTrackChanged();
                    }
                };
                nud.UserData = line;
                l = new Label(container1);
                l.Y = 137 + 35;
                l.Text = "Multilines";
            }
            game.Cursor = OpenTK.MouseCursor.Default;
        }
        private void nud_redlinemultiplier_ValueChanged(ControlBase sender, EventArgs arguments)
        {
            var l = (StandardLine)sender.UserData;
            if (l is RedLine)
            {
                using (var trk = game.Track.CreateTrackWriter())
                {
                    game.Track.UndoManager.BeginAction();
                    var copy = (RedLine)l.Clone();
                    copy.Multiplier = (int)Math.Round(((NumericUpDown)sender).Value);
                    copy.CalculateConstants();
                    trk.ReplaceLine(l, copy);
                    game.Track.UndoManager.EndAction();
                    game.Track.NotifyTrackChanged();
                }
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