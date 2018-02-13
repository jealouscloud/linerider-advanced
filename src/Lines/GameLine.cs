//
//  Line.cs
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

using OpenTK;
using System;
using linerider.Game;
using linerider.Utils;
using System.Drawing;
namespace linerider.Lines
{
    public abstract class GameLine : Line
    {
        public const int UninitializedID = int.MinValue;
        public int ID = UninitializedID;
        public float Width = 1;
        public virtual LineType Type
        {
            get
            {
                throw new Exception("Line has no defined type");
            }
        }


        public override string ToString()
        {
            return "ID: " + ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public Color GetColor()
        {
            switch (Type)
            {
                case LineType.Blue:
                    return Constants.BlueLineColor;
                case LineType.Red:
                    return Constants.RedLineColor;
                case LineType.Scenery:
                    return Constants.SceneryLineColor;
                default:
                    throw new Exception("Unable to get the color for this line, its type is unknown");
            }
        }
        public virtual GameLine Clone()
        {
            throw new Exception(
                "Unable to clone this line as it has no defined type");
        }
    }
}