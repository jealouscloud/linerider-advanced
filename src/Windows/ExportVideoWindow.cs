//
//  ExportVideoWindow.cs
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
namespace linerider.Windows
{
    class ExportVideoWindow : Gwen.Controls.WindowControl
    {

        public ExportVideoWindow(Gwen.Controls.ControlBase parent, GLWindow game) : base(parent, "Export Video")
        {
            game.Track.Stop();
            var openwindows = game.Canvas.GetOpenWindows();
            foreach (var v in openwindows)
            {
                if (v is WindowControl)
                {
                    ((WindowControl)v).Close();
                }
            }
            MakeModal(true);
            Width = 400;
            Height = 280;
            ControlBase bottom = new ControlBase(this);
            bottom.Height = 150;
            bottom.Width = 400 - 13;
            Align.AlignBottom(bottom);

            var buttonok = new Button(bottom);

            buttonok.Name = "Okay";
            buttonok.Text = "Record";
            buttonok.Height = 20;
            buttonok.Y = 80;
            buttonok.Width = 100;
			if (!Drawing.SafeFrameBuffer.CanRecord)
			{
				buttonok.IsHidden = true;
			}
			buttonok.Clicked += (o, e) =>
            {
                var wnd = ((WindowControl)o.Parent.Parent);
                wnd.Close();
                if (game.Track.GetFlag() == null)
                {
                    var pop = PopupWindow.Create(parent, game,
                        "No flag detected, place one at the end of the track so the recorder knows where to stop.",
                        "Error", true, false);
                    pop.FindChildByName("Okay", true).Clicked +=
                        (o1, e1) => { pop.Close(); };
                }
                else
                {
                    var radiogrp = (RadioButtonGroup)this.FindChildByName("qualityselector",true);
                    bool is1080p = radiogrp.Selected.Text == "1080p";
                    TrackFiles.TrackRecorder.RecordTrack(game,is1080p,((LabeledCheckBox)FindChildByName("smooth",true)).IsChecked);
                }
            };
            Align.AlignLeft(buttonok);
            var buttoncancel = new Button(bottom);

            buttoncancel.Name = "Cancel";
            buttoncancel.Text = "Cancel";
            buttoncancel.Height = 20;
            buttoncancel.Y = 80;
            buttoncancel.Width = 100;
            buttoncancel.Clicked += (o, e) => { this.Close(); };
            Align.AlignRight(buttoncancel);
            var label = new RichLabel(this);
            label.Dock = Pos.Top;
            label.Width = this.Width;
			if (Drawing.SafeFrameBuffer.CanRecord)
			{
				label.AddText("You are about to export your track as a video file. Make sure the end of the track is marked by a flag. It will be located in the same folder as linerider.exe. Please allow some minutes depending on your computer speed. The window will become unresponsive during this time." + Environment.NewLine + Environment.NewLine + "After recording, a console window will open to encode the video. Closing it will cancel the process and all progress will be lost.", parent.Skin.Colors.Label.Default, parent.Skin.DefaultFont);
			}
			else
			{
                label.AddText("Video export is not supported on this machine." + Environment.NewLine + "Sorry.", parent.Skin.Colors.Label.Default, parent.Skin.DefaultFont);
			}
			label.SizeToChildren(false, true);
            var radio = new RadioButtonGroup(bottom);
            radio.Name = "qualityselector";
            radio.AddOption("720p").Select();
            radio.AddOption("1080p");
            Align.AlignLeft(radio);
            radio.Y += 20;
			if (!Drawing.SafeFrameBuffer.CanRecord)
			{
				radio.IsHidden = true;
			}
            LabeledCheckBox smooth = new LabeledCheckBox(bottom);
            smooth.Name = "smooth";
            smooth.IsChecked = true;
            smooth.Text = "Use Smooth Playback";
            Align.AlignLeft(smooth);
            smooth.Y += 5;
            DisableResizing();
        }
    }
}
