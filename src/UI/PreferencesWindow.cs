//
//  PreferencesWindow.cs
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
using Gwen;
using Gwen.Controls;
using System.Globalization;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using linerider.Utils;

namespace linerider.UI
{
    class PreferencesWindow : WindowControl
    {
        private MainWindow game;
        public PreferencesWindow(Gwen.Controls.ControlBase parent, MainWindow pgame) : base(parent, "Preferences")
        {
            game = pgame;
            Width = 400;
            Height = 420;//blaze it---*shot*
            MakeModal(true);
            DisableResizing();
            ///controls
            TabControl tcontainer = new TabControl(this);
            CreateBasicTab(tcontainer);
            //CreateAdvancedTab(tcontainer); //todo
            CreateRecordingTab(tcontainer);
            CreateAboutTab(tcontainer);
        }
        private void CreateRecordingTab(TabControl tcontainer)
        {
            var container = tcontainer.AddPage("Recording").Page;
            var gb = new GroupBox(container);
            gb.Text = "Recording Mode";
            gb.Width = 180;
            gb.Height = 150;
            var marg = gb.Margin;
            marg.Bottom = 5;
            marg.Right = 5;
            gb.Margin = marg;
            Gwen.Align.AlignTop(gb);
            Gwen.Align.AlignLeft(gb);
            var lcb = new LabeledCheckBox(gb);
            lcb.Text = "Show PPF";
            lcb.IsChecked = Settings.Local.ShowPpf;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.Local.ShowPpf = ((LabeledCheckBox)o).IsChecked;
            };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Show FPS";
            lcb.IsChecked = Settings.Local.ShowFps;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.Local.ShowFps = ((LabeledCheckBox)o).IsChecked;
            };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Show Timer";
            lcb.IsChecked = Settings.Local.ShowTimer;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.Local.ShowTimer = ((LabeledCheckBox)o).IsChecked;
            };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Show Tools";
            lcb.IsChecked = Settings.Local.RecordingShowTools;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.Local.RecordingShowTools = ((LabeledCheckBox)o).IsChecked;
            };
            lcb.Dock = Pos.Top;
        }
        private void CreateBasicTab(TabControl tcontainer)
        {
            var container = tcontainer.AddPage("Basic").Page;
            tcontainer.Dock = Gwen.Pos.Fill;
            //modes
            GroupBox gb = new GroupBox(container);
            var modesgb = gb;
            gb.Text = "Modes";
            gb.Width = 180;
            gb.Height = 200;
            var marg = tcontainer.Margin;
            marg.Bottom = 5;
            tcontainer.Margin = marg;
            marg = gb.Margin;
            marg.Bottom = 15;
            marg.Right = 5;
            gb.Margin = marg;
            RecurseLayout(Skin);
            Gwen.Align.AlignBottom(gb);
            Gwen.Align.AlignRight(gb);
            LabeledCheckBox lcb = new LabeledCheckBox(gb);
            marg = lcb.Margin;
            marg.Top += 5;
            lcb.Margin = marg;
            lcb.Text = "Recording Mode";
            lcb.Dock = Pos.Top;
            lcb.IsChecked = Settings.Local.RecordingMode;
            lcb.CheckChanged += (o, e) => { Settings.Local.RecordingMode = ((LabeledCheckBox)o).IsChecked; };
            lcb.SetToolTipText(@"Disables many editor features
and changes the client so it can be 
recorded with a specific aesthetic");
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Color Playback";
            lcb.IsChecked = Settings.Local.ColorPlayback;
            lcb.CheckChanged += (o, e) => { Settings.Local.ColorPlayback = ((LabeledCheckBox)o).IsChecked; };
            lcb.SetToolTipText(@"During playback the lines will no
longer turn black by default, and 
will stay as they are in editor mode");
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Hit Test";
            lcb.IsChecked = Settings.Local.HitTest;
            lcb.CheckChanged += (o, e) => { Settings.Local.HitTest = ((LabeledCheckBox)o).IsChecked; };
            lcb.SetToolTipText(@"During playback, hitting a line will turn it 
the color of the original line.");
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Preview Mode";
            lcb.IsChecked = Settings.Local.PreviewMode;
            lcb.CheckChanged += (o, e) => { Settings.Local.PreviewMode = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb.SetToolTipText(@"The opposite of Color Playback. The editor will
show the lines as black instead");
            //

            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Zero Start";
            lcb.IsChecked = game.Track.ZeroStart;
            lcb.CheckChanged += (o, e) => { game.Track.ZeroStart = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb.SetToolTipText(@"Starts the track with 0 momentum");

            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Smooth Camera";
            lcb.IsChecked = Settings.SmoothCamera;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.SmoothCamera = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
                game.Track.Stop();
                game.Track.InitCamera();
                var round = FindChildByName("roundlegacycamera", true);
                foreach (var c in round.Children)
                {
                    c.IsDisabled = Settings.SmoothCamera;
                }
            };
            lcb.Dock = Pos.Top;
            lcb.SetToolTipText("Enables a smooth predictive camera.\r\nExperimental and subject to change.");

            lcb = new LabeledCheckBox(gb);
            lcb.Name = "roundlegacycamera";
            lcb.Text = "Round Legacy Camera";
            lcb.IsChecked = Settings.RoundLegacyCamera;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.RoundLegacyCamera = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
                game.Track.Stop();
                game.Track.InitCamera();
            };
            lcb.Dock = Pos.Top;
            lcb.SetToolTipText("If the new camera is disabled\r\nmakes the camera bounds round\r\ninstead of rectangle");

            foreach (var c in lcb.Children)
            {
                c.IsDisabled = Settings.SmoothCamera;
            }
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Smooth Playback";
            lcb.IsChecked = Settings.SmoothPlayback;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.SmoothPlayback = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
            };
            lcb.SetToolTipText("Interpolates frames for a smooth 60+ fps.");
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Onion Skinning";
            lcb.IsChecked = Settings.Local.OnionSkinning;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.Local.OnionSkinning = ((LabeledCheckBox)o).IsChecked;
                game.Invalidate();
            };
            lcb.Dock = Pos.Top;
            //
            gb = new GroupBox(container);
            gb.Text = "Editor View";
            gb.Width = 180;
            gb.Height = 100;
            marg = gb.Margin;
            marg.Bottom = 15;
            marg.Right = 5;
            gb.Margin = marg;
            Gwen.Align.AlignTop(gb);
            Gwen.Align.AlignRight(gb);
            Align.PlaceDownLeft(modesgb, gb);
            lcb = new LabeledCheckBox(gb);
            marg = lcb.Margin;
            marg.Top += 5;
            lcb.Margin = marg;
            lcb.Text = "Contact Lines";
            lcb.IsChecked = Settings.Local.DrawContactPoints;
            lcb.CheckChanged += (o, e) => { Settings.Local.DrawContactPoints = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Momentum Vectors";
            lcb.IsChecked = Settings.Local.MomentumVectors;
            lcb.CheckChanged += (o, e) => { Settings.Local.MomentumVectors = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Gravity Wells";
            lcb.IsChecked = Settings.Local.RenderGravityWells;
            lcb.CheckChanged += (o, e) => { Settings.Local.RenderGravityWells = ((LabeledCheckBox)o).IsChecked; game.Track.Invalidate(); };
            lcb.Dock = Pos.Top;
            //playback
            gb = new GroupBox(container);
            gb.Text = "Playback";
            gb.Width = 180;
            gb.Height = 150;
            marg = gb.Margin;
            marg.Bottom = 5;
            marg.Right = 5;
            gb.Margin = marg;
            Gwen.Align.AlignTop(gb);
            Gwen.Align.AlignLeft(gb);
            RadioButtonGroup rbg = new RadioButtonGroup(gb);
            rbg.Text = "Playback Zoom";
            rbg.AddOption("Current Zoom");
            rbg.AddOption("Default Zoom");
            rbg.AddOption("Specific Zoom");
            rbg.SetSelection(Settings.PlaybackZoomType);
            rbg.SelectionChanged += (o, e) =>
            {
                Settings.PlaybackZoomType = ((RadioButtonGroup)o).SelectedIndex;
                Settings.Save();
            };
            rbg.Dock = Pos.Top;
            rbg.AutoSizeToContents = false;
            rbg.Height = 90;
            var nud = new NumericUpDown(rbg);
            nud.Value = Settings.PlaybackZoomValue;
            nud.Max = 24;
            nud.Min = 1;
            nud.Dock = Pos.Bottom;
            nud.ValueChanged += (o, e) =>
            {
                Settings.PlaybackZoomValue = ((NumericUpDown)o).Value;
                Settings.Save();
            };
            var cbplayback = new ComboBox(gb);
            cbplayback.Dock = Pos.Top;
            for (var i = 0; i < Constants.MotionArray.Length; i++)
            {
                var f = (Constants.MotionArray[i] / (float)Constants.PhysicsRate);
                cbplayback.AddItem("Playback: " + f + "x", f.ToString(CultureInfo.InvariantCulture), f);
            }
            cbplayback.SelectByName(Settings.Local.DefaultPlayback.ToString(CultureInfo.InvariantCulture));
            cbplayback.ItemSelected += (o, e) =>
            {
                Settings.Local.DefaultPlayback = (float)e.SelectedItem.UserData;
            };
            var cbslowmo = new ComboBox(gb);
            cbslowmo.Dock = Pos.Top;
            var fpsarray = new[] { 1, 2, 5, 10, 20 };
            for (var i = 0; i < fpsarray.Length; i++)
            {
                cbslowmo.AddItem("Slowmo FPS: " + fpsarray[i], fpsarray[i].ToString(CultureInfo.InvariantCulture),
                    fpsarray[i]);
            }
            cbslowmo.SelectByName(Settings.Local.SlowmoSpeed.ToString(CultureInfo.InvariantCulture));
            cbslowmo.ItemSelected += (o, e) =>
            {
                Settings.Local.SlowmoSpeed = (int)e.SelectedItem.UserData;
            };
            //editor
            var backup = gb;
            gb = new GroupBox(container);
            gb.Text = "Editor";
            gb.Width = 180;
            gb.Height = 170;
            marg = gb.Margin;
            marg.Bottom = 5;
            marg.Right = 5;
            gb.Margin = marg;
            Gwen.Align.PlaceDownLeft(gb, backup);
            //Gwen.Align.AlignRight(gb);
            lcb = new LabeledCheckBox(gb);
            marg = lcb.Margin;
            marg.Top += 5;
            lcb.Margin = marg;
            lcb.Text = "All Pink Lifelock";
            lcb.SetToolTipText(@"I hope you know where the manual is.");
            lcb.IsChecked = Settings.PinkLifelock;
            lcb.CheckChanged += (o, e) => { Settings.PinkLifelock = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Disable Line Snap";
            lcb.IsChecked = Settings.Local.DisableSnap;
            lcb.CheckChanged += (o, e) => { Settings.Local.DisableSnap = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Force XY Snap";
            lcb.IsChecked = Settings.Local.ForceXySnap;
            lcb.CheckChanged += (o, e) => { Settings.Local.ForceXySnap = ((LabeledCheckBox)o).IsChecked; };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Superzoom";
            lcb.IsChecked = Settings.SuperZoom;
            lcb.CheckChanged += (o, e) => { Settings.SuperZoom = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "White BG";
            lcb.IsChecked = Settings.WhiteBG;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.WhiteBG = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
            };
            lcb.Dock = Pos.Top;
            lcb = new LabeledCheckBox(gb);
            lcb.Text = "Night Mode";
            lcb.IsChecked = Settings.NightMode;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.NightMode = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
                game.Invalidate();
                game.Canvas.ButtonsToggleNightmode();
            };
            lcb.Dock = Pos.Top;

            lcb = new LabeledCheckBox(container);
            lcb.Text = "Check for Updates";
            lcb.IsChecked = Settings.CheckForUpdates;
            lcb.CheckChanged += (o, e) => { Settings.CheckForUpdates = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
            lcb.Dock = Pos.Bottom;
        }

        private void CreateAboutTab(TabControl tcontainer)
        {
            var container = tcontainer.AddPage("About").Page;
            PropertyTree tree = new PropertyTree(container);
            var pt = tree.Add("Keys (uneditable)");
            tree.ExpandAll();
            tree.Dock = Pos.Top;

            pt.Add("Pencil Tool", CreateUneditable(pt), "Q");
            pt.Add("Line Tool", CreateUneditable(pt), "W");
            pt.Add("Eraser Tool", CreateUneditable(pt), "E");
            pt.Add("Line Adjust Tool", CreateUneditable(pt), "R");
            pt.Add("Select Hand Tool", CreateUneditable(pt), "T");
            pt.Add("Hand Tool", CreateUneditable(pt), "Space");

            pt.Add("Move Rider", CreateUneditable(pt), "D").SetToolTipText("Hold D and click the rider to move him");

            pt.Add("Start Track", CreateUneditable(pt), "Y");
            pt.Add("Stop Track", CreateUneditable(pt), "U");
            pt.Add("Play before flag (w/scrubbing)", CreateUneditable(pt), "Shift+I");
            pt.Add("Play ignoring flag", CreateUneditable(pt), "ALT+Y");
            pt.Add("Slowmo Playback", CreateUneditable(pt), "Shift+Y");

            pt.Add("Open Preferences", CreateUneditable(pt), "ESC, CTRL+P");
            pt.Add("Save Track", CreateUneditable(pt), "CTRL+S");
            pt.Add("Load Track", CreateUneditable(pt), "O");

            pt.Add("Blue Color", CreateUneditable(pt), "1").SetToolTipText("Set tool color to blue");
            pt.Add("Red Color", CreateUneditable(pt), "2").SetToolTipText("Set tool color to red");
            pt.Add("Green Color", CreateUneditable(pt), "3").SetToolTipText("Set tool color to green");
            pt.Add("Disable Line Snap", CreateUneditable(pt), "S").SetToolTipText("Disables line snapping while pressed");
            pt.Add("45° Line Snap", CreateUneditable(pt), "X").SetToolTipText("Snap lines to the nearest 45 degree angle");
            pt.Add("Move to First Line", CreateUneditable(pt), "HOME");
            pt.Add("Move to Last Line", CreateUneditable(pt), "END");
            pt.Add("Toggle Tool Settings", CreateUneditable(pt), "TAB").SetToolTipText("Shift between current line settings, \r\nlike the red line multiplier if it's the selected line type.");

            pt.Add("(Line tool) Flip Line", CreateUneditable(pt), "Shift").SetToolTipText("Flips line while using the line tool");
            pt.Add("(Adjust tool) Lock Angle", CreateUneditable(pt), "Shift").SetToolTipText("Locks the angle of the selected line\r\nwith the line adjust tool");
            pt.Add("(Adjust tool) Move Whole Line", CreateUneditable(pt), "CTRL");
            pt.Add("(Adjust tool) Lock Length", CreateUneditable(pt), "TAB");
            pt.Add("(Adjust tool) Lifelock", CreateUneditable(pt), "ALT").SetToolTipText("While pressed, move a line until \r\nit no longer kills bosh in that frame");

            pt.Add("Focus Start Point", CreateUneditable(pt), "F1");
            pt.Add("Focus on Flag", CreateUneditable(pt), "F2");
            pt.Add("Update Track File Cache", CreateUneditable(pt), "F5");

            pt.Add("Calculate Flag", CreateUneditable(pt), "Right Click Flag Icon").SetToolTipText("Right click the flag icon to calculate the validity of a flag");
            pt.Add("(playback) Pause/Resume", CreateUneditable(pt), "Space");
            pt.Add("(playback) Flag", CreateUneditable(pt), "I");
            pt.Add("(playback) Zoom In", CreateUneditable(pt), "Z");
            pt.Add("(playback) Zoom Out", CreateUneditable(pt), "X");
            pt.Add("(playback) Slow Playback", CreateUneditable(pt), "-");
            pt.Add("(playback) Speed Playback", CreateUneditable(pt), "+");
            pt.Add("(playback) Frame Left", CreateUneditable(pt), "Left");
            pt.Add("(playback) Frame Right", CreateUneditable(pt), "Right");
            pt.Add("(playback) (hold) Rewind", CreateUneditable(pt), "Shift+Left");
            pt.Add("(playback) (hold) Playback", CreateUneditable(pt), "Shift+Right");
            pt.Add("(playback) Iterations Left", CreateUneditable(pt), "Alt+Left");
            pt.Add("(playback) Iterations Right", CreateUneditable(pt), "Alt+Right");

            tree.Dock = Pos.Fill;
            pt.SplitWidth = 200;
        }
        private Gwen.Controls.Property.PropertyBase CreateUneditable(Properties pt)
        {
            var ret = new Gwen.Controls.Property.LabelProperty(pt);
            return ret;
        }
        private Gwen.Controls.Property.KeyProperty CreateEditableKey(string name, string key, Properties pt)
        {
            var ret = new Gwen.Controls.Property.KeyProperty(pt);
            ret.SetValue(key, null);
            ret.IsDisabled = true;
            pt.Add(name, ret);
            return ret;
        }
        private Gwen.Controls.Property.NumberProperty CreateEditableNumber(string name, string key, Properties pt)
        {
            var ret = new Gwen.Controls.Property.NumberProperty(pt);
            pt.Add(name, ret, key);
            return ret;
        }
    }
}
