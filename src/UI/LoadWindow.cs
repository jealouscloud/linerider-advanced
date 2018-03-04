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
using System.Text;
using System.Threading;
using Gwen;
using Gwen.Controls;
using Gwen.Controls.Property;
using System.IO;
using linerider.IO;
using linerider.IO.SOL;
namespace linerider.UI
{
    class LoadWindow : WindowControl
    {
        const string DefaultTitle = "Load Track";
        class BadException : Exception
        {
            public BadException(string s) : base(s)
            {
            }
        }
        private MainWindow game;
        public LoadWindow(Gwen.Controls.ControlBase parent, MainWindow glgame) : base(parent, DefaultTitle)
        {
            game = glgame;
            game.Track.Stop();
            MakeModal(true);
            var tv = new TreeControl(this);
            tv.Margin = Margin.One;
            tv.Name = "loadtree";
            var files = Program.UserDirectory + "Tracks";
            if (Directory.Exists(files))
            {
                var solfiles = Directory.GetFiles(files, "*.*")
                        .Where(s => s != null && s.EndsWith(".sol", StringComparison.OrdinalIgnoreCase));
                foreach (var sol in solfiles)
                {
                    AddTrack(tv, sol, null);
                }
            }
            if (Directory.Exists(files))
            {
                var trkfiles = TrackIO.EnumerateTrackFiles(files);
                foreach (var trk in trkfiles)
                {
                    AddTrack(tv, trk, null);
                }

                var folders = Directory.GetDirectories(files);
                foreach (var folder in folders)
                {
                    var trackfiles = TrackIO.EnumerateTrackFiles(folder);

                    AddTrack(tv, folder, trackfiles);
                }
            }
            tv.SelectionChanged += treeview_SelectionChanged;
            var container = new ControlBase(this);
            container.Height = 30;
            container.Dock = Pos.Bottom;

            this.Width = 400;
            this.Height = 400;
            this.MinimumSize = new System.Drawing.Point(this.Width, this.Height);

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

            game.Cursor = game.Cursors["default"];
        }
        private void AddTrack(TreeControl tv, string fileroot, string[] childpaths)
        {
            // all filenames are expected to be passed as absolute
            // fileroot, childpaths.
            if (childpaths == null)
            {
                var rootnode = tv.AddNode(Path.GetFileName(fileroot));
                rootnode.UserData = fileroot;
            }
            else if (childpaths.Length != 0)
            {
                //a folder of tracks.
                var rootnode = tv.AddNode(Path.GetFileName(fileroot));

                rootnode.UserData = fileroot;
                foreach (var child in childpaths)
                {
                    rootnode.AddNode(Path.GetFileName(child)).UserData = child;
                }
            }
        }
        private void treeview_SelectionChanged(ControlBase sender, EventArgs e)
        {
            var tv = (TreeControl)this.FindChildByName("loadtree", true);

            var en = (List<TreeNode>)tv.SelectedChildren;
            this.Title = DefaultTitle;
            if (en.Count == 1)
            {
                var selected = en[0];
                var userdata = selected.UserData;
                if (userdata is string filepath)
                {
                    // track folder
                    bool folder =selected.Children.Count > 0;
                    if (folder)
                    {
                        filepath = (string)selected.Children[0].UserData;
                    }
                    var title = Path.GetFileName(filepath);
                    if (folder)
                    {
                        title = Path.GetFileName((string)userdata)+Path.DirectorySeparatorChar+title;
                    }
                    this.Title = DefaultTitle+" -- " + title;
                }
            }
        }
        private void btndelete_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            var window = (WindowControl)sender.Parent.Parent;
            var tv = (TreeControl)window.FindChildByName("loadtree", true);
            var en = (List<TreeNode>)tv.SelectedChildren;
            if (en.Count > 0)
            {
                var selected = en[0];
                var delwindow = new WindowControl(this, "Delete Track", false);
                delwindow.MakeModal(true);
                delwindow.DeleteOnClose = true;
                var mg = new Margin(0, 30, 0, 5);
                var btnok = new Button(delwindow);
                btnok.Clicked += (o, e) =>
                {
                    try
                    {
                        if (selected.UserData is linerider.IO.SOL.sol_track sol)
                        {
                            if (!selected.IsRoot)
                                return;
                            var data = sol;
                            File.Delete(data.filename);
                        }
                        else if (selected.UserData is string)
                        {
                            var data = (string)selected.UserData;
                            var dir = Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar;
                            if (!data.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                                throw new BadException("Please report this bug immediately. LRA just tried to delete a file outside of the tracks folder.");
                            var subs = data.IndexOf(dir, StringComparison.OrdinalIgnoreCase) + dir.Length;
                            if (subs + 1 >= data.Length || data[subs + 1] == Path.DirectorySeparatorChar)
                            {
                                throw new BadException("Please report this bug immediately. LRA might have just tried to delete your whole tracks folder.");
                            }
                            if (data.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))//unopened sol file
                            {
                                File.Delete(data);
                            }
                            else
                            {
                                bool trackfolder = selected.Children.Count > 0;
                                if (trackfolder)
                                {
                                    Directory.Delete(data, true);
                                }
                                else
                                {
                                    File.Delete(data);
                                }
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (BadException ue)
                    {
                        throw ue;
                    }
                    catch
                    {
                        return;
                    }
                    selected.Parent.RemoveChild(selected, true);
                    if (!selected.IsRoot && selected.Parent.Children.Count == 0)
                    {
                        selected.Parent.Parent.RemoveChild(selected.Parent, true);
                    }
                    this.Title = DefaultTitle;
                    delwindow.Close();
                };
                btnok.Dock = Pos.Left;
                btnok.Text = "Okay";
                btnok.Margin = mg;
                var btncancel = new Button(delwindow);
                btncancel.Clicked += (o, e) => { delwindow.Close(); };
                btncancel.Text = "Cancel";
                btncancel.Dock = Pos.Right;
                btncancel.Margin = mg;
                var lbl = new Label(delwindow)
                {
                    Dock = Pos.Center,
                    Text = "Are you sure you want to delete the track" + (selected.Children.Count > 0 ? " folder?" : "?")
                };
                lbl.SizeToContents();
                delwindow.Width = lbl.Width + 12;
                delwindow.Height = 55 + mg.Top;
                delwindow.Show();
                delwindow.SetPosition((game.Canvas.Width / 2) - (delwindow.Width / 2), (game.Canvas.Height / 2) - (delwindow.Height / 2));
                delwindow.DisableResizing();
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
                if (selected.UserData is sol_track sol)
                {
                    LoadSOL(sol);
                }
                else if (selected.UserData is string)
                {
                    var filepath = (string)selected.UserData;
                    if (filepath.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))//unopened sol file
                    {
                        if (!ExpandSOL(filepath, selected))
                            return;
                    }
                    else
                    {
                        bool trackfolder = selected.Children.Count > 0;
                        if (trackfolder)
                        {
                            filepath = (string)selected.Children[0].UserData;
                        }
                        Settings.Local.EnableSong = false;
                        LoadTRK(filepath);
                    }
                }
                game.Track.NotifyTrackChanged();
                window.Close();
            }
        }
        /// <summary>
        /// Displays all the tracks contained in an sol
        /// returns true if the window can close.
        /// </summary>
        private bool ExpandSOL(string filepath, TreeNode parent)
        {
            try
            {
                var tracks = SOLLoader.LoadSol(filepath);
                if (tracks.Count != 0)
                {
                    foreach (var track in tracks)
                    {
                        parent.AddNode(track.name).UserData = track;
                    }
                    if (tracks.Count == 1)
                    {
                        Settings.Local.EnableSong = false;
                        game.Track.ChangeTrack(SOLLoader.LoadTrack(tracks[0]));
                        return true;
                    }
                    else
                    {
                        parent.UserData = tracks[0];
                        parent.ExpandAll();
                        return false;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                if (Program.IsDebugged)
                    throw e;
                PopupWindow.QueuedActions.Enqueue(() =>
                PopupWindow.Error(
                    "Failed to load the .sol track:" +
                    Environment.NewLine +
                    e.Message));
                return true;
            }
        }
        private void LoadSOL(sol_track sol)
        {
            try
            {
                Settings.Local.EnableSong = false;
                game.Track.ChangeTrack(SOLLoader.LoadTrack(sol));
            }
            catch (Exception e)
            {
                if (Program.IsDebugged)
                    throw e;
                PopupWindow.QueuedActions.Enqueue(() =>
                PopupWindow.Error(
                    "Failed to load the track:" +
                    Environment.NewLine +
                    e.Message));
                return;
            }
        }
        private void LoadTRK(string filepath)
        {
            string name = TrackIO.GetTrackName(filepath);
            if (!ThreadPool.QueueUserWorkItem((o) => LoadTrack(filepath, name)))
            {
                LoadTrack(filepath, name);
            }
        }
        private void LoadTrack(string file, string name)
        {
            game.Loading = true;
            try
            {
                Track track;
                if (file.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase))
                {
                    track = TRKLoader.LoadTrack(file, name);
                }
                else
                {
                    throw new Exception("Filetype unknown");
                }
                game.Track.ChangeTrack(track);
                Settings.LastSelectedTrack = file;
                Settings.Save();
            }
            catch (TrackIO.TrackLoadException e)
            {
                PopupWindow.QueuedActions.Enqueue(() =>
                PopupWindow.Error(
                    "Failed to load the track:" +
                    Environment.NewLine +
                    e.Message));
                return;
            }
            catch (Exception e)
            {
                PopupWindow.QueuedActions.Enqueue(() =>
                PopupWindow.Error(
                    "An unknown error occured while loading the track." +
                    Environment.NewLine +
                    e.Message));
                return;
            }
            finally
            {
                game.Loading = false;
            }
        }
    }
}
