//
//  GameCanvas.cs
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
using Gwen;
using Gwen.Controls;
using Gwen.Skin;
using linerider.Tools;
using System.IO;
using System.Threading;
using linerider.Audio;
using OpenTK;
using Color = System.Drawing.Color;

namespace linerider
{
    public class GameCanvas : Canvas
    {
        public Sprite SpriteLoading;
        public Gwen.Renderer.OpenTK Renderer;
        public ColorControls ColorControls;
        public ImageButton FlagTool;
        private GLWindow game;
        public GameCanvas(SkinBase skin, GLWindow Game) : base(skin)
        {
            game = Game;
            BoundsChanged += GameCanvas_BoundsChanged;
        }

        public bool IsModalOpen
        {
            get { return Children.FirstOrDefault(x => x is Gwen.ControlInternal.Modal) != null; }
        }
        public static GameCanvas CreateCanvas(GLWindow game)
        {
            var renderer = new Gwen.Renderer.OpenTK();

            var tx = new Texture(renderer);
            Gwen.Renderer.OpenTK.LoadTextureInternal(tx, GameResources.DefaultSkin);
            var skin = new TexturedBase(renderer, tx, GameResources.DefaultColors);
            var bmpfont = new Gwen.Renderer.OpenTK.BitmapFont(renderer, "SourceSansPro", 10, 10, GameResources.SourceSansProq,
                new List<System.Drawing.Bitmap> { GameResources.SourceSansPro_img });
            skin.DefaultFont = bmpfont;
            var canvas = new GameCanvas(skin, game) { Renderer = renderer };
            canvas.SpriteLoading = new Sprite(canvas);
            canvas.SpriteLoading.SetImage(GameResources.loading);
            canvas.SpriteLoading.IsHidden = true;
            canvas.SpriteLoading.IsTabable = false;
            canvas.SpriteLoading.KeyboardInputEnabled = false;
            canvas.SpriteLoading.MouseInputEnabled = false;
            canvas.SpriteLoading.SetSize(32, 32);
            canvas.SpriteLoading.RotationPoint.X = 16;
            canvas.SpriteLoading.RotationPoint.Y = 16;
            canvas.SpriteLoading.SetPosition(canvas.Width - 32, 0);

            var timeslider = new HorizontalIntSlider(canvas)
            {
                X = 120,
                Y = canvas.Height - 25,
                Width = canvas.Width - 120 * 2,
                Height = 25,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "timeslider",
            };
            var btnfastfoward = new ImageButton(canvas)
            {
                X = timeslider.Right,
                Y = canvas.Height - 25,
                Width = 24,
                Height = 24,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "btnfastforward"
            };
            btnfastfoward.SetImage(GameResources.fast_forward, GameResources.fast_forward_white);
            var btnslowmo = new ImageButton(canvas)
            {
                X = timeslider.X - 24,
                Y = canvas.Height - 25,
                Width = 24,
                Height = 24,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "btnslowmo"
            };
            btnslowmo.SetImage(GameResources.rewind, GameResources.rewind_white);
            btnslowmo.Clicked += (o, e) => { game.PlaybackDown(); };
            btnfastfoward.Clicked += (o, e) => { game.PlaybackUp(); };

            timeslider.ValueChanged += canvas.timeslider_ValueChanged;
            var labelTrackName = new Label(canvas)
            {
                TextColor = System.Drawing.Color.Black,
                Dock = Pos.Left,
                Margin = new Margin(5, 0, 0, 0),
                Name = "trackname"
            };
            Label labeliteration = new Label(canvas);
            labeliteration.Name = "labeliterations";
            labeliteration.Font = new Font(renderer, "Arial", 18);
            labeliteration.SetText("");
            labeliteration.TextColor = Color.Black;
            Align.PlaceDownLeft(labeliteration, labelTrackName);

            var toprightcontainer = new ControlBase(canvas)
            {
                Dock = Pos.Right,
                Width = 150,
                Height = 300,
                MouseInputEnabled = false
            };
            var fps = new Label(toprightcontainer) { TextColor = Color.Black, Name = "fps" };
            var labelppf = new Label(toprightcontainer) { TextColor = Color.Black, Name = "ppf" };
            var labelPlayback = new Label(toprightcontainer) { TextColor = Color.Black, Name = "labelplayback" };
            var flagtime = new Label(canvas) { TextColor = Color.Black, Name = "flagtime" };
            var textheight = renderer.MeasureText(skin.DefaultFont, "@").Y + 3;
            labelppf.SetPosition(0, textheight);
            labelPlayback.SetPosition(0, textheight * 2);

            var vslider = new VerticalSlider(canvas) { Min = 0.1f, Max = 24f, Value = game.Track.Zoom, IsTabable = false };
            vslider.ValueChanged += (o, e) =>
            {
                var slider = (VerticalSlider)o;
                var diff = slider.Value - game.Track.Zoom;
                game.Zoom(diff, false);
            };
            vslider.SetPosition(0, (int)canvas.Height - 150);
            vslider.Name = "vslider";
            Align.AlignRight(vslider);
            vslider.Height = 125;
            return canvas;
        }
        public override void Dispose()
        {
            var iterations = FindChildByName("labeliterations") as Label;
            iterations?.Font.Dispose();
            base.Dispose();
        }
        private bool _draggingSlider = false;
        private void timeslider_ValueChanged(ControlBase sender, EventArgs arguments)
        {
            var slider = (HorizontalIntSlider)sender;
            if (slider.Held || _draggingSlider)
            {
                game.Track.EnterPlayback();
                game.Track.SetFrame(slider.Value, false);
                game.Track.ExitPlayback();
                if (!game.Track.Playing)
                {
                    game.Track.Camera.SetPosition(game.Track.CameraAroundRider(game.Track.RiderState));
                }
            }
            if (slider.Held)
            {
                _draggingSlider = true;
                if (game.EnableSong)
                {
                    AudioPlayback.Pause();
                }
            }
            else if (_draggingSlider)
            {
                _draggingSlider = false;
                game.Scheduler.Reset();
                if (game.EnableSong)
                {
                    game.UpdateSongPosition(game.Track.CurrentFrame / 40f);
                }
            }
        }

