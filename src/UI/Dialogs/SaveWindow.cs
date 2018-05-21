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
    public class SaveWindow : DialogBase
    {
        private ComboBox _savelist;
        private TextBox _namebox;
        private Label _errorbox;
        private DropDownButton _savebutton;
        private const string CreateNewTrack = "<create new track>";
        public SaveWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Save Track";
            RichLabel l = new RichLabel(this);
            l.Dock = Dock.Top;
            l.AutoSizeToContents = true;
            l.AddText("Files are saved to Documents/LRA/Tracks", Skin.Colors.Text.Foreground);
            _errorbox = new Label(this)
            {
                Dock = Dock.Top,
                TextColor = Color.Red,
                Text = "",
                Margin = new Margin(0, 0, 0, 5)
            };
            ControlBase bottomcontainer = new ControlBase(this)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true
            };
            _savelist = new ComboBox(bottomcontainer)
            {
                Dock = Dock.Top,
                Margin = new Margin(0, 0, 0, 5)
            };
            _namebox = new TextBox(bottomcontainer)
            {
                Dock = Dock.Fill,
            };
            _savebutton = new DropDownButton(bottomcontainer)
            {
                Dock = Dock.Right,
                Text = "Save",
                UserData = ".trk",
                Margin = new Margin(2, 0, 0, 0),
            };
            _savebutton.DropDownClicked += (o, e) =>
              {
                  Menu pop = new Menu(_canvas);
                  pop.AddItem(".trk (recommended)").Clicked += (o2, e2) =>
                  {
                      _savebutton.Text = "Save";
                      _savebutton.UserData = ".trk";
                  };
                  pop.AddItem(".track.json (for .com support)").Clicked += (o2, e2) =>
                  {
                      _savebutton.Text = "Save (.json)";
                      _savebutton.UserData = ".json";
                  };
                  pop.AddItem(".sol (outdated)").Clicked += (o2, e2) =>
                  {
                      _savebutton.Text = "Save (.sol)";
                      _savebutton.UserData = ".sol";
                  };
                  pop.Open(Pos.Center);
              };
            _savebutton.Clicked += (o, e) =>
            {
                Save();
            };
            Padding = new Padding(0, 0, 0, 0);
            AutoSizeToContents = true;
            MakeModal(true);
            Setup();
            MinimumSize = new Size(250, MinimumSize.Height);
        }
        private void Setup()
        {
            _savelist.AddItem(CreateNewTrack);
            _savelist.SelectByText(CreateNewTrack);
            var directories = GetDirectories();
            foreach (var dir in directories)
            {
                _savelist.AddItem(dir);
            }
            _savelist.SelectByText(_editor.Name);
        }
        private void Save()
        {
            var filetype = (string)_savebutton.UserData;
            var filename = _namebox.Text;
            var folder = _savelist.SelectedItem.Text;
            if (folder == CreateNewTrack)
            {
                folder = filename;
            }
            if (
                !TrackIO.CheckValidFilename(
                    folder + filename) ||
                    filename == Utils.Constants.DefaultTrackName ||
                    (folder.Length == 0))
            {
                _errorbox.Text = "Error\n* Save name is invalid";
                return;
            }
            using (var trk = _editor.CreateTrackWriter())
            {
                var l = trk.GetOldestLine();
                if (l == null)
                {
                    _errorbox.Text = "Track must have at least one line";
                    return;
                }
                trk.Name = folder;
                try
                {
                    string filepath;
                    switch (filetype)
                    {
                        case ".trk":
                            {
                                filepath = TrackIO.SaveTrackToFile(trk.Track, filename);
                                Settings.LastSelectedTrack = filepath;
                                Settings.Save();
                            }
                            break;
                        case ".sol":
                            {
                                if (!CheckSol(trk))
                                    return;
                                //purposely do not set this to lastselectedtrack
                                //currently it's deemed non-performant and slow
                                TrackIO.SaveToSOL(trk.Track, filename);
                            }
                            break;
                        case ".json":
                            {
                                filepath = TrackIO.SaveTrackToJsonFile(trk.Track, filename);
                                Settings.LastSelectedTrack = filepath;
                                Settings.Save();
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Unknown save filetype");
                    }
                    _editor.ResetTrackChangeCounter();
                }
                catch (Exception e)
                {
                    _errorbox.Text = "Error\n* An unknown error occured...\n" + e.Message;
                    return;
                }
            }
            Close();
        }
        private bool CheckSol(TrackReader trk)
        {
            Dictionary<string, bool> features;
            features = trk.GetFeatures();
            bool six_one;
            bool redmultiplier;
            bool scenerywidth;
            features.TryGetValue(TrackFeatures.six_one, out six_one);
            features.TryGetValue(TrackFeatures.redmultiplier, out redmultiplier);
            features.TryGetValue(TrackFeatures.scenerywidth, out scenerywidth);
            if (six_one || redmultiplier || scenerywidth)
            {
                var msg = "*Error\nThe following features are incompatible with .sol:\n";
                if (six_one)
                {
                    msg += "\n* The track is based on 6.1";
                }
                if (redmultiplier)
                {
                    msg += "\n* Red line multipliers";
                }
                if (scenerywidth)
                {
                    msg += "\n* Variable width scenery";
                }
                _errorbox.Text = msg;
                return false;
            }
            return true;
        }
        private List<string> GetDirectories()
        {
            var ret = new List<string>();
            var dir = Constants.TracksDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var folders = Directory.GetDirectories(Constants.TracksDirectory);
            foreach (var folder in folders)
            {
                var trackname = Path.GetFileName(folder);
                if (trackname != Utils.Constants.DefaultTrackName)
                {
                    ret.Add(trackname);
                }
            }
            return ret;
        }
    }
}
