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

namespace linerider.Windows
{
	class PreferencesWindow : Window
	{
		private GLWindow game;
		public PreferencesWindow(Gwen.Controls.ControlBase parent, GLWindow pgame) : base(parent, "Preferences")
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
		private void CreateAdvancedTab(TabControl tcontainer)
		{
			var container = tcontainer.AddPage("Advanced").Page;
			PropertyTree tree = new PropertyTree(container);
			var pt = tree.Add("Advanced");
			tree.ExpandAll();
			tree.Dock = Pos.Top;

			var zoom = CreateEditableNumber("Zoom", game.Track.Zoom.ToString(), pt);
			zoom.MinValue = 0.1;
			zoom.MaxValue = 200;
			zoom.ValueChanged+=(o,e)=>
			{
				if (!double.IsNaN(zoom.NumValue))
				{
					game.Zoom((float)zoom.NumValue - game.Track.Zoom);
				}
			};
			tree.Dock = Pos.Fill;
			var mar = tree.Margin;
			mar.Right = 100;
			tree.Margin = mar;
			pt.SplitWidth = 200;
			Button btn = new Button(container);
			btn.Width = 100;
			btn.Height = 20;
			Align.AlignBottom(btn);
			Align.AlignRight(btn);
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
			lcb.IsChecked = game.SettingShowPpf;
			lcb.CheckChanged += (o, e) =>
			{
				game.SettingShowPpf = ((LabeledCheckBox)o).IsChecked;
			};
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Show FPS";
			lcb.IsChecked = game.SettingShowFps;
			lcb.CheckChanged += (o, e) =>
			{
				game.SettingShowFps = ((LabeledCheckBox)o).IsChecked;
			};
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Show Timer";
			lcb.IsChecked = game.SettingShowTimer;
			lcb.CheckChanged += (o, e) =>
			{
				game.SettingShowTimer = ((LabeledCheckBox)o).IsChecked;
			};
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Show Tools";
			lcb.IsChecked = game.SettingRecordingShowTools;
			lcb.CheckChanged += (o, e) =>
			{
				game.SettingRecordingShowTools = ((LabeledCheckBox)o).IsChecked;
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
			gb.Height = 180;
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
			lcb.IsChecked = game.SettingRecordingMode;
			lcb.CheckChanged += (o, e) => { game.SettingRecordingMode = ((LabeledCheckBox)o).IsChecked; };
			lcb.SetToolTipText(@"Disables many editor features
and changes the client so it can be 
recorded with a specific aesthetic");
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Color Playback";
			lcb.IsChecked = game.SettingColorPlayback;
			lcb.CheckChanged += (o, e) => { game.SettingColorPlayback = ((LabeledCheckBox)o).IsChecked; };
			lcb.SetToolTipText(@"During playback the lines will no
longer turn black by default, and 
will stay as they are in editor mode");
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Hit Test";
			lcb.IsChecked = game.HitTest;
			lcb.CheckChanged += (o, e) => { game.HitTest = ((LabeledCheckBox)o).IsChecked; };
			lcb.SetToolTipText(@"During playback, hitting a line will turn it 
the color of the original line.");
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Preview Mode";
			lcb.IsChecked = game.SettingPreviewMode;
			lcb.CheckChanged += (o, e) => { game.SettingPreviewMode = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb.SetToolTipText(@"The opposite of Color Playback. The editor will
shoe the lines as black instead");
			//

			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Zero Start";
			lcb.IsChecked = game.Track.ZeroStart;
			lcb.CheckChanged += (o, e) => { game.Track.ZeroStart = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb.SetToolTipText(@"Starts the track with 0 momentum");

            lcb = new LabeledCheckBox(gb);
            lcb.Text = "New Camera";
            lcb.IsChecked = Settings.SmoothCamera;
            lcb.CheckChanged += (o, e) =>
            {
                Settings.SmoothCamera = ((LabeledCheckBox)o).IsChecked;
                Settings.Save();
            };
            lcb.Dock = Pos.Top;
            lcb.SetToolTipText("Enabled a smooth predictive camera.\r\nExperimental.");

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
			lcb.IsChecked = game.SettingOnionSkinning;
			lcb.CheckChanged += (o, e) =>
			{
				game.SettingOnionSkinning = ((LabeledCheckBox)o).IsChecked;
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
			lcb.IsChecked = game.SettingDrawContactPoints;
			lcb.CheckChanged += (o, e) => { game.SettingDrawContactPoints = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Momentum Vectors";
			lcb.IsChecked = game.SettingMomentumVectors;
			lcb.CheckChanged += (o, e) => { game.SettingMomentumVectors = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Gravity Wells";
			lcb.IsChecked = game.SettingRenderGravityWells;
			lcb.CheckChanged += (o, e) => { game.SettingRenderGravityWells = ((LabeledCheckBox)o).IsChecked; game.InvalidateTrack(); };
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
			for (var i = 0; i < GLWindow.MotionArray.Length; i++)
			{
				var f = (GLWindow.MotionArray[i] / 40f);
				cbplayback.AddItem("Playback: " + f + "x", f.ToString(CultureInfo.InvariantCulture), f);
			}
			cbplayback.SelectByName(game.SettingDefaultPlayback.ToString(CultureInfo.InvariantCulture));
			cbplayback.ItemSelected += (o, e) =>
			{
				game.SettingDefaultPlayback = (float)e.SelectedItem.UserData;
			};
			var cbslowmo = new ComboBox(gb);
			cbslowmo.Dock = Pos.Top;
			var fpsarray = new[] { 1, 2, 5, 10, 20 };
			for (var i = 0; i < fpsarray.Length; i++)
			{
				cbslowmo.AddItem("Slowmo FPS: " + fpsarray[i], fpsarray[i].ToString(CultureInfo.InvariantCulture),
					fpsarray[i]);
			}
			cbslowmo.SelectByName(game.SettingSlowmoSpeed.ToString(CultureInfo.InvariantCulture));
			cbslowmo.ItemSelected += (o, e) =>
			{
				game.SettingSlowmoSpeed = (int)e.SelectedItem.UserData;
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
			lcb.IsChecked = Settings.PinkLifelock;
			lcb.CheckChanged += (o, e) => { Settings.PinkLifelock = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Disable Line Snap";
			lcb.IsChecked = game.SettingDisableSnap;
			lcb.CheckChanged += (o, e) => { game.SettingDisableSnap = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Force XY Snap";
			lcb.IsChecked = game.SettingForceXySnap;
			lcb.CheckChanged += (o, e) => { game.SettingForceXySnap = ((LabeledCheckBox)o).IsChecked; };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Superzoom";
			lcb.IsChecked = Settings.SuperZoom;
			lcb.CheckChanged += (o, e) => { Settings.SuperZoom = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "White BG";
			lcb.SetToolTipText(@"For if you're a bad person");
			lcb.IsChecked = Settings.WhiteBG;
			lcb.CheckChanged += (o, e) =>
			{
				Settings.WhiteBG = ((LabeledCheckBox)o).IsChecked;
				Settings.Save();
				if (!Settings.NightMode)
					GL.ClearColor(Settings.WhiteBG ? GLWindow.ColorWhite : GLWindow.ColorOffwhite);
			};
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Night Mode";
			lcb.IsChecked = Settings.NightMode;
			lcb.CheckChanged += (o, e) =>
			{
				if (((LabeledCheckBox)o).IsChecked)
				{
					GL.ClearColor(new Color4(50, 50, 60, 255));
				}
				else
				{
					GL.ClearColor(Settings.WhiteBG ? GLWindow.ColorWhite : GLWindow.ColorOffwhite);
				}
				Settings.NightMode = ((LabeledCheckBox)o).IsChecked;
				Settings.Save();
				game.Canvas.ButtonsToggleNightmode();
				game.Track.RefreshTrack();
			};
			lcb.Dock = Pos.Top;
			lcb = new LabeledCheckBox(gb);
			lcb.Text = "Live Line Editing";
			lcb.SetToolTipText("For the line adjust tool during playback\r\nEnable this if you have a slow PC");
			lcb.IsChecked = Settings.LiveAdjustment;
			lcb.CheckChanged += (o, e) => { Settings.LiveAdjustment = ((LabeledCheckBox)o).IsChecked; Settings.Save(); };
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
			pt.Add(name, ret,key);
			return ret;
		}
	}
}
