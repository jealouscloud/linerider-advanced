//
//  StandardLine.cs
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
using linerider.Lines;
using linerider.Game;
namespace linerider
{
    public class StandardLine : Line
    {
        public enum ExtensionDirection
        {
            //imperative to trk format this does not exceed 2 bits
            None = 0,
            Left = 1,
            Right = 2,
            Both = 3
        }

        public LineTrigger Trigger = null;
        public const double Zone = 10;
        public Vector2d DiffNormal;
        protected double DotScalar;
        public double Distance;
        public ExtensionDirection Extension;
        protected double ExtensionRatio;
        protected double limit_left => Extension.HasFlag(ExtensionDirection.Left) ? -ExtensionRatio : 0.0;
        protected double limit_right => Extension.HasFlag(ExtensionDirection.Right) ? 1.0 + ExtensionRatio : 1.0;

        public bool inv = false;
        public StandardLine Next;
        public StandardLine Prev;

        /// <summary>
        /// Gets/sets the property Position and complies to the inv field.
        /// </summary>
        public Vector2d Start
        {
            get { return inv ? Position2 : Position; }
            set
            {
                if (inv)
                    Position2 = value;
                else
                    Position = value;
            }
        }
        /// <summary>
        /// Gets/sets the property Position2 and complies to the inv field.
        /// </summary>
        public Vector2d End
        {
            get { return inv ? Position : Position2; }
            set
            {
                if (inv)
                    Position = value;
                else
                    Position2 = value;
            }
        }

        public StandardLine(Vector2d p1, Vector2d p2, bool inv = false) : base(p1, p2)
        {
            this.inv = inv;
            CalculateConstants();
            SetExtension(0);
        }

        public void SetExtension(int lim)
        {
            SetExtension((ExtensionDirection)lim);
        }

        public void RemoveExtension(ExtensionDirection i)
        {
            switch (Extension)
            {
                case ExtensionDirection.Left:
                    if (i == ExtensionDirection.Left)
                        SetExtension(ExtensionDirection.None);
                    break;
                case ExtensionDirection.Right:
                    if (i == ExtensionDirection.Right)
                        SetExtension(ExtensionDirection.None);
                    break;
                case ExtensionDirection.Both:
                    if (i == ExtensionDirection.Left)
                        SetExtension(ExtensionDirection.Right);
                    else if (i == ExtensionDirection.Right)
                        SetExtension(ExtensionDirection.Left);
                    else
                        SetExtension(ExtensionDirection.None);
                    break;
            }
        }

        public void AddExtension(ExtensionDirection i)
        {
            switch (Extension)
            {
                case ExtensionDirection.Left:
                    if (i == ExtensionDirection.Right)
                        i = ExtensionDirection.Both;
                    break;
                case ExtensionDirection.Right:
                    if (i == ExtensionDirection.Left)
                        i = ExtensionDirection.Both;
                    break;
            }
            SetExtension(i);
        }

        public void SetExtension(ExtensionDirection i)
        {
            Extension = i;
        }
        /// <summary>
        /// Calculates the line constants, needs called if a point changes.
        /// </summary>
        public override void CalculateConstants()
        {
            diff = Position2 - Position;
            var sqrDistance = diff.LengthSquared;
            DotScalar = (1 / sqrDistance);
            Distance = Math.Sqrt(sqrDistance);

            DiffNormal = diff * (1 / Distance);//normalize
            //flip to be the angle towards the top of the line
            DiffNormal = inv ? DiffNormal.PerpendicularRight : DiffNormal.PerpendicularLeft;
            ExtensionRatio = Math.Min(0.25, Zone / Distance);
        }
        public override bool Interact(ref SimulationPoint p)
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
                        var pos = p.Location - doty * DiffNormal;
                        var friction = DiffNormal.Yx * p.Friction * doty;
                        if (p.Previous.X >= pos.X)
                            friction.X = -friction.X;
                        if (p.Previous.Y >= pos.Y)
                            friction.Y = -friction.Y;
                        p = new SimulationPoint(pos, p.Previous + friction, p.Momentum, p.Friction);
                        return true;
                    }
                }
            }
            return false;
        }
        public override LineState GetState()
        {
            return new LineState() { Pos1 = Position, Pos2 = Position2, extension = Extension, Next = Next, Prev = Prev, Parent = this, Inverted = inv, Exists = ID >= 0 };
        }
    }

}
