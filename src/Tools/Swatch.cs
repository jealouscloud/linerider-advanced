//
//  Tool.cs
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
using OpenTK;
using linerider.Utils;
using linerider.Game;

namespace linerider.Tools
{
    public class Swatch : GameService
    {
        private float _greenmultiplier = 1;
        private float _redmultiplier = 1;
        public float GreenMultiplier
        {
            get
            {
                return _greenmultiplier;
            }
            set
            {
                _greenmultiplier = value;
            }
        }

        public int RedMultiplier
        {
            get
            {
                return (int)Math.Round(_redmultiplier);
            }
            set
            {
                _redmultiplier = value;
            }
        }
        public const int MaxRedMultiplier = 3;
        public const int MinRedMultiplier = 1;
        public const int MaxGreenMultiplier = 3;
        public const float MinGreenMultiplier = 0.5f;
        public LineType Selected { get; set; } = LineType.Blue;
        public void IncrementSelectedMultiplier()
        {
            if (CurrentTools.SelectedTool != CurrentTools.EraserTool &&
            CurrentTools.SelectedTool.ShowSwatch)
            {
                var sw = CurrentTools.SelectedTool.Swatch;
                switch (Selected)
                {
                    case LineType.Red:
                        {
                            var mul = sw.RedMultiplier;
                            mul++;
                            if (mul > Swatch.MaxRedMultiplier)
                                mul = Swatch.MinRedMultiplier;
                            sw.RedMultiplier = mul;
                        }
                        break;
                    case LineType.Scenery:
                        {
                            var mul = sw.GreenMultiplier;
                            mul++;
                            mul = (float)Math.Floor(mul);
                            if (mul > Swatch.MaxGreenMultiplier)
                                mul = Swatch.MinGreenMultiplier;
                            sw.GreenMultiplier = mul;
                        }
                        break;
                }
            }
        }
    }
}