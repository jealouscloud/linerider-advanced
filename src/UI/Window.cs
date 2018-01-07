//
//  Window.cs
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

namespace linerider.UI
{
    abstract class Window : Gwen.Controls.WindowControl
    {
        public string Text { get; private set; }
        public bool Modal = false;
        private List<Label> labels = new List<Label>();
        public Window(Gwen.Controls.ControlBase ctrl, string title = "") : base(ctrl, title)
        {
        }
        public void SetText(string text)
        {
            Text = text.Replace("\r\n", "\n"); ;
            CreateText();
        }
        private void CreateText()
        {
            foreach (Label l in labels)
            {
                RemoveChild(l, true);
            }
            labels.Clear();
            if (Text == null)
                return;
            var font = Skin.DefaultFont;
            int maxwidth = this.m_InnerPanel.Width - (m_InnerPanel.Padding.Left + m_InnerPanel.Padding.Right);
            // ive written similar code to this over and over again and i'm so fucking tired of it
            // i rewrote fucking gwen's layout engine and cant actually commit it to master because
            // i can't afford the time it takes to maintain that project too
            string idontcareanymore = "";
            for (int i = 0; i < Text.Length; i++)
            {
                var idx = Text.IndexOfAny(new char[] { ' ', '\n' }, i);
                if (idx != -1)
                {
                    var substr = Text.Substring(i, idx - i);
                    var sz = Skin.Renderer.MeasureText(font, idontcareanymore + substr);
                    if (Text[idx] == '\n')
                    {
                        AddLine(idontcareanymore);
                        idontcareanymore = "";
                        i = idx;
                    }
                    else if (sz.X > maxwidth)
                    {
                        AddLine(idontcareanymore);
                        idontcareanymore = "";
                        if (Skin.Renderer.MeasureText(font, substr).X < maxwidth)//honestly i dont fucking care
                        {
                            i--;
                        }
                    }
                    else
                    {
                        idontcareanymore += substr + " ";
                        i = idx;//for loop will inc
                    }
                }
                else
                {
                    idontcareanymore += Text.Substring(i,Text.Length - i);
                    break;
                }
            }
            if (idontcareanymore.Length != 0)
            {
                AddLine(idontcareanymore);
            }
        }
        private void AddLine(string line)
        {
            Label add = new Label(this);
            add.Alignment = Pos.CenterH | Pos.Top;
            add.Text = line;
            add.Dock = Pos.Top;
            add.SizeToContents();
            int maxwidth = this.m_InnerPanel.Width - (m_InnerPanel.Padding.Left + m_InnerPanel.Padding.Right);
            add.AutoSizeToContents = false;
            add.Width = maxwidth;
            labels.Add(add);
        }
        protected override void OnBoundsChanged(System.Drawing.Rectangle oldBounds)
        {
            base.OnBoundsChanged(oldBounds);
        }
        protected override void Layout(Gwen.Skin.SkinBase skin)
        {
            base.Layout(skin);
            CreateText();
        }
    }
}
