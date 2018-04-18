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
            SetSize(400, 400);
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
        private void PopulateSong(ControlBase parent)
        {
            //todo move this to track edit pane
            var currentpanel = CreateHeaderPanel(parent, "Current Song");
            currentpanel.Dock = Dock.Fill;
            ListBox Songs = new ListBox(currentpanel);
            Songs.RowSelected += (o, e) =>
             {
                 var str = (string)e.SelectedItem.UserData;
                 Settings.Local.CurrentSong.Location = str;
             };
            Songs.Dock = Dock.Fill;
            var filedir = Program.UserDirectory + "Songs";
            if (Directory.Exists(filedir))
            {
                var songfiles = Directory.GetFiles(filedir, "*.*");
                var supportedfiles = new List<string>();
                string[] supportedfiletypes = new string[]
                {
                    ".mp3",".wav",".wave",".ogg",".wma",".m4a",".aac"
                };

                foreach (var file in songfiles)
                {
                    var lower = file.ToLower(Program.Culture);
                    foreach (var type in supportedfiletypes)
                    {
                        if (lower.EndsWith(type, StringComparison.OrdinalIgnoreCase))
                        {
                            supportedfiles.Add(file);
                            break;
                        }
                    }
                }

                foreach (var sf in supportedfiles)
                {
                    var name = Path.GetFileName(sf);
                    var nodename = name.ToLower().Contains(".ogg") ? name : "[convert] " + name;
                    var node = Songs.AddRow(nodename);
                    node.UserData = name;
                    if (name == Settings.Local.CurrentSong?.Location)
                        node.IsSelected = true;
                }
            }
            var opts = CreateHeaderPanel(parent, "Sync options");
            var syncenabled = AddCheckbox(opts, "Enable Song", Settings.Local.EnableSong, (o, e) =>
               {
                   Settings.Local.EnableSong = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            Spinner offset = new Spinner(null)
            {
                Min = -1000,
                Value = Settings.Local.CurrentSong.Offset,
            };
            offset.ValueChanged += (o, e) =>
            {
                Settings.Local.CurrentSong.Offset = (float)offset.Value;
            };
            CreateLabeledControl(opts, "Offset", offset);
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
            CreateLabeledControl(opts, "Volume", vol);
            vol.Width = 200;

            this.IsHiddenChanged += (o, e) =>
            {
                if (!this.IsHidden) return;
                if (Settings.Local.EnableSong)
                {
                    var fn = Program.UserDirectory + "Songs" +
                             Path.DirectorySeparatorChar +
                             Settings.Local.CurrentSong.Location;
                    if (File.Exists(fn))
                    {
                        _canvas.Loading = true;
                        Audio.AudioService.LoadFile(ref fn);
                        _canvas.Loading = false;
                    }
                }
            };
        }
        private void PopulateModes(ControlBase parent)
        {
            var playbackmode = CreateHeaderPanel(parent, "Playback Color");
            AddCheckbox(playbackmode, "Color Playback", Settings.Local.ColorPlayback, (o, e) =>
               {
                   Settings.Local.ColorPlayback = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var preview = AddCheckbox(playbackmode, "Preview Mode", Settings.Local.PreviewMode, (o, e) =>
               {
                   Settings.Local.PreviewMode = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var background = CreateHeaderPanel(parent, "Background Color");
            AddCheckbox(background, "Night Mode", Settings.NightMode, (o, e) =>
               {
                   Settings.NightMode = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var whitebg = AddCheckbox(background, "Pure White Background", Settings.WhiteBG, (o, e) =>
               {
                   Settings.WhiteBG = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            var panelgeneral = CreateHeaderPanel(parent, "General");
            var superzoom = AddCheckbox(panelgeneral, "Superzoom", Settings.SuperZoom, (o, e) =>
               {
                   Settings.SuperZoom = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            ComboBox scroll = CreateLabeledCombobox(panelgeneral, "Scroll Sensitivity:");
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
            var camtype = CreateHeaderPanel(parent, "Camera Type");
            var smooth = AddCheckbox(camtype, "Smooth Camera", Settings.SmoothCamera, (o, e) =>
            {
                Settings.SmoothCamera = ((Checkbox)o).IsChecked;
                _editor.InitCamera();
                Settings.Save();
            });
            var round = AddCheckbox(camtype, "Round Legacy Camera", Settings.RoundLegacyCamera, (o, e) =>
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
            Panel advancedtools = CreateHeaderPanel(parent, "Advanced Visualization");

            var contact = AddCheckbox(advancedtools, "Show Contact Points", Settings.Local.DrawContactPoints, (o, e) =>
            {
                Settings.Local.DrawContactPoints = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var momentum = AddCheckbox(advancedtools, "Momentum Vectors", Settings.Local.MomentumVectors, (o, e) =>
            {
                Settings.Local.MomentumVectors = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var hitbox = AddCheckbox(advancedtools, "Draw Line Hitbox", Settings.Local.RenderGravityWells, (o, e) =>
            {
                Settings.Local.RenderGravityWells = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            Panel pblifelock = CreateHeaderPanel(parent, "Lifelock Conditions");
            AddCheckbox(pblifelock, "Next frame constraints", Settings.LifeLockNoOrange, (o, e) =>
            {
                Settings.LifeLockNoOrange = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            AddCheckbox(pblifelock, "No Fakie Death", Settings.LifeLockNoFakie, (o, e) =>
            {
                Settings.LifeLockNoFakie = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            Panel panelSnap = CreateHeaderPanel(parent, "Snapping");
            var linesnap = AddCheckbox(panelSnap, "Enable Line Snapping", !Settings.Local.DisableSnap, (o, e) =>
            {
                Settings.Local.DisableSnap = !((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var forcesnap = AddCheckbox(panelSnap, "Force X/Y snap", Settings.Local.ForceXySnap, (o, e) =>
            {
                Settings.Local.ForceXySnap = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            Panel panelGeneral = CreateHeaderPanel(parent, "Tools");
            var onion = AddCheckbox(panelGeneral, "Onion Skinning", Settings.Local.OnionSkinning, (o, e) =>
            {
                Settings.Local.OnionSkinning = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            var hittest = AddCheckbox(panelGeneral, "Hit Test", Settings.Local.HitTest, (o, e) =>
            {
                Settings.Local.HitTest = ((Checkbox)o).IsChecked;
                Settings.Save();
            });
            onion.Tooltip = "Visualize the rider before/after\nthe current frame.";
            momentum.Tooltip = "Visualize the direction of\nmomentum for each contact point";
            contact.Tooltip = "Visualize the parts of the rider\nthat interact with lines.";
            forcesnap.Tooltip = "Forces all lines drawn to\nsnap to a 45 degree angle";
            hitbox.Tooltip = "Visualizes the hitbox of lines\nUsed for advanced editing";
            hittest.Tooltip = "Lines that have been hit by\nthe rider will glow.";
        }
        private void PopulatePlayback(ControlBase parent)
        {
            var general = CreateHeaderPanel(parent, "Initial Zoom");
            RadioButtonGroup pbzoom = new RadioButtonGroup(general)
            {
                Dock = Dock.Left,
                ShouldDrawBackground = false,
            };
            pbzoom.AddOption("Current Zoom");
            pbzoom.AddOption("Default Zoom");
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
            var framerate = CreateHeaderPanel(parent, "Frame Control");
            var smooth = AddCheckbox(framerate, "Smooth Playback", Settings.SmoothPlayback, (o, e) =>
               {
                   Settings.SmoothPlayback = ((Checkbox)o).IsChecked;
                   Settings.Save();
               });
            ComboBox pbrate = CreateLabeledCombobox(framerate, "Playback Rate:");
            for (var i = 0; i < Constants.MotionArray.Length; i++)
            {
                var f = (Constants.MotionArray[i] / (float)Constants.PhysicsRate);
                pbrate.AddItem(f + "x", f.ToString(CultureInfo.InvariantCulture), f);
            }
            pbrate.SelectByName(Settings.Local.DefaultPlayback.ToString(CultureInfo.InvariantCulture));
            pbrate.ItemSelected += (o, e) =>
            {
                Settings.Local.DefaultPlayback = (float)e.SelectedItem.UserData;
            };
            var cbslowmo = CreateLabeledCombobox(framerate, "Slowmo FPS:");
            var fpsarray = new[] { 1, 2, 5, 10, 20 };
            for (var i = 0; i < fpsarray.Length; i++)
            {
                cbslowmo.AddItem(fpsarray[i].ToString(), fpsarray[i].ToString(CultureInfo.InvariantCulture),
                    fpsarray[i]);
            }
            cbslowmo.SelectByName(Settings.Local.SlowmoSpeed.ToString(CultureInfo.InvariantCulture));
            cbslowmo.ItemSelected += (o, e) =>
            {
                Settings.Local.SlowmoSpeed = (int)e.SelectedItem.UserData;
                Settings.Save();
            };
            smooth.Tooltip = "Interpolates frames from the base\nphysics rate of 40 frames/second\nup to 60 frames/second";
        }
        private void Setup()
        {
            var cat = _prefcontainer.Add("Settings");
            var page = AddPage(cat, "Playback");
            PopulatePlayback(page);
            page = AddPage(cat, "Editor");
            PopulateEditor(page);
            page = AddPage(cat, "Environment");
            PopulateModes(page);
            page = AddPage(cat, "Camera");
            PopulateCamera(page);
            page = AddPage(cat, "Keybindings");
            cat = _prefcontainer.Add("Song Sync");
            page = AddPage(cat, "Song");
            PopulateSong(page);
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
        private Checkbox AddCheckbox(ControlBase parent, string text, bool val, GwenEventHandler<EventArgs> checkedchanged, Dock dock = Dock.Top)
        {
            Checkbox check = new Checkbox(parent)
            {
                Dock = dock,
                Text = text,
                IsChecked = val,
            };
            check.CheckChanged += checkedchanged;
            return check;
        }
        private Panel CreateHeaderPanel(ControlBase parent, string headertext)
        {
            Panel panel = new Panel(parent)
            {
                Dock = Dock.Top,
                Children =
                {
                    new Label(parent)
                    {
                        Dock = Dock.Top,
                        Text = headertext,
                        Alignment = Pos.Left | Pos.CenterV,
                        Font = _canvas.Fonts.DefaultBold,
                        Margin = new Margin(-10, 5, 0, 5)
                    }
                },
                AutoSizeToContents = true,
                Margin = new Margin(0, 0, 0, 10),
                Padding = new Padding(10, 0, 0, 0),
                ShouldDrawBackground = false
            };
            return panel;
        }
        private void CreateLabeledControl(ControlBase parent, string label, ControlBase control)
        {
            control.Dock = Dock.Right;
            ControlBase container = new ControlBase(parent)
            {
                Children =
                {
                    new Label(null)
                    {
                        Text = label,
                        Dock = Dock.Left,
                        Alignment = Pos.Left | Pos.CenterV,
                        Margin = new Margin(0,0,10,0)
                    },
                    control
                },
                AutoSizeToContents = true,
                Dock = Dock.Top,
                Margin = new Margin(0, 1, 0, 1)
            };
        }
        private ComboBox CreateLabeledCombobox(ControlBase parent, string label)
        {
            var combobox = new ComboBox(null)
            {
                Dock = Dock.Right,
                Width = 100
            };
            ControlBase container = new ControlBase(parent)
            {
                Children =
                {
                    new Label(null)
                    {
                        Text = label,
                        Dock = Dock.Left,
                        Alignment = Pos.Left | Pos.CenterV,
                        Margin = new Margin(0,0,10,0)
                    },
                    combobox
                },
                AutoSizeToContents = true,
                Dock = Dock.Top,
                Margin = new Margin(0, 1, 0, 1)
            };
            return combobox;
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
