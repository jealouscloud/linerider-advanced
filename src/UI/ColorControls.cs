//
//  ColorControls.cs
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

using Gwen.Controls;
using OpenTK;
using System;
using System.Drawing;

namespace linerider.UI
{
    public class ColorControls
    {
        #region Properties

        public float GreenMultiplier
        {
            get
            {
                return _green.Multiplier;
            }
        }

        public int RedMultiplier
        {
            get
            {
                return (int)Math.Round(_red.Multiplier);
            }
        }

        public LineType Selected
        {
            get
            {
                return eraser ? _eraserselected : _selected;
            }
            set
            {
                if (eraser)
                    _eraserselected = value;
                else
                    _selected = value;

                _green.Alpha = value == LineType.Scenery ? (byte)64 : (byte)255;
                _red.Alpha = value == LineType.Red ? (byte)64 : (byte)255;
                _blue.Alpha = value == LineType.Blue ? (byte)64 : (byte)255;
            }
        }

        #endregion Properties

        #region Constructors

        public ColorControls(ControlBase parent, Vector2 start)
        {
            _blue = new ColorButton(parent);
            _red = new MultiplierButton(parent);
            _green = new MultiplierButton(parent);
            _green.min = 0.5f;
            _blue.Color = Color.FromArgb(0, 102, 255);
            _red.Color = Color.FromArgb(204, 0, 0);
            _green.Color = Color.FromArgb(42, 209, 0);
            _blue.SetSize(32, 16);
            _red.SetSize(32, 16);
            _green.SetSize(32, 16);
            SetStart(start);
            _blue.Clicked += blue_Clicked;
            _red.Clicked += red_Clicked;
            _green.Clicked += green_Clicked;
        }

        #endregion Constructors

        #region Methods

        public bool GetVisible()
        {
            return !_green.IsHidden;//we assume all are always either viisble or invisible
        }

        public void OnTabButtonPressed()
        {
            switch (Selected)
            {
                case LineType.Scenery:
                    _green.TickMultiplier();
                    break;

                case LineType.Red:
                    _red.TickMultiplier();
                    break;
            }
        }

        public void SetEraser(bool enabled)
        {
            eraser = enabled;
            _green.eraser = enabled;
            _red.eraser = enabled;
            Selected = Selected;
        }

        public void SetStart(Vector2 start)
        {
            _blue.SetPosition(start.X, start.Y);
            _red.SetPosition(_blue.X + 35, start.Y);
            _green.SetPosition(_red.X + 35, start.Y);
        }

        public void SetVisible(bool val)
        {
            _green.IsHidden = !val;
            _red.IsHidden = !val;
            _blue.IsHidden = !val;
        }

        #endregion Methods

        #region Classes

        public class ColorButton : Button
        {
            #region Fields

            public byte Alpha = 255;
            public Color Color = Color.Black;

            #endregion Fields

            #region Constructors

            public ColorButton(ControlBase canvas)
                : base(canvas)
            {
            }

            #endregion Constructors

            #region Methods

            protected override void Render(Gwen.Skin.SkinBase skin)
            {
                skin.Renderer.DrawColor = Color.FromArgb(IsDepressed ? 64 : (IsHovered ? 128 : Alpha), Color.R, Color.G, Color.B);
                skin.Renderer.DrawFilledRect(RenderBounds);
            }

            #endregion Methods
        }

        public class MultiplierButton : ColorButton
        {
            #region Fields

            public bool eraser = false;
            public float max = 3;
            public float min = 1;
            public float Multiplier = 1;

            #endregion Fields

            #region Constructors

            public MultiplierButton(ControlBase canvas)
                : base(canvas)
            {
            }

            #endregion Constructors

            #region Methods

            public void TickMultiplier()
            {
                Multiplier++;
                Multiplier = (float)Math.Floor(Multiplier);
                if (Multiplier > max)
                    Multiplier = min;
                GetCanvas().Redraw();
            }

            protected override void OnMouseClickedRight(int x, int y, bool down)
            {
                base.OnMouseClickedRight(x, y, down);
                if (!down && !eraser)
                {
                    TickMultiplier();
                }
            }

            protected override void Render(Gwen.Skin.SkinBase skin)
            {
                base.Render(skin);
                if (Alpha == 64)
                {
                    skin.Renderer.DrawColor = Color.Black;
                    if (!eraser)
                    {
                        skin.Renderer.RenderText(Font, new Point(0, 0), Multiplier + "x");
                    }
                }
            }

            #endregion Methods
        }

        #endregion Classes

        #region Fields

        private readonly ColorButton _blue;
        private readonly MultiplierButton _green;
        private readonly MultiplierButton _red;
        private LineType _eraserselected = LineType.All;
        private LineType _selected = LineType.All;
        private bool eraser = false;

        #endregion Fields

        private void blue_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            Selected = LineType.Blue;
        }

        private void green_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            Selected = LineType.Scenery;
        }

        private void red_Clicked(ControlBase sender, ClickedEventArgs arguments)
        {
            Selected = LineType.Red;
        }
    }
}