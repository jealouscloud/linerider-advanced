//
//  DynamicLine.cs
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
	
	public class DynamicLine : Line
	{
		protected DynamicObject p1;
		protected DynamicObject p2;
		public override Vector2d Position {
			get {
				return p1.Position;
			}
			set {
				p1.Position = value;
			}
		}
		public override Vector2d Position2 {
			get {
				return p2.Position;
			}
			set {
				p2.Position = value;
			}
        }
        protected double rl=-1;
        public virtual double restLength
        {
            get
            {
                return rl;
            }
            set
            {
                rl = value;
            }
        }
        public DynamicLine(DynamicObject P1, DynamicObject P2)
		{
			p1 = P1;
            p2 = P2;
            rl = (Position - Position2).Length;//rest length
        }
        public virtual void satisfyDistance(Rider rider)
        {
            var d = Position - Position2;
            var dista = d.Length;
            var res = (dista - restLength) / dista * 0.500000;
            if (dista == 0)
                res = 0;
            d.X = (d.X * res);
            d.Y = (d.Y * res);
            Position -= d;
            Position2 += d;
        }
	}
	
}
