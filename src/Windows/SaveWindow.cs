//
//  SaveWindow.cs
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
using System.IO;
using System.Linq;
using System.Text;
using Gwen;
using Gwen.Controls;
using Gwen.Controls.Property;
using OpenTK;

namespace linerider.Windows
{
    class SaveWindow : Window
    {
        private GLWindow game;
        public SaveWindow(Gwen.Controls.ControlBase parent, GLWindow glgame) : base(parent, "Save Track")
        {
            game = glgame;
            game.Track.Stop();
            MakeModal(true);
            Label l = new Label(this);
            l.Text = "Tracks are saved to\r\nDocuments/LRA/Tracks";

            var bottom = new PropertyBase(this) { Name = "bottom", Margin = new Margin(0, 10, 0, 5), Height = 30 };
            var cb = new ComboBox(this);

            cb.ItemSelected += (o, e) =>
            {
                var snd = ((ComboBox)o);
                var txt = snd.SelectedItem.Text;
                this.UserData = txt;
            };
            var tb = new TextBox(bottom) { Name = "tb" };

            tb.Dock = Pos.Left;
            bottom.Dock = Pos.Bottom;
            cb.Dock = Pos.Bottom;
            cb.Margin = new Margin(0, 0, 0, 0);
            this.Width = 200;
            this.Height = 130;
            var dir = Program.UserDirectory + "Tracks";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var folders = Directory.GetDirectories(Program.UserDirectory + "Tracks");
            cb.AddItem("<create new track>"); //< used as it cant be used as a file character
            cb.SelectByText("<create new track>");
            foreach (var folder in folders)
            {
                var trackname = Path.GetFileName(folder);
                cb.AddItem(trackname);
            }
            cb.SelectByText(game.Track.Name);
            var btn = new Button(bottom) { Name = "savebtn" };
            btn.Width = 50;
            btn.Text = "Save";
            btn.Dock = Pos.Right;
            btn.Clicked += savebtn_Clicked;
            DisableResizing();
            game.Cursor = MouseCursor.Default;
        }
        private void savebtn_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            var window = sender.Parent.Parent as WindowControl;
            if (window == null)
                throw new Exception("Invalid window data");
            if (window.UserData != null)
            {
                var tb = (TextBox)window.FindChildByName("tb", true);
                var txt = (string)window.UserData;
                if (txt == "<create new track>")
                {
                    txt = tb.Text;
                    if (txt.Length == 0)
                        return;
                }
                if (!TrackLoader.CheckValidFilename(txt + tb.Text))
                {
                    sender.SetToolTipText("Attempted to save with an invalid name");
                    return;
                }
                game.Track.Name = txt;
                try
                {
                    game.Track.Save(tb.Text, game.CurrentSong);
                }
                catch
                {
                    sender.SetToolTipText("An error occured trying to save");
                    return;
                }
            }
            window.Close();
        }
    }
}
