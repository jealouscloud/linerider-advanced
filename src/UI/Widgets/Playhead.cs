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
        private Editor _editor;
        private bool _wasdragging = false;
        public Playhead(ControlBase parent, Editor editor) : base(parent)
        {
            Dock = Dock.Bottom;
            _editor = editor;
            SnapToNotches = true;
            DrawNotches = false;
            IsTabable = false;
            KeyboardInputEnabled=false;
            
            Setup();
        }
        private void Setup()
        {
            ValueChanged += (o, e) =>
            {
                if (Held)
                {
                    _editor.SetFrame((int)Value, false);
                    _editor.UpdateCamera();
                    _wasdragging = true;
                }
                else if (_wasdragging)
                {
                    _wasdragging = false;
                    _editor.Scheduler.Reset();
                    linerider.Audio.AudioService.EnsureSync();
                }
            };
        }
        public override void Think()
        {
            Min = 0;
            var max = Math.Max(0, _editor.FrameCount - 1);
            Max = max;
            if (max > 0)
                NotchCount = max;
            Value = _editor.Offset;
            base.Think();
        }
    }
}