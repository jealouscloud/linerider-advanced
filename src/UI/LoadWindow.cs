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
using Gwen;
using Gwen.Controls;
using Gwen.Controls.Property;
using System.IO;
namespace linerider.UI
{
    class LoadWindow : WindowControl
    {
        class BadException : Exception
        {
            public BadException(string s) : base(s)
            {
            }
        }
        private GLWindow game;
        public LoadWindow(Gwen.Controls.ControlBase parent, GLWindow glgame) : base(parent, "Load Track")
        {
            game = glgame;
            game.Track.Stop();
            MakeModal(true);
            var tv = new TreeControl(this);
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
                var trkfiles = Directory.GetFiles(files, "*.*")
                        .Where(s => s != null && s.EndsWith(".trk", StringComparison.OrdinalIgnoreCase));
                foreach (var trk in trkfiles)
                {
                    AddTrack(tv, trk, null);
                }

                var folders = Directory.GetDirectories(files);
                foreach (var folder in folders)
                {
                    var trackfiles = TrackLoader.EnumerateTRKFiles(folder);

                    AddTrack(tv, folder, trackfiles);
                }
            }
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
                        if (selected.UserData is sol_track)
                        {
                            if (!selected.IsRoot)
                                return;
                            var data = selected.UserData as sol_track;
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
                if (selected.UserData is sol_track)
                {
                    var data = (sol_track)selected.UserData;
                    try
                    {
                        Settings.Local.EnableSong = false;
                        game.Track.ChangeTrack(TrackLoader.LoadTrack(data));
                    }
                    catch (Exception e)
                    {
                        if (Program.IsDebugged)
                            throw e;
                        window.Close();
                        PopupWindow.Error("An error occured loading the track.", "Error");
                        return;
                    }
                }
                else if (selected.UserData is string)
                {
                    var data = (string)selected.UserData;
                    try
                    {
                        if (data.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))//unopened sol file
                        {
                            List<sol_track> tracks = null;
                            try
                            {
                                tracks = TrackLoader.LoadSol(data);
                                if (tracks.Count == 0)
                                    return;
                                foreach (var track in tracks)
                                {
                                    selected.AddNode(track.name).UserData = track;
                                }
                                if (tracks.Count == 1)
                                {
                                    Settings.Local.EnableSong = false;
                                    game.Track.ChangeTrack(TrackLoader.LoadTrack(tracks[0]));
                                }
                                else
                                {
                                    selected.UserData = tracks[0];
                                    selected.ExpandAll();
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                if (Program.IsDebugged)
                                    throw e;
                                PopupWindow.Error("An error occured loading the .sol", "Error!");
                            }
                        }
                        else
                        {
                            bool trackfolder = selected.Children.Count > 0;
                            string trackname;
                            if (trackfolder)
                            {
                                // getfilenamewithoutextension interacts weird
                                // with periods in the directory name.
                                trackname = Path.GetFileName(data);
                                data = (string)selected.Children[0].UserData;
                            }
                            else
                            {
                                trackname = Path.GetFileNameWithoutExtension(data);
                            }
                            Settings.Local.EnableSong = false;
                            game.Track.ChangeTrack(TrackLoader.LoadTrackTRK(data, trackname));
                        }
                    }
                    catch (Exception e)
                    {
                        if (Program.IsDebugged)
                            throw e;
                        window.Close();
                        PopupWindow.Error("An error occured loading the track. \nIt might be from a newer version.");
                        return;
                    }
                }
                game.Track.NotifyTrackChanged();
                window.Close();
            }
        }
    }
}