        public void ButtonsToggleNightmode()
        {
            var nightmode = Settings.Default.NightMode;
            var buttons = FindChildByName("buttons");
            foreach (var v in buttons.Children)
            {
                if (v is ImageButton)
                {
                    var tool = (ImageButton)v;
                    tool.Nightmode(nightmode);
                }
            }
            //    _btnPauseTool.Nightmode(nightmode);
            //   _btnFastForward.Nightmode(nightmode);
            //  _btnRewind.Nightmode(nightmode);
        }
        private bool _nodeslocked = false;
        private readonly List<savenode> _nodes = new List<savenode>();
        private class savenode
        {

            #region Fields

            public object data;
            public string name;

            #endregion Fields

        }
        //todo investigate locking on ui thread if already started
        public void UpdateSaveNodes()
        {
            new Thread(InitSaveNodes) { IsBackground = true }.Start(); //in the background, load the save files.
        }

        private void InitSaveNodes()
        {
            if (_nodeslocked)
                return;
            game.Loading = true;
            lock (_nodes)
            {
                _nodeslocked = true;
                _nodes.Clear();
                var files = Program.CurrentDirectory + "Tracks";
                if (Directory.Exists(files))
                {
                    var solfiles =
                        Directory.GetFiles(files, "*.*")
                            .Where(s => s != null && s.EndsWith(".sol", StringComparison.OrdinalIgnoreCase));

                    foreach (var file in solfiles)
                    {
                        List<sol_track> tracks = null;
                        try
                        {
                            tracks = TrackLoader.LoadSol(file);
                        }
                        catch
                        {
                            //ignored
                        }
                        if (tracks != null)
                        {
                            var node = new savenode { name = "[SOL] " + Path.GetFileNameWithoutExtension(file) };
                            var addnode = new List<savenode>();
                            for (var i = 0; i < tracks.Count; i++)
                            {
                                addnode.Add(new savenode { name = tracks[i].name, data = tracks[i] });
                            }
                            node.data = addnode;
                            _nodes.Add(node);
                        }
                    }
                    var trkfiles =
                        Directory.GetFiles(files, "*.*")
                            .Where(s => s != null && s.EndsWith(".trk", StringComparison.OrdinalIgnoreCase));
                    foreach (var trk in trkfiles)
                    {
                        var save = Path.GetFileNameWithoutExtension(trk);
                        var node = new savenode { name = "[TRK] " + save };
                        var addnode = new List<savenode>();
                        addnode.Add(new savenode { name = (save), data = new[] { save, null } });
                        node.data = addnode;
                        _nodes.Add(node);
                    }
                    var folders = Directory.GetDirectories(files);
                    foreach (var folder in folders)
                    {
                        var trackname = Path.GetFileName(folder);
                        var node = new savenode { name = trackname };
                        var trackfiles = TrackLoader.EnumerateTRKFiles(folder);
                        var addnode = new List<savenode>();
                        for (var i = 0; i < trackfiles.Length; i++)
                        {
                            var trk = trackfiles[i];
                            var save = Path.GetFileNameWithoutExtension(trk);
                            addnode.Add(new savenode { name = (save), data = new[] { trackname, save } });
                        }
                        node.data = addnode;
                        _nodes.Add(node);
                    }
                }
                _nodeslocked = false;
            }
            game.Loading = false;
            game.Invalidate();
        }
        public void ShowLoadWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            game.Track.Stop();
            var wc = new WindowControl(this, "Load Track", false) { DeleteOnClose = true };
            wc.MakeModal(true);
            var tv = new TreeControl(wc);
            tv.Name = "loadtree";
            lock (_nodes)
            {
                for (var i = 0; i < _nodes.Count; i++)
                {
                    var root = tv.AddNode(_nodes[i].name);
                    var list = (List<savenode>)_nodes[i].data;
                    for (var i1 = 0; i1 < list.Count; i1++)
                    {
                        if (i1 == 0)
                            root.UserData = list[i1].data;
                        root.AddNode(list[i1].name).UserData = list[i1].data;
                    }
                }
            }
            var container = new ControlBase(wc);
            container.Height = 30;
            container.Dock = Pos.Bottom;

