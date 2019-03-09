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
using System.Drawing;
using Gwen;
using Gwen.Controls;
using linerider.Tools;

namespace linerider.UI
{
    public class Playhead : HorizontalSlider
    {
        private const int MinimumFrames = 40;
        private Editor _editor;
        private bool _wasdraggingoffset = false;
        private PlayheadMarker _flagmarker;
        private PlayheadMarker _endslider;
        private int _maxviewed = MinimumFrames;
        public int MaxViewed
        {
            get
            {
                return Math.Max(MinimumFrames, _maxviewed);
            }
            set
            {
                if (value != _maxviewed)
                {
                    _maxviewed = value;
                }
            }
        }
        private int FlagFrame
        {
            get
            {
                var flag = _editor.GetFlag();
                if (flag == null)
                    return -1;
                return flag.FrameID;
            }
        }
        public int DisplayMax
        {
            get
            {
                return _endslider.Frame;
            }
        }
        public Playhead(ControlBase parent, Editor editor) : base(parent)
        {
            Dock = Dock.Bottom;
            _editor = editor;
            SnapToNotches = true;
            DrawNotches = false;
            IsTabable = false;
            KeyboardInputEnabled = false;

            Setup();
        }
        public override void Think()
        {
            Min = 0;
            _maxviewed = Math.Max(_maxviewed, _editor.Offset);
            NotchCount = MaxViewed;
            Max = NotchCount;
            Value = _editor.Offset;
            if (!_flagmarker.IsHeld)
            {
                UpdateFlagMarker();
            }

            base.Think();
        }
        protected override void ProcessLayout(Size size)
        {
            _endslider.SetSize(15, Height);
            if (!_endslider.IsHeld)
            {
                ResetEndMarker();
            }
            UpdateFlagMarker();
            base.ProcessLayout(size);
        }
        private void Setup()
        {
            _flagmarker = new PlayheadMarker(this)
            {
                Cursor = Cursors.Hand,
                MouseInputEnabled = true,
            };
            _endslider = new PlayheadMarker(this)
            {
                Cursor = Cursors.SizeWE,
                MouseInputEnabled = true,
            };

            _flagmarker.IsHidden = true;
            _flagmarker.SetImage(GameResources.flagmarker);
            _endslider.SetImage(GameResources.playheadmarker);
            _endslider.SendToBack();

            _flagmarker.Margin = new Margin(5, 0, 0, 32 - 12);
            _endslider.Margin = new Margin(0, 0, 0, 0);
            _flagmarker.Dragged += (o, e) =>
            {
                if (FlagFrame != _flagmarker.Frame)
                    _editor.Flag(_flagmarker.Frame, false);
                UpdateFlagMarker();
            };
            _endslider.Released += OnEndMoved;
            ValueChanged += OnValueChanged;
        }
        private void OnEndMoved(object sender, EventArgs e)
        {
            MaxViewed = _endslider.Frame;
            if (_editor.Offset > MaxViewed)
            {
                _editor.SetFrame((int)MaxViewed, false);
                _editor.UpdateCamera();
                _editor.Scheduler.Reset();
                linerider.Audio.AudioService.EnsureSync();

                var flag = FlagFrame;
                if (flag != -1)
                {
                    _editor.Flag(MaxViewed, false);
                }
            }
            ResetEndMarker();
        }
        private void OnValueChanged(object sender, EventArgs e)
        {
            if (Held)
            {
                _editor.SetFrame((int)Value, false);
                _editor.UpdateCamera();
                _wasdraggingoffset = true;
            }
            else if (_wasdraggingoffset)
            {
                _wasdraggingoffset = false;
                _editor.Scheduler.Reset();
                linerider.Audio.AudioService.EnsureSync();
            }
        }
        private int PercentToX(double perc, ControlBase control)
        {
            var margin = control.Margin;
            var w = (double)Width - (margin.Width + control.Width);
            return (int)Math.Round(margin.Left + (perc * w));
        }
        private void ResetEndMarker()
        {
            var x = PercentToX(1, _endslider);
            _endslider.X = x;
        }
        private void UpdateFlagMarker()
        {
            var flagframe = FlagFrame;

            _flagmarker.IsHidden = flagframe == -1;
            if (flagframe != -1)
            {
                var perc = flagframe / Max;
                var x = PercentToX(perc, _flagmarker);

                _flagmarker.X = x;
            }
        }
    }
}