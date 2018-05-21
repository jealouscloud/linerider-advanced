using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;

namespace linerider.UI
{
    public class PreferencesWindow : DialogBase
    {
        private CollapsibleList _prefcontainer;
        private ControlBase _focus;
        private int _tabscount = 0;
        public PreferencesWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Preferences";
            SetSize(450, 425);
            MinimumSize = Size;
            ControlBase bottom = new ControlBase(this)
            {
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
            };
            Button defaults = new Button(bottom)
            {
                Dock = Dock.Right,
                Margin = new Margin(0, 2, 0, 0),
                Text = "Restore Defaults"
            };
            defaults.Clicked += (o, e) => RestoreDefaults();
            _prefcontainer = new CollapsibleList(this)
            {
                Dock = Dock.Left,
                AutoSizeToContents = false,
                Width = 100,
                Margin = new Margin(0, 0, 5, 0)
            };
            MakeModal(true);
            Setup();
        }
        private void RestoreDefaults()
        {
            var mbox = MessageBox.Show(
                _canvas,
                "Are you sure? This cannot be undone.", "Restore Defaults",
                MessageBox.ButtonType.OkCancel,
                true);
            mbox.RenameButtons("Restore");
            mbox.Dismissed += (o, e) =>
            {
                if (e == DialogResult.OK)
                {
                    Settings.RestoreDefaultSettings();
                    Settings.Save();
                    _editor.InitCamera();
                    Close();// this is lazy, but i don't want to update the ui
                }
            };
        }
        private void PopulateAudio(ControlBase parent)
        {
            var opts = GwenHelper.CreateHeaderPanel(parent, "Sync options");
            var syncenabled = GwenHelper.AddCheckbox(opts, "Mute", Settings.MuteAudio, (o, e) =>
               {
                   Settings.MuteAudio = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            HorizontalSlider vol = new HorizontalSlider(null)
            {
                Min = 0,
                Max = 100,
                Value = Settings.Volume,
                Width = 80,
            };
            vol.ValueChanged += (o, e) =>
              {
                  Settings.Volume = (float)vol.Value;
                  Settings.Save();
              };
            GwenHelper.CreateLabeledControl(opts, "Volume", vol);
            vol.Width = 200;
        }
        private void PopulateKeybinds(ControlBase parent)
        {
            var hk = new HotkeyWidget(parent);
        }
        private void PopulateModes(ControlBase parent)
        {
            var background = GwenHelper.CreateHeaderPanel(parent, "Background Color");
            GwenHelper.AddCheckbox(background, "Night Mode", Settings.NightMode, (o, e) =>
               {
                   Settings.NightMode = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var whitebg = GwenHelper.AddCheckbox(background, "Pure White Background", Settings.WhiteBG, (o, e) =>
               {
                   Settings.WhiteBG = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var panelgeneral = GwenHelper.CreateHeaderPanel(parent, "General");
            var superzoom = GwenHelper.AddCheckbox(panelgeneral, "Superzoom", Settings.SuperZoom, (o, e) =>
               {
                   Settings.SuperZoom = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            ComboBox scroll = GwenHelper.CreateLabeledCombobox(panelgeneral, "Scroll Sensitivity:");
            scroll.Margin = new Margin(0, 0, 0, 0);
            scroll.Dock = Dock.Bottom;
            scroll.AddItem("0.25x").Name = "0.25";
            scroll.AddItem("0.5x").Name = "0.5";
            scroll.AddItem("0.75x").Name = "0.75";
            scroll.AddItem("1x").Name = "1";
            scroll.AddItem("2x").Name = "2";
            scroll.AddItem("3x").Name = "3";
            scroll.SelectByName("1");//default if user setting fails.
            scroll.SelectByName(Settings.ScrollSensitivity.ToString(Program.Culture));
            scroll.ItemSelected += (o, e) =>
            {
                if (e.SelectedItem != null)
                {
                    Settings.ScrollSensitivity = float.Parse(e.SelectedItem.Name, Program.Culture);
                    Settings.Save();
                }
            };
            superzoom.Tooltip = "Allows the user to zoom in\nnearly 10x more than usual.";
        }
        private void PopulateCamera(ControlBase parent)
        {
            var camtype = GwenHelper.CreateHeaderPanel(parent, "Camera Type");
            var smooth = GwenHelper.AddCheckbox(camtype, "Smooth Camera", Settings.SmoothCamera, (o, e) =>
            {
                Settings.SmoothCamera = ((Checkbox)o).IsChecked;
                _editor.InitCamera();
                Settings.Save();
            });
            var round = GwenHelper.AddCheckbox(camtype, "Round Legacy Camera", Settings.RoundLegacyCamera, (o, e) =>
            {
                Settings.RoundLegacyCamera = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            if (smooth.IsChecked)
            {
                round.IsDisabled = true;
            }
            smooth.CheckChanged += (o, e) =>
            {
                round.IsDisabled = smooth.IsChecked;
            };
        }
        private void PopulateEditor(ControlBase parent)
        {
            Panel advancedtools = GwenHelper.CreateHeaderPanel(parent, "Advanced Visualization");

            var contact = GwenHelper.AddCheckbox(advancedtools, "Contact Points", Settings.Editor.DrawContactPoints, (o, e) =>
            {
                Settings.Editor.DrawContactPoints = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var momentum = GwenHelper.AddCheckbox(advancedtools, "Momentum Vectors", Settings.Editor.MomentumVectors, (o, e) =>
            {
                Settings.Editor.MomentumVectors = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var hitbox = GwenHelper.AddCheckbox(advancedtools, "Line Hitbox", Settings.Editor.RenderGravityWells, (o, e) =>
            {
                Settings.Editor.RenderGravityWells = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var hittest = GwenHelper.AddCheckbox(advancedtools, "Hit Test", Settings.Editor.HitTest, (o, e) =>
             {
                 Settings.Editor.HitTest = ((Checkbox)o).IsChecked;
                 Settings.Save();
             });
            var onion = GwenHelper.AddCheckbox(advancedtools, "Onion Skinning", Settings.OnionSkinning, (o, e) =>
            {
                Settings.OnionSkinning = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            Panel pblifelock = GwenHelper.CreateHeaderPanel(parent, "Lifelock Conditions");
            GwenHelper.AddCheckbox(pblifelock, "Next frame constraints", Settings.Editor.LifeLockNoOrange, (o, e) =>
            {
                Settings.Editor.LifeLockNoOrange = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            GwenHelper.AddCheckbox(pblifelock, "No Fakie Death", Settings.Editor.LifeLockNoFakie, (o, e) =>
            {
                Settings.Editor.LifeLockNoFakie = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            onion.Tooltip = "Visualize the rider before/after\nthe current frame.";
            momentum.Tooltip = "Visualize the direction of\nmomentum for each contact point";
            contact.Tooltip = "Visualize the parts of the rider\nthat interact with lines.";
            hitbox.Tooltip = "Visualizes the hitbox of lines\nUsed for advanced editing";
            hittest.Tooltip = "Lines that have been hit by\nthe rider will glow.";
        }
        private void PopulatePlayback(ControlBase parent)
        {
            var playbackzoom = GwenHelper.CreateHeaderPanel(parent, "Playback Zoom");
            RadioButtonGroup pbzoom = new RadioButtonGroup(playbackzoom)
            {
                Dock = Dock.Left,
                ShouldDrawBackground = false,
            };
            pbzoom.AddOption("Default Zoom");
            pbzoom.AddOption("Current Zoom");
            pbzoom.AddOption("Specific Zoom");
            Spinner playbackspinner = new Spinner(pbzoom)
            {
                Dock = Dock.Bottom,
                Max = 24,
                Min = 1,
            };
            pbzoom.SelectionChanged += (o, e) =>
            {
                Settings.PlaybackZoomType = ((RadioButtonGroup)o).SelectedIndex;
                Settings.Save();
                playbackspinner.IsHidden = (((RadioButtonGroup)o).SelectedLabel != "Specific Zoom");
            };
            playbackspinner.ValueChanged += (o, e) =>
            {
                Settings.PlaybackZoomValue = (float)((Spinner)o).Value;
                Settings.Save();
            };
            pbzoom.SetSelection(Settings.PlaybackZoomType);
            playbackspinner.Value = Settings.PlaybackZoomValue;

            var playbackmode = GwenHelper.CreateHeaderPanel(parent, "Playback Color");
            GwenHelper.AddCheckbox(playbackmode, "Color Playback", Settings.ColorPlayback, (o, e) =>
               {
                   Settings.ColorPlayback = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var preview = GwenHelper.AddCheckbox(playbackmode, "Preview Mode", Settings.PreviewMode, (o, e) =>
               {
                   Settings.PreviewMode = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var framerate = GwenHelper.CreateHeaderPanel(parent, "Frame Control");
            var smooth = GwenHelper.AddCheckbox(framerate, "Smooth Playback", Settings.SmoothPlayback, (o, e) =>
               {
                   Settings.SmoothPlayback = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            ComboBox pbrate = GwenHelper.CreateLabeledCombobox(framerate, "Playback Rate:");
            for (var i = 0; i < Constants.MotionArray.Length; i++)
            {
                var f = (Constants.MotionArray[i] / (float)Constants.PhysicsRate);
                pbrate.AddItem(f + "x", f.ToString(CultureInfo.InvariantCulture), f);
            }
            pbrate.SelectByName(Settings.DefaultPlayback.ToString(CultureInfo.InvariantCulture));
            pbrate.ItemSelected += (o, e) =>
            {
                Settings.DefaultPlayback = (float)e.SelectedItem.UserData;
                Settings.Save();
            };
            var cbslowmo = GwenHelper.CreateLabeledCombobox(framerate, "Slowmo FPS:");
            var fpsarray = new[] { 1, 2, 5, 10, 20 };
            for (var i = 0; i < fpsarray.Length; i++)
            {
                cbslowmo.AddItem(fpsarray[i].ToString(), fpsarray[i].ToString(CultureInfo.InvariantCulture),
                    fpsarray[i]);
            }
            cbslowmo.SelectByName(Settings.SlowmoSpeed.ToString(CultureInfo.InvariantCulture));
            cbslowmo.ItemSelected += (o, e) =>
            {
                Settings.SlowmoSpeed = (int)e.SelectedItem.UserData;
                Settings.Save();
            };
            smooth.Tooltip = "Interpolates frames from the base\nphysics rate of 40 frames/second\nup to 60 frames/second";
        }
        private void PopulateTools(ControlBase parent)
        {
            var select = GwenHelper.CreateHeaderPanel(parent, "Select Tool -- Line Info");
            var length = GwenHelper.AddCheckbox(select, "Show Length", Settings.Editor.ShowLineLength, (o, e) =>
               {
                   Settings.Editor.ShowLineLength = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var angle = GwenHelper.AddCheckbox(select, "Show Angle", Settings.Editor.ShowLineAngle, (o, e) =>
               {
                   Settings.Editor.ShowLineAngle = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var showid = GwenHelper.AddCheckbox(select, "Show ID", Settings.Editor.ShowLineID, (o, e) =>
               {
                   Settings.Editor.ShowLineID = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            Panel panelSnap = GwenHelper.CreateHeaderPanel(parent, "Snapping");
            var linesnap = GwenHelper.AddCheckbox(panelSnap, "Snap New Lines", Settings.Editor.SnapNewLines, (o, e) =>
            {
                Settings.Editor.SnapNewLines = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var movelinesnap = GwenHelper.AddCheckbox(panelSnap, "Snap Line Movement", Settings.Editor.SnapMoveLine, (o, e) =>
            {
                Settings.Editor.SnapMoveLine = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var forcesnap = GwenHelper.AddCheckbox(panelSnap, "Force X/Y snap", Settings.Editor.ForceXySnap, (o, e) =>
            {
                Settings.Editor.ForceXySnap = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            forcesnap.Tooltip = "Forces all lines drawn to\nsnap to a 45 degree angle";
            movelinesnap.Tooltip = "Snap to lines when using the\nselect tool to move a single line";
        }
        private void PopulateOther(ControlBase parent)
        {
            var updates = GwenHelper.CreateHeaderPanel(parent, "Updates");

            var showid = GwenHelper.AddCheckbox(updates, "Check For Updates", Settings.CheckForUpdates, (o, e) =>
               {
                   Settings.CheckForUpdates = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
        }
        private void Setup()
        {
            var cat = _prefcontainer.Add("Settings");
            var page = AddPage(cat, "Editor");
            PopulateEditor(page);
            page = AddPage(cat, "Playback");
            PopulatePlayback(page);
            page = AddPage(cat, "Tools");
            PopulateTools(page);
            page = AddPage(cat, "Environment");
            PopulateModes(page);
            page = AddPage(cat, "Camera");
            PopulateCamera(page);
            cat = _prefcontainer.Add("Application");
            page = AddPage(cat, "Audio");
            PopulateAudio(page);
            page = AddPage(cat, "Keybindings");
            PopulateKeybinds(page);
            page = AddPage(cat, "Other");
            PopulateOther(page);
            if (Settings.SettingsPane >= _tabscount && _focus == null)
            {
                Settings.SettingsPane = 0;
                _focus = page;
                page.Show();
            }

        }
        private void CategorySelected(object sender, ItemSelectedEventArgs e)
        {
            if (_focus != e.SelectedItem.UserData)
            {
                if (_focus != null)
                {
                    _focus.Hide();
                }
                _focus = (ControlBase)e.SelectedItem.UserData;
                _focus.Show();
                Settings.SettingsPane = (int)_focus.UserData;
                Settings.Save();
            }
        }
        private ControlBase AddPage(CollapsibleCategory category, string name)
        {
            var btn = category.Add(name);
            Panel panel = new Panel(this);
            panel.Dock = Dock.Fill;
            panel.Padding = Padding.Five;
            panel.Hide();
            panel.UserData = _tabscount;
            btn.UserData = panel;
            category.Selected += CategorySelected;
            if (_tabscount == Settings.SettingsPane)
                btn.Press();
            _tabscount += 1;
            return panel;
        }
    }
}
