using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;

namespace linerider.UI
{
    public class TrackInfoWindow : DialogBase
    {
        private PropertyTree _tree;
        public TrackInfoWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Track Info";
            Setup();
            MakeModal(true);
            DisableResizing();
            SetSize(350, 400);
        }
        private void ListSongs(ControlBase parent)
        {
            ListBox Songs = new ListBox(parent);
            Songs.AllowMultiSelect = false;
            Songs.RowSelected += (o, e) =>
             {
                 var str = (string)e.SelectedItem.UserData;
                 var song = _editor.Song;
                 song.Location = str;
                 _editor.Song = song;
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
                    var nodename = IsOgg(name) ? name : "[convert] " + name;
                    var node = Songs.AddRow(nodename);
                    node.UserData = name;
                    if (name == _editor.Song.Location)
                        node.IsSelected = true;
                }
            }
        }
        private void PopulateSong(ControlBase parent)
        {
            var currentpanel = GwenHelper.CreateHeaderPanel(parent, "Current Song");
            currentpanel.Dock = Dock.Fill;
            ListSongs(currentpanel);
            var opts = GwenHelper.CreateHeaderPanel(parent, "Sync options");
            var syncenabled = GwenHelper.AddCheckbox(opts, "Sync Song to Track", _editor.Song.Enabled, (o, e) =>
               {
                   var song = _editor.Song;
                   song.Enabled = ((Checkbox)o).IsChecked;
                   _editor.Song = song;
               });
            Spinner offset = new Spinner(null)
            {
                Min = 0,
                Value = _editor.Song.Offset
            };
            offset.ValueChanged += (o, e) =>
            {
                var song = _editor.Song;
                song.Offset = (float)offset.Value;
                _editor.Song = song;
            };
            CreateLabeledControl(opts, "Offset", offset);
            Button help = new Button(parent)
            {
                Dock = Dock.Bottom,
                Text = "Help",
            };
            help.Clicked += (o, e) =>
            {
                MessageBox.Show(_canvas, 
                "LRA can sync songs with tracks you make.\nSongs are imported from Documents/LRA/Songs.\n\nWe load files as .ogg, and LRA will mark files it needs to automatically convert with [convert]", 
                "Help", true);
            };
            this.IsHiddenChanged += (o, e) =>
            {
                if (!this.IsHidden) return;
                var fn = Program.UserDirectory + "Songs" +
                         Path.DirectorySeparatorChar +
                         _editor.Song.Location;
                if (_editor.Song.Enabled && File.Exists(fn))
                {
                    if (!IsOgg(fn) &&
                        !linerider.IO.ffmpeg.FFMPEG.HasExecutable)
                    {
                        var song = _editor.Song;
                        song.Location = null;
                        _editor.Song = song;
                        _canvas.ShowffmpegMissing();
                    }
                    else
                    {
                        _canvas.Loading = true;
                        Audio.AudioService.LoadFile(ref fn);

                        var song = _editor.Song;
                        song.Location = Path.GetFileName(fn);
                        _editor.Song = song;

                        _canvas.Loading = false;
                    }
                }
            };
        }
        private void Setup()
        {
            Dictionary<string, bool> trackfeatures;
            using (var trk = _editor.CreateTrackReader())
            {
                trackfeatures = trk.GetFeatures();
            }
            TabControl tabs = new TabControl(this)
            {
                Dock = Dock.Fill
            };
            var settings = tabs.AddPage("Settings");
            var song = tabs.AddPage("Song");
            _tree = new PropertyTree(settings)
            {
                Dock = Dock.Fill,
            };
            var table = _tree.Add("Settings", 150);
            NumberProperty startzoom = new NumberProperty(null)
            {
                Min = 1,
                NumberValue = _editor.StartZoom,
                Max = Constants.MaxZoom,
            };
            startzoom.ValueChanged += (o, e) =>
            {
                _editor.StartZoom = (float)startzoom.NumberValue;
            };
            table.Add("Start Zoom", startzoom);
            var zerostart = GwenHelper.AddPropertyCheckbox(table, "Zero Start", _editor.ZeroStart);
            zerostart.ValueChanged += (o, e) =>
             {
                 _editor.ZeroStart = zerostart.IsChecked;
             };
            table = _tree.Add("Info", 150);

            // var trackname = table.AddLabel("Track Name", _editor.Name);
            var physics = table.AddLabel("Physics", CheckFeature(trackfeatures, TrackFeatures.six_one) ? "6.1" : "6.2");

            table.AddLabel("Blue Lines", _editor.BlueLines.ToString());
            table.AddLabel("Red Lines", _editor.RedLines.ToString());
            table.AddLabel("Scenery Lines", _editor.GreenLines.ToString());
            table = _tree.Add("Features Used", 150);

            AddFeature(table, trackfeatures, "Red Multiplier", TrackFeatures.redmultiplier);
            AddFeature(table, trackfeatures, "Scenery Width", TrackFeatures.scenerywidth);
            AddFeature(table, trackfeatures, "Line Triggers", TrackFeatures.ignorable_trigger);

            PopulateSong(song);
            _tree.ExpandAll();
            // table.Add
        }
        private bool CheckFeature(Dictionary<string, bool> features, string feature)
        {
            var hasvalue = features.TryGetValue(feature, out bool featureval);
            return hasvalue && featureval;
        }
        private void AddFeature(
            PropertyTable table,
            Dictionary<string, bool> features,
            string name,
            string feature)
        {
            var featureval = CheckFeature(features, feature);
            var check = GwenHelper.AddPropertyCheckbox(
                table,
                name,
                featureval);
            check.Disable();
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
        private bool IsOgg(string file)
        {
            return file.EndsWith(".ogg", true, Program.Culture);
        }
    }
}
