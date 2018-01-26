//
//  RedLine.cs
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
using OpenTK;
using linerider.Game;
namespace linerider.Lines
{
    public class InteractionTestLine : StandardLine
    {
        public class LineInteractionException : Exception
        {
        }
        public InteractionTestLine(Vector2d p1, Vector2d p2, bool inv = false) : base(p1, p2, inv) { }
        public override SimulationPoint Interact(SimulationPoint p)
        {
            if (Vector2d.Dot(p.Momentum, DiffNormal) > 0)
            {
                var startDelta = p.Location - this.Position;
                var doty = Vector2d.Dot(DiffNormal, startDelta);
                if (doty > 0 && doty < Zone)
                {
                    var dotx = Vector2d.Dot(startDelta, diff) * DotScalar;
                    if (dotx <= limit_right && dotx >= limit_left)
                    {
                        throw new LineInteractionException();
                    }
                }
            }
            return p;
        }
    }
}