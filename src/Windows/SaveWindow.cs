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
        public SaveWindow(Gwen.Controls.ControlBase parent, GLWindow game) : base(parent, "Save Track")
        {
            game.Track.Stop();
            MakeModal(true);

            var bottom = new PropertyBase(this) { Name = "bottom", Margin = new Margin(0, 10, 0, 5), Height = 30 };
            var cb = new ComboBox(this);

            cb.ItemSelected += (o, e) => {
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
            this.Height = 100;
            var dir = Program.CurrentDirectory + "Tracks";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var folders = Directory.GetDirectories(Program.CurrentDirectory + "Tracks");
            cb.AddItem("<create new track>"); //< used as it cant be used as a file character
            cb.SelectByText("<create new track>");
            foreach (var folder in folders)
            {
                var trackname = Path.GetFileName(folder);
                cb.AddItem(trackname);
            }
            cb.SelectByText(game.Track.Name);
            game.Cursor = MouseCursor.Default;
        }
    }
}
