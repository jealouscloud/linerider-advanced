using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Audio;

namespace linerider.Windows
{
    class SongWindow : WindowControl
    {
        private GLWindow game;
        public SongWindow(Gwen.Controls.ControlBase parent, GLWindow glgame) : base(parent, "Song Sync")
        {
            game = glgame;
            this.MakeModal(true);

            this.MinimumSize = new System.Drawing.Point(220, 240);

            this.Width = 220;
            this.Height = 240;
            //wc.DisableResizing();
            var enablesongcb = new LabeledCheckBox(this);
            enablesongcb.CheckChanged += (o, e) => { game.EnableSong = enablesongcb.IsChecked; };
            enablesongcb.IsChecked = game.EnableSong;
            enablesongcb.Text = "Enable Song";
            enablesongcb.Dock = Pos.Top;
            var gb = new GroupBox(this);
            gb.Text = "Song Selection";
            gb.Dock = Pos.Fill;
            var Songs = new TreeControl(gb);
            Songs.Height = 100;
            Songs.Margin = new Margin() { Bottom = 10 };
            Songs.Dock = Pos.Fill;
            Songs.Name = "songtv";

            var filedir = Program.CurrentDirectory + "Songs";
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
                    var node = Songs.AddNode(nodename);
                    node.UserData = name;
                    if (name == game.CurrentSong?.Location)
                        node.IsSelected = true;
                }
            }
            Songs.SelectionChanged += (snd, ev) =>
            {
                var tc = (TreeControl)this.FindChildByName("songtv", true);
                var list = (List<TreeNode>)tc.SelectedChildren;
                if (list.Count == 1)
                {
                    game.CurrentSong.Location = (string)list[0].UserData;
                }
            };
            this.IsHiddenChanged += (o, e) =>
            {
                if (!this.IsHidden) return;
                if (game.EnableSong)
                {
                    var fn = Program.CurrentDirectory + "Songs" +
                             Path.DirectorySeparatorChar +
                             game.CurrentSong.Location;
                    if (File.Exists(fn))
                    {
                        game.Loading = true;
                        AudioPlayback.LoadFile(ref fn);
                        game.Loading = false;
                    }
                }
                try
                {
                    var svolume =
                        (HorizontalSlider)this.FindChildByName("volume", true);
                    Settings.Volume = svolume.Value;
                    Settings.Save();
                }
                catch
                {
                    // ignored
                }
            };
            var container = new ControlBase(gb);
            container.Dock = Pos.Bottom;
            container.Height = 20;
            var offset = new NumericUpDown(container);
            offset.ValueChanged += (snd, ev) => { game.CurrentSong.Offset = offset.Value; };
            offset.Min = 0;
            offset.Max = 10000;
            offset.Value = game.CurrentSong.Offset;
            offset.Dock = Pos.Right;
            var label = new Label(container);
            label.Dock = Pos.Left;
            label.Text = "Offset (secs)";
            gb.Height = 150;
            var container2 = new ControlBase(this);
            container2.Dock = Pos.Bottom;
            container2.Height = 40;
            label = new Label(container2);
            label.Margin = new Margin() { Top = 13 };
            label.Dock = Pos.Left;
            label.Text = "Volume:";
            var volume = new HorizontalSlider(container2);
            volume.Min = 0;
            volume.Max = 100;
            if (Settings.Volume > 100)
                Settings.Volume = 100;
            if (Settings.Volume < 0)
                Settings.Volume = 0;
            volume.Value = Settings.Volume;
            volume.Name = "volume";
            volume.SnapToNotches = false;
            volume.KeyboardInputEnabled = false;
            volume.Width = 150;
            volume.Dock = Pos.Right;
            game.Cursor = OpenTK.MouseCursor.Default;
        }
    }
}
