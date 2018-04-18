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
using System.Diagnostics;
using System.Drawing;
using Gwen;
using Gwen.Controls;
using linerider.Tools;

namespace linerider.UI
{
    public class RightInfoBar : WidgetContainer
    {
        private Editor _editor;
        private TrackLabel _fpslabel;
        private TrackLabel _playbackratelabel;
        private TrackLabel _riderspeedlabel;
        private Stopwatch _fpswatch = new Stopwatch();
        public RightInfoBar(ControlBase parent, Editor editor) : base(parent)
        {
            Dock = Dock.Right;
            _editor = editor;
            AutoSizeToContents = true;
            Setup();
            OnThink += Think;
        }
        private void Think(object sender, EventArgs e)
        {
            var rec = IO.TrackRecorder.Recording;
            _fpslabel.IsHidden = rec && !Settings.Recording.ShowFps;
            _riderspeedlabel.IsHidden = rec && !Settings.Recording.ShowPpf;
            _playbackratelabel.IsHidden = rec || _editor.Scheduler.UpdatesPerSecond == 40;
        }
        private void Setup()
        {
            _fpslabel = new TrackLabel(this)
            {
                Dock = Dock.Top,
                Alignment = Pos.Right | Pos.CenterV,
                TextRequest = (o, currenttext) =>
                {
                    var rec = IO.TrackRecorder.Recording;
                    if (rec && Settings.Recording.ShowFps)
                    {
                        return "60 FPS";
                    }
                    else if (!_fpswatch.IsRunning || _fpswatch.ElapsedMilliseconds > 500)
                    {
                        _fpswatch.Restart();
                        return Math.Round(_editor.FramerateCounter.FPS) + " FPS";
                    }
                    return currenttext;
                },
                Margin = new Margin(0, 0, 5, 0)
            };
            _riderspeedlabel = new TrackLabel(this)
            {
                Dock = Dock.Top,
                Alignment = Pos.Right | Pos.CenterV,
                TextRequest = (o, e) =>
                {
                    var ppf = _editor.RenderRider.CalculateMomentum().Length;
                    var n = (double)_riderspeedlabel.UserData;
                    var roundppf = Math.Round(ppf, 2);
                    if (n != ppf)
                    {
                        _riderspeedlabel.UserData = roundppf;
                        return string.Format("{0:N2}", Math.Round(ppf, 2)) + " P/f";
                    }
                    return e;
                },
                Margin = new Margin(0, 0, 5, 0),
                UserData = 0.0,
            };
            _playbackratelabel = new TrackLabel(this)
            {
                Dock = Dock.Top,
                Alignment = Pos.Right | Pos.CenterV,
                TextRequest = (o, e) =>
                {
                    var rate = Math.Round(_editor.Scheduler.UpdatesPerSecond / 40.0, 3);
                    string x = "";
                    if (rate.ToString() != "1")
                    {
                        x = $"{rate}x";
                    }
                    return x;
                },
                Margin = new Margin(0, 0, 5, 0),
            };
        }
    }
}