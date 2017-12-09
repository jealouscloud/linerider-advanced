using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwen;
using Gwen.Controls;
using Gwen.Controls.Property;
using System.IO;
namespace linerider.Windows
{
    class LoadWindow : Window
    {
        public static List<Tuple<string, List<sol_track>>> sols = new List<Tuple<string, List<sol_track>>>();
        public static void UpdateSOLs()
        {
            sols.Clear();
            var newlist = new List<Tuple<string, List<sol_track>>>();
            var files = Program.UserDirectory + "Tracks";
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
                        newlist.Add(new Tuple<string, List<sol_track>>(Path.GetFileName(file), tracks));
                    }
                    catch
                    {
                        //ignored
                    }
                }
            }
            sols = newlist;
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
            foreach (var sol in sols)
            {
                AddSolTracks(tv, sol.Item1, sol.Item2);
            }
            if (Directory.Exists(files))
            {
                var trkfiles =
       Directory.GetFiles(files, "*.*")
           .Where(s => s != null && s.EndsWith(".trk", StringComparison.OrdinalIgnoreCase));
                foreach (var trk in trkfiles)
                {
                    AddTrack(tv, trk, null);
                }
                var folders = Directory.GetDirectories(files);
                foreach (var folder in folders)
                {
                    var trackname = Path.GetFileName(folder);
                    var trackfiles = TrackLoader.EnumerateTRKFiles(folder);

                    AddTrack(tv, trackname, trackfiles);
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

            game.Cursor = OpenTK.MouseCursor.Default;
        }
        private void AddSolTracks(TreeControl tv, string name, List<sol_track> tracks)
        {
            var rootnode = tv.AddNode(name);
            if (tracks.Count != 0)
                rootnode.UserData = tracks[0];
            foreach (var child in tracks)
            {
                rootnode.AddNode(child.name).UserData = child;
            }
        }
        private void AddTrack(TreeControl tv, string root, string[] children)
        {
            if (children == null)
            {
                tv.AddNode(Path.GetFileName(root)).UserData = root;
            }
            else
            {
                if (children.Length != 0)
                {
                    var rootnode = tv.AddNode(root);
                    rootnode.UserData = root;
                    foreach (var child in children)
                    {
                        rootnode.AddNode(Path.GetFileName(child)).UserData = child;
                    }
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
                        var data = (string)selected.UserData;
                        var isfile = data.EndsWith(".trk", StringComparison.OrdinalIgnoreCase);
                        if (selected.IsRoot)
                        {
                            try
                            {
                                if (isfile)
                                {
                                    File.Delete(Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar + data[0]);
                                }
                                else
                                {
                                    Directory.Delete(
                                        Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar + data[0] + Path.DirectorySeparatorChar, true);
                                }
                            }
                            catch
                            {
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                File.Delete(Program.UserDirectory + "Tracks" +
                                            Path.DirectorySeparatorChar + data[0] +
                                            Path.DirectorySeparatorChar + data[1] + ".trk");
                            }
                            catch
                            {
                                return;
                            }
                        }
                    }

                    selected.Parent.RemoveChild(selected, true);
                    if (!selected.IsRoot && selected.Parent.Children.Count == 0)
                    {
                        selected.Parent.Parent.RemoveChild(selected.Parent, true);
                    }
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
                else if (selected.UserData is string)
                {
                    var data = (string)selected.UserData;
                    try
                    {
                        bool trackfolder = selected.Children.Count > 0;
                        string trackname = selected.IsRoot ? Path.GetFileNameWithoutExtension(data) : (string)selected.Parent.UserData;
                        if (trackfolder)
                        {
                            data = (string)selected.Children[0].UserData;
                        }
                        game.EnableSong = false;
                        game.Track.ChangeTrack(TrackLoader.LoadTrackTRK(data, trackname));
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
    }
}
