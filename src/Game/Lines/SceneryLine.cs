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

namespace linerider.Game
{
    public class SceneryLine : GameLine
    {
        public override LineType Type
        {
            get
            {
                return LineType.Scenery;
            }
        }
        public override System.Drawing.Color Color => Utils.Constants.SceneryLineColor;
        public SceneryLine(Vector2d p1, Vector2d p2) 
        {
            Position = p1;
            Position2 = p2;
        }
        public override GameLine Clone()
        {
            return new SceneryLine(Position, Position2)
            {
                ID = ID,
                Width = Width
            };
        }
    }
}