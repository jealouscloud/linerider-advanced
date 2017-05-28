//
//  RepelLine.cs
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
using OpenTK.Graphics.OpenGL;
namespace linerider
{
	
	public class RepelLine : DynamicLine
	{
		public RepelLine(DynamicObject P1, DynamicObject P2) : base(P1,P2)
        {
            rl = (Position - Position2).Length * 0.5;
		}
		public override void satisfyDistance (Rider rider)
		{
			var d = Position - Position2;
            double dista = d.Length;
            if (dista < restLength) 
            {
                double res = (dista - restLength) / dista * 0.5;
                if (dista == 0)
                    res = 0;
                d.X = (d.X * res);
                d.Y = (d.Y * res);
				Position -= d;
				Position2 += d;
			}
		}
        public override bool Interact (DynamicObject obj)
		{
            return false;
		}
	}
	
}
