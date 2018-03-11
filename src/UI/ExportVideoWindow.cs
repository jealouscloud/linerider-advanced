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
using linerider.Drawing;
namespace linerider.UI
{
    static class ExportVideoWindow
    {
        public static void Create(MainWindow game)
        {
            string howto = "You are about to export your track as a video file. Make sure the end of the track is marked by a flag. " +
            "It will be located in your line rider user directory (Documents/LRA).\r\n" +
            "Please allow some minutes depending on your computer speed. " +
            "The window will become unresponsive during this time.\n\n" +
            "After recording, a console window will open to encode the video. " +
            "Closing it will cancel the process and all progress will be lost.";

            if (!SafeFrameBuffer.CanRecord)
            {
                howto = "Video export is not supported on this machine.\n\nSorry.";
            }
            var popup = PopupWindow.Create(howto, "Export Video", true, true);
            popup.Width = 350;

            popup.Container.Height += 50;
            var btn = popup.Container.FindChildByName("Okay");
            btn.Margin = new Margin(btn.Margin.Left, btn.Margin.Top + (Settings.Local.EnableSong ? 70 : 50), btn.Margin.Right, btn.Margin.Bottom);
            btn = popup.Container.FindChildByName("Cancel");
            btn.Margin = new Margin(btn.Margin.Left, btn.Margin.Top + (Settings.Local.EnableSong ? 70 : 50), btn.Margin.Right, btn.Margin.Bottom);
            popup.Layout();
            var radio = new RadioButtonGroup(popup.Container);
            radio.Name = "qualityselector";
            radio.AddOption("720p").Select();
            radio.AddOption("1080p");
            if (!SafeFrameBuffer.CanRecord)
            {
                radio.IsHidden = true;
            }
            LabeledCheckBox smooth = new LabeledCheckBox(popup.Container);
            smooth.Name = "smooth";
            smooth.IsChecked = true;
            smooth.Text = "Smooth Playback";
            Align.AlignBottom(smooth);

            LabeledCheckBox music = new LabeledCheckBox(popup.Container);
            music.Name = "music";
            music.IsChecked = Settings.Local.EnableSong;
            music.IsHidden = !Settings.Local.EnableSong;
            music.Text = "Include Music";
            if (Settings.Local.EnableSong)
            {
                popup.Container.Height += 20;
                Align.AlignBottom(music);
            }
            popup.Layout();

            popup.SetPosition((game.RenderSize.Width / 2) - (popup.Width / 2), (game.RenderSize.Height / 2) - (popup.Height / 2));

            popup.Dismissed += (o, e) =>
            {
                if (popup.Result == System.Windows.Forms.DialogResult.OK)
                {
                    if (game.Track.GetFlag() == null)
                    {
                        var pop = PopupWindow.Create(
                            "No flag detected, place one at the end of the track so the recorder knows where to stop.",
                            "Error", true, false);
                    }
                    else
                    {
                        var radiogrp = radio;
                        bool is1080p = radiogrp.Selected.Text == "1080p";
                        IO.TrackRecorder.RecordTrack(game, is1080p, smooth.IsChecked, music.IsChecked);
                    }
                }
            };
        }
    }
}
