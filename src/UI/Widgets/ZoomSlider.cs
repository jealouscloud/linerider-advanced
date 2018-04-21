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
using linerider.Utils;
using OpenTK;

namespace linerider.UI
{
    public class ZoomSlider : VerticalSlider
    {
        private Editor _editor;
        private Tooltip _tooltip;
        public ZoomSlider(ControlBase parent, Editor editor) : base(parent)
        {
            TooltipDelay = 0;
            IsTabable = false;
            KeyboardInputEnabled = false;
            _editor = editor;
            Height = 125;
            Width = 30;
            _tooltip = new Tooltip(parent.GetCanvas());
            _tooltip.IsHidden = true;
            Positioner = (o) =>
            {
                return new System.Drawing.Point(Parent.Width - Width, (Parent.Height - Height) - 50);
            };
            SetRange(Constants.MinimumZoom, Constants.MaxZoom);
            ValueChanged += (o, e) =>
            {
                if (Held)
                {
                    var val = Value;
                    _editor.Zoom = (float)MathHelper.Clamp(Value, Constants.MinimumZoom, Settings.Local.MaxZoom);
                }
                UpdateTooltip();
            };
            m_SliderBar.HoverEnter += (o, e) =>
            {
                _tooltip.IsHidden = false;
                UpdateTooltip();
            };
            HoverEnter += (o, e) =>
            {
                _tooltip.IsHidden = false;
                UpdateTooltip();
            };
            m_SliderBar.HoverLeave += (o, e) =>
            {
                _tooltip.IsHidden = true;
            };
            HoverLeave += (o, e) =>
            {
                _tooltip.IsHidden = true;
            };
            m_SliderBar.Pressed += (o, e) =>
            {
                _tooltip.IsHidden = false;
                UpdateTooltip();
            };
            Value = editor.Zoom;
            OnThink += (o,e)=>
            {
                Value = editor.Zoom;
            };
        }
        public void UpdateTooltip()
        {
            if (!_tooltip.IsHidden)
            {
                var loc = LocalPosToCanvas(new Point(0, m_SliderBar.Y));
                _tooltip.Text = Math.Round(_editor.Zoom, 2) + "x"; ;
                _tooltip.Layout();
                _tooltip.SetPosition(loc.X - _tooltip.Width, loc.Y);
            }
        }
    }
}