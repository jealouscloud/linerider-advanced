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
using linerider.UI;
namespace linerider
{
    internal class PopupWindow : GameService
    {
        public static Window Error(string text, string title = "Error!")
        {
            return Create(text, title, true, false);
        }
        public static Window Create(string text, string title, bool ok, bool cancel)
        {
            var wc = new Window(game.Canvas, title);
            wc.MakeModal(true);
            wc.Width = 300;
            wc.Layout();

            wc.SetText(text);
            wc.Layout();
            if (cancel)
            {
                Button btn = new Button(wc.Container);
                btn.Margin = new Margin(1, 1, 5, 1);
                btn.Dock = Pos.Right;
                btn.Name = "Cancel";
                btn.Text = "Cancel";
                btn.Width = 70;
                btn.Clicked += (o, e) =>
                {
                    wc.Close();
                    wc.Result = System.Windows.Forms.DialogResult.Cancel;
                    if (wc.Dismissed != null)
                        wc.Dismissed.Invoke(o, e);
                };
            }
            if (ok)
            {
                Button btn = new Button(wc.Container);
                btn.Margin = new Margin(1, 1, 5, 1);
                btn.Dock = Pos.Right;
                btn.Name = "Okay";
                btn.Text = "Okay";
                btn.Width = 70;
                btn.Clicked += (o, e) =>
                {
                    wc.Close();
                    wc.Result = System.Windows.Forms.DialogResult.OK;
                    if (wc.Dismissed != null)
                        wc.Dismissed.Invoke(o, e);
                };
            }
            wc.Show();
            wc.SetPosition((game.RenderSize.Width / 2) - (wc.Width / 2), (game.RenderSize.Height / 2) - (wc.Height / 2));
            wc.DisableResizing();
            return wc;
        }
    }
}
