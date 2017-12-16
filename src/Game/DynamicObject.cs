//
//  DynamicObject.cs
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

    public class DynamicObject : GameObject
    {
        public Vector2d Momentum;
        public Vector2d Prev;
        public double Friction;
        public Vector2d Gravity = new Vector2d(0, 0.35 * 0.5);
        public DynamicObject(Vector2d pos, double friction)
        {
            Position = pos;
            Prev = new Vector2d(0, 0);
            Friction = friction;
        }
        public override void Tick()
        {
            Momentum = Position - Prev + Gravity;
            Prev = Position;
            Position += Momentum;
            base.Tick();
        }
        public virtual DynamicObject Clone()
        {
            return new DynamicObject(Position, Friction) { Prev = Prev, Gravity = Gravity, Momentum = Momentum };
        }
    }
    public class ScarfObject : DynamicObject
    {
        public ScarfObject(Vector2d pos, double friction) : base(pos, friction)
        {
        }
        public override void Tick()
        {
            Momentum = (Position - Prev) * Friction + Gravity;
            Prev = Position;
            Position += Momentum;
        }
        public override DynamicObject Clone()
        {
            return new ScarfObject(Position, Friction) { Prev = Prev, Gravity = Gravity, Momentum = Momentum };
        }
    }
    public class NewScarfObject : DynamicObject
    {
        public NewScarfObject(Vector2d pos, double friction) : base(pos, friction)
        {
        }
        public override void Tick()
        {
            Momentum = (Position - Prev) * Friction + (Gravity * 2);
            Prev = Position;
            Position += Momentum;
        }
        public override DynamicObject Clone()
        {
            return new ScarfObject(Position, Friction) { Prev = Prev, Gravity = Gravity, Momentum = Momentum };
        }
    }
}