//
//  PopupWindow.cs
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
using Gwen.Controls;
using Gwen;
namespace linerider
{
    internal class PopupWindow
    {
        public static WindowControl Error(ControlBase parent, GLWindow game, string text, string title)
        {
            var wc = new WindowControl(parent, title, false);
            wc.MakeModal(true);
            wc.Width = 200;
            RichLabel l = new RichLabel(wc);
            //  Align.StretchHorizontally(l);
            l.Dock = Pos.Top;
            l.Width = wc.Width;
            l.AddText(text, parent.Skin.Colors.Label.Default, parent.Skin.DefaultFont);
            wc.Layout();
            l.SizeToChildren(false, true);
            wc.Height = 65 + l.Height;
            Align.CenterHorizontally(l);
                Button btn = new Button(wc);
                btn.Name = "Okay";
                btn.Text = "Okay";
                btn.Height = 20;
                btn.Y = l.Y + l.Height + 10;
                btn.Width = 100;
            btn.Clicked+= (o, e) => { ((WindowControl)o.Parent).Close();};
                Align.AlignLeft(l);
            wc.Show();
            wc.SetPosition((game.RenderSize.Width / 2) - (wc.Width / 2), (game.RenderSize.Height / 2) - (wc.Height / 2));
            wc.DisableResizing();
            return wc;
        }
        public static WindowControl Create(ControlBase parent, GLWindow game, string text, string title, bool ok, bool cancel)
        {
            var wc = new WindowControl(parent, title, false);
            wc.MakeModal(true);
            wc.Width = 200;
            RichLabel l = new RichLabel(wc);
          //  Align.StretchHorizontally(l);
          l.Dock = Pos.Top;
            l.Width = wc.Width;
            l.AddText(text, parent.Skin.Colors.Label.Default, parent.Skin.DefaultFont);
            wc.Layout();
            l.SizeToChildren(false, true);
            wc.Height = 65 + l.Height;
            Align.CenterHorizontally(l);
            if (ok)
            {
                Button btn = new Button(wc);
                btn.Name = "Okay";
                btn.Text = "Okay";
                btn.Height = 20;
                btn.Y = l.Y + l.Height + 10;
                btn.Width = 100;
                Align.AlignLeft(l);
                
            }
            if (cancel)
            {
                Button btn = new Button(wc);
                btn.Name = "Cancel";
                btn.Text = "Cancel";
                btn.SizeToContents();
                btn.Height = 20;
                btn.Width = 70;
                btn.Y = l.Y + l.Height + 10;
                btn.X = (wc.Width - 12) - btn.Width;
            }
            wc.Show();
            wc.SetPosition((game.RenderSize.Width / 2) - (wc.Width / 2), (game.RenderSize.Height / 2) - (wc.Height / 2));
            wc.DisableResizing();
            return wc;
        }
    }
}
