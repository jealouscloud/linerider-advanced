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
    public class Window : Gwen.Controls.WindowControl
    {
        public EventHandler<EventArgs> Dismissed;
        public ControlBase Container;
        public System.Windows.Forms.DialogResult Result { get; set; }
        public string Text { get; private set; }
        public bool Modal = false;
        private List<Label> labels = new List<Label>();
        public Window(Gwen.Controls.ControlBase ctrl, string title = "") : base(ctrl, title)
        {
            Container = new ControlBase(m_InnerPanel);
            Container.Margin = new Margin(0, 20, 0, 10);
            Container.Dock = Pos.Top;
            Container.Height = 25;
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
                m_InnerPanel.RemoveChild(l, true);
            }
            labels.Clear();
            if (Text == null)
                return;
            var font = Skin.DefaultFont;
            int maxwidth = this.m_InnerPanel.Width - (m_InnerPanel.Padding.Left + m_InnerPanel.Padding.Right);
            // ive written similar code to this over and over again and i'm so fucking tired of it
            // i rewrote fucking gwen's layout engine and cant actually commit it to master because
            // i can't afford the time it takes to maintain that project too
            var f = (Gwen.Renderer.BitmapFont)font;
            var wrapped = f.fontdata.WordWrap(Text,maxwidth);
            foreach(var line in wrapped)
            {
                AddLine(line);
            }
            Container.BringToFront();
            m_InnerPanel.Layout();
            m_InnerPanel.SizeToChildren(false, true);
            SizeToChildren(false, true);
        }
        /// Breaks the given string and returns the remaining characters
        private int BreakString(string bigtext, int maxwidth)
        {
            string longword = "";
            for (int i = 0; i < bigtext.Length; i++)
            {
                if (Skin.Renderer.MeasureText(Skin.DefaultFont, longword + bigtext[i]).X > maxwidth)
                {
                    AddLine(longword);
                    longword = "";
                }
                else
                {
                    longword += bigtext[i];
                }
            }
            return longword.Length;
        }
        private void AddLine(string line)
        {
            Label add = new Label(m_InnerPanel);
            add.Alignment = Pos.CenterH | Pos.Top;
            add.Dock = Pos.Top;
            add.AutoSizeToContents = false;
            add.Text = line;
            var measure = Skin.Renderer.MeasureText(add.Font, "|");
            int maxwidth = this.m_InnerPanel.Width - (m_InnerPanel.Padding.Left + m_InnerPanel.Padding.Right);
            add.SetSize(maxwidth, measure.Y);
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