            wc.Width = 400;
            wc.Height = 400;
            wc.MinimumSize = new System.Drawing.Point(wc.Width, wc.Height);

            wc.SetPosition((int)(Width / 2) - (wc.Width / 2), (int)(Height / 2) - (wc.Height / 2));
            //     wc.DisableResizing();
            tv.Dock = Pos.Fill;
            var btn = new Button(container);
            btn.Margin = new Margin(0, 5, 0, 5);
            btn.Dock = Pos.Left;
            btn.Height = 20;
            btn.Width = 150;
            btn.Text = "Load";
            btn.Clicked += loadbtn_Clicked;
            var btndelete = new Button(container);
            btndelete.Margin = btn.Margin;
            btndelete.Dock = Pos.Right;
            btndelete.Height = 20;
            btndelete.Width = 50;
            btndelete.Text = "Delete";
            btndelete.Clicked += btndelete_Clicked;
            wc.Show();

            game.Cursor = MouseCursor.Default;
        }
        private void btndelete_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            var window = (WindowControl)sender.Parent.Parent;
            var tv = (TreeControl)window.FindChildByName("loadtree", true);
            var en = (List<TreeNode>)tv.SelectedChildren;
            if (en.Count > 0)
            {
                var selected = en[0];
                var wc = new WindowControl(this, "Delete Track", false);
                wc.MakeModal(true);
                wc.DeleteOnClose = true;
                var mg = new Margin(0, 30, 0, 5);
                var btnok = new Button(wc);
                btnok.Clicked += (o, e) =>
                {
                    if (selected.UserData is sol_track)
                    {
                        if (!selected.IsRoot)
                            return;
                        wc.Close();
                        var data = selected.UserData as sol_track;
                        File.Delete(data.filename);
                    }
                    else
                    {
                        wc.Close();
                        var data = (string[])selected.UserData;
                        if (selected.IsRoot)
                        {
                            try
                            {
                                if (data[1] == null)
                                {
                                    File.Delete(Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar + data[0] + ".trk");
                                }
                                else
                                {
                                    Directory.Delete(
                                        Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar + data[0] + Path.DirectorySeparatorChar, true);
                                }
                            }
                            catch
                            {
                                return;
                            }
                            for (var i = 0; i < _nodes.Count; i++)
                            {
                                if (_nodes[i].name == data[0])
                                {
                                    _nodes.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                File.Delete(Program.CurrentDirectory + "Tracks" +
                                            Path.DirectorySeparatorChar + data[0] +
                                            Path.DirectorySeparatorChar + data[1] + ".trk");
                            }
                            catch
                            {
                                return;
                            }
                            for (var i = 0; i < _nodes.Count; i++)
                            {
                                if (_nodes[i].name == data[0])
                                {
                                    var nod = (List<savenode>)_nodes[i].data;
                                    for (var p = 0; p < nod.Count; p++)
                                    {
                                        if (nod[p].name == data[1])
                                        {
                                            nod.RemoveAt(p);
                                            break;
                                        }
                                    }
                                    if (nod.Count == 0)
                                    {
                                        _nodes.RemoveAt(i);
                                        try
                                        {
                                            Directory.Delete(
                                                Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar +
                                                data[0] + Path.DirectorySeparatorChar, true);
                                        }
                                        catch
                                        {
                                            return;
                                        }
                                        selected.Parent.Parent.RemoveChild(selected.Parent, true);
                                    }
                                    else
                                    {
                                        selected.Parent.UserData = _nodes[_nodes.Count - 1];
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    selected.Parent.RemoveChild(selected, true);
                };
                btnok.Dock = Pos.Left;
                btnok.Text = "Okay";
                btnok.Margin = mg;
                var btncancel = new Button(wc);
                btncancel.Clicked += (o, e) => { wc.Close(); };
                btncancel.Text = "Cancel";
                btncancel.Dock = Pos.Right;
                btncancel.Margin = mg;
                var lbl = new Label(wc)
                {
                    Dock = Pos.Center,
                    Text = "Are you sure you want to delete the track" + (selected.IsRoot ? " folder?" : "?")
                };
                lbl.SizeToContents();
                wc.Width = lbl.Width + 12;
                wc.Height = 55 + mg.Top;
                wc.Show();
                wc.SetPosition((int)(Width / 2) - (wc.Width / 2),
                    (int)(Height / 2) - (wc.Height / 2));
                wc.DisableResizing();
            }
        }
        private void loadbtn_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            var window = (WindowControl)sender.Parent.Parent;
            var tv = (TreeControl)window.FindChildByName("loadtree", true);

            var en = (List<TreeNode>)tv.SelectedChildren;
            if (en.Count > 0)
            {
                var selected = en[0];
                if (selected.UserData is sol_track)
                {
                    var data = (sol_track)selected.UserData;
                    try
                    {
                        game.EnableSong = false;
                        game.Track.ChangeTrack(TrackLoader.LoadTrack(data));
                    }
                    catch
                    {
                        window.Close();
                        var wc = PopupWindow.Create(this, game, "An error occured loading the track.", "Error", true, false);
                        wc.FindChildByName("Okay", true).Clicked += (o, e) => { wc.Close(); };
                        return;
                    }
                }
                else if (selected.UserData is string[])
                {
                    var data = (string[])selected.UserData;
                    try
                    {
                        game.EnableSong = false;
                        game.Track.ChangeTrack(TrackLoader.LoadTrackTRK(data[0], data[1]));
                    }
                    catch
                    {
                        window.Close();
                        var wc = PopupWindow.Create(this, game, "An error occured loading the track. \nIt might be from a newer version.", "Error", true, false);
                        wc.FindChildByName("Okay", true).Clicked += (o, e) => { wc.Close(); };
                        return;
                    }
                }
                game.Track.TrackUpdated();
                window.Close();
            }
        }
        public void ShowSaveWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            Windows.SaveWindow sw = new Windows.SaveWindow(this, game);
            var bottom = sw.FindChildByName("bottom", true);
            var btn = new Button(bottom) { Name = "savebtn" };
            btn.Width = 50;
            btn.Text = "Save";
            btn.Dock = Pos.Right;
            btn.Clicked += savebtn_Clicked;

            sw.Show();
            sw.SetPosition((int)(Width / 2) - (sw.Width / 2), (int)(Height / 2) - (sw.Height / 2));
            sw.DisableResizing();

        }
        public void ShowSongWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            game.Track.Stop();
            var wc = new WindowControl(this, "Song Sync", false) { DeleteOnClose = true };
            wc.MakeModal(true);

            wc.MinimumSize = new System.Drawing.Point(220, 240);

            wc.Width = 220;
            wc.Height = 240;
            wc.SetPosition((int)(Width / 2) - (wc.Width / 2), (int)(Height / 2) - (wc.Height / 2));
            //wc.DisableResizing();
            var enablesongcb = new LabeledCheckBox(wc);
            enablesongcb.CheckChanged += (o, e) => { game.EnableSong = enablesongcb.IsChecked; };
            enablesongcb.IsChecked = game.EnableSong;
            enablesongcb.Text = "Enable Song";
            enablesongcb.Dock = Pos.Top;
            var gb = new GroupBox(wc);
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
                        if (lower.EndsWith(type,StringComparison.OrdinalIgnoreCase))
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
                var tc = (TreeControl)wc.FindChildByName("songtv", true);
                var list = (List<TreeNode>)tc.SelectedChildren;
                if (list.Count == 1)
                {
                    game.CurrentSong.Location = (string)list[0].UserData;
                }
            };
            wc.IsHiddenChanged += (o, e) =>
            {
                if (!wc.IsHidden) return;
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
                        (HorizontalSlider)wc.FindChildByName("volume", true);
                    Settings.Default.Volume = svolume.Value;
                    Settings.Default.Save();
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
            var container2 = new ControlBase(wc);
            container2.Dock = Pos.Bottom;
            container2.Height = 40;
            label = new Label(container2);
            label.Margin = new Margin() { Top = 13 };
            label.Dock = Pos.Left;
            label.Text = "Volume:";
            var volume = new HorizontalSlider(container2);
            volume.Min = 0;
            volume.Max = 100;
            if (Settings.Default.Volume > 100)
                Settings.Default.Volume = 100;
            if (Settings.Default.Volume < 0)
                Settings.Default.Volume = 0;
            volume.Value = Settings.Default.Volume;
            volume.Name = "volume";
            volume.SnapToNotches = false;
            volume.KeyboardInputEnabled = false;
            volume.Width = 150;
            volume.Dock = Pos.Right;
            wc.Show();
            game.Cursor = MouseCursor.Default;
        }
        public void ShowDelete()
        {
            var window = PopupWindow.Create(this, game, "Do you want to delete the current track?", "Delete Track", true, true);
            window.FindChildByName("Okay", true).Clicked += (o, e) =>
            {
                game.Track.Stop();
                game.Track.ChangeTrack(new Track() { Name = "untitled" });
                game.Invalidate();
                window.Close();
            };
            window.FindChildByName("Cancel", true).Clicked += (o, e) => { window.Close(); };
        }
        private void savebtn_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            var window = sender.Parent.Parent as WindowControl;
            if (window == null)
                throw new Exception("Invalid window data");
            if (window.UserData != null)
            {
                var tb = (TextBox)window.FindChildByName("tb", true);
                var saveindex = 0;
                var txt = (string)window.UserData;
                if (txt == "<create new track>")
                {
                    txt = tb.Text;
                    if (txt.Length == 0)
                        return;
                }
                if (
                    Directory.Exists(Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar + txt +
                                     Path.DirectorySeparatorChar))
                {
                    var trackfiles =
                        TrackLoader.EnumerateTRKFiles(Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar +
                                                      txt);
                    for (var i = 0; i < trackfiles.Length; i++)
                    {
                        var s = Path.GetFileNameWithoutExtension(trackfiles[i]);
                        var index = s.IndexOf(" ", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            s = s.Remove(index);
                        }
                        if (int.TryParse(s, out saveindex))
                        {
                            break;
                        }
                    }
                }
                var invalidchars = Path.GetInvalidFileNameChars();
                for (var i = 0; i < txt.Length; i++)
                {
                    if (invalidchars.Contains(txt[i]))
                    {
                        sender.SetToolTipText("Attempted to save with an invalid name");
                        return;
                    }
                }
                game.Track.Name = txt;
                saveindex++;
                var save = saveindex + " " + tb.Text;
                try
                {
                    game.Track.Save(save, game.CurrentSong);
                }
                catch
                {
                    sender.SetToolTipText("An error occured trying to save");
                    return;
                }
                savenode node = null;
                for (var i = 0; i < _nodes.Count; i++)
                {
                    if (_nodes[i].name.Equals(game.Track.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        node = _nodes[i];
                        break;
                    }
                }
                if (node == null)
                {
                    node = new savenode { data = new List<savenode>(), name = game.Track.Name };
                    _nodes.Add(node);
                }
                ((List<savenode>)node.data).Insert(0, new savenode { name = (save), data = new[] { game.Track.Name, save } });
            }
            window.Close();
        }
        public void ShowPreferences()
        {
            var trk = game.Track;
            if (trk.Animating && !trk.Paused)
            {
                trk.TogglePause();
            }
            if (GetOpenWindows().Count != 0)
                return;
            var pw = new Windows.PreferencesWindow(this, game);
            pw.Show();
            pw.SetPosition(Width / 2 - (pw.Width / 2), Height / 2 - (pw.Height / 2));

            game.Cursor = MouseCursor.Default;
        }
        public List<ControlBase> GetOpenWindows()
        {
            List<ControlBase> ret = new List<ControlBase>();
            foreach (var child in Children)
            {
                if (child is WindowControl)
                {
                    ret.Add(child);
                }
                else if (child is Gwen.ControlInternal.Modal)
                {
                    ret.Add(child.Children.Single(x => x is WindowControl));
                }
            }
            return ret;
        }
        public void SetTooltip(ControlBase control, string str)
        {
            if (control == null)
                control = this;
            control.SetToolTipText(str);
            ToolTip.Enable(control);
        }
        public void RemoveTooltip(ControlBase control)
        {
            if (control == null)
                control = this;
            ToolTip.Disable(control);
            control.Tooltip = null;
        }
        int _lastfpsupdate = 0;
        protected override void Render(SkinBase skin)
        {
            SpriteLoading.IsHidden = !game.Loading;
            if (game.Loading)
                SpriteLoading.Rotation = (Environment.TickCount % 1000) / 1000f;
            var trackname = (Label)FindChildByName("trackname");
            trackname.Text = game.Track.Name;
            var sppf = "";
            var playback = "";
            var fpsorlinecount = "";
            var fpslabel = ((Label)FindChildByName("fps", true));
            var ppflabel = ((Label)FindChildByName("ppf", true));
            var labelplayback = ((Label)FindChildByName("labelplayback", true));
            if (game.Track.Animating)
            {
                if (Math.Abs(Environment.TickCount - _lastfpsupdate) > 500)
                {
                    _lastfpsupdate = Environment.TickCount;
                    fpsorlinecount = game.SettingRecordingMode ? "40 FPS" : Math.Round(game.Track.FpsCounter.FPS) + " FPS";
                    if (!game.SettingShowFps && game.SettingRecordingMode)
                    {
                        fpsorlinecount = "";
                    }
                }
                else
                {
                    fpsorlinecount = game.SettingRecordingMode ? "40 FPS" : fpslabel.Text;
                }
                var mo = Vector2d.Zero;
                for (int i = 0; i < game.Track.RiderState.ModelAnchors.Length; i++)
                {
                    mo += game.Track.RiderState.ModelAnchors[i].Momentum;
                }
                mo /= game.Track.RiderState.ModelAnchors.Length;
                var ppf = Math.Sqrt(mo.X * mo.X) + Math.Sqrt(mo.Y * mo.Y);
                var pixels = Math.Round(ppf, 2);
                sppf = string.Format("{0:N2}", pixels) + " ppf";
                if (!game.SettingShowPpf && game.SettingRecordingMode)
                {
                    sppf = "";
                }
                var currts = TimeSpan.FromSeconds(game.Track.CurrentFrame / 40f);
                playback = currts.ToString("mm\\:ss") + ":" + (game.Track.CurrentFrame % 40f) + " " +
                    Math.Round(game.Scheduler.UpdatesPerSecond / 40f, 3) + "x";
                if (!game.SettingShowTimer && game.SettingRecordingMode)
                {
                    playback = "";
                }
            }
            else
            {
                _lastfpsupdate = 0;
                fpsorlinecount = "Lines: " + game.Track.LineCount;
            }
            fpslabel.Text = fpsorlinecount;
            ppflabel.Text = sppf;
            labelplayback.Text = playback;
            var par = fpslabel.Parent;
            fpslabel.X = par.Width - fpslabel.Width;
            ppflabel.X = par.Width - ppflabel.Width;
            labelplayback.X = par.Width - labelplayback.Width;
            var flag = game.Track.GetFlag();
            var labelflagtime = (Label)FindChildByName("flagtime");
            if (flag != null)
            {
                var cam = game.Track.CameraAroundRider(flag.State);
                cam.X -= 15;
                cam.Y -= 15;
                var ts = TimeSpan.FromSeconds((flag.Frame) / 40f);
                labelflagtime.IsHidden = false;
                labelflagtime.Text = ts.ToString("mm\\:ss") + ":" + (flag.Frame % 40f);
                labelflagtime.SetPosition((float)(cam.X + game.ScreenTranslation.X) * game.Track.Zoom,
                    (float)(cam.Y + game.ScreenTranslation.Y) * game.Track.Zoom);
            }
            else
            {
                labelflagtime.IsHidden = true;
            }
            base.Render(skin);
        }

        internal void DisableFlagTooltip()
        {
            RemoveTooltip(FlagTool);
        }

        internal void CalculateFlag(GLTrack.Tracklocation loc)
        {
            if (loc?.State == null || FlagTool.Tooltip != null) return;
            var invalid = false;
            var state = new Rider();
            game.Track.Reset(state);
            var frame = loc.Frame;
            if (frame > 400) //many frames, will likely lag the game. Update the window as a fallback.
            {
                if (frame > 10000) //too many frames, could lag the game very bad.
                {
                    SetTooltip(FlagTool, "Flag is incalculable.");
                    game.Invalidate();
                    return;
                }
                game.Title = Program.WindowTitle + " [Validating flag]";
            }

            for (var i = 0; i < frame; i++) //tick the exact number of frames that the flag should be on.
            {
                game.Track.Tick(state);
            }
            for (var i = 0; i < state.ModelAnchors.Count(); i++)
            {
                if (state.ModelAnchors[i].Position != loc.State.ModelAnchors[i].Position ||
                    state.ModelAnchors[i].Prev != loc.State.ModelAnchors[i].Prev)
                {
                    invalid = true;
                    break;
                }
            }
            SetFlagTooltip(!invalid);
            SetTooltip(FlagTool, invalid ? "Flag is invalid" : "Flag is valid");
            if (frame > 400)
            {
                game.Title = Program.WindowTitle;
            }
            game.Invalidate();
        }

        internal void SetFlagTooltip(bool valid)
        {
            SetTooltip(FlagTool, valid ? "Flag is valid" : "Flag is invalid");
        }

        private void GameCanvas_BoundsChanged(ControlBase sender, EventArgs arguments)
        {
            SpriteLoading.SetPosition(Width - 32, 0);
            var slider = (HorizontalIntSlider)FindChildByName("timeslider");
            slider.X = 120;
            slider.Width = Width - 120 * 2;
            slider.Y = Height - 25;
            var vslider = FindChildByName("vslider", true);
            vslider.X = Width - vslider.Width;
            vslider.Y = Height - vslider.Height - 25;
            FindChildByName("btnslowmo").SetPosition(slider.X - 24, slider.Y);
            FindChildByName("btnfastforward").SetPosition(slider.Right, slider.Y);
        }
    }
}