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
using linerider.Game;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace linerider.Lines
{
    public class StandardLine : GameLine
    {
        public const double Zone = 10;
        /// <summary>
        /// Extension direction
        /// </summary>
        public enum Ext
        {
            //imperative to trk format this does not exceed 2 bits
            None = 0,
            Left = 1,
            Right = 2,
            Both = 3
        }

        public Vector2d DiffNormal;
        public double ExtensionRatio;
        protected double DotScalar;
        public double Distance;
        public Ext Extension;
        public LineTrigger Trigger = null;
        public Vector2d Difference;
        public bool inv = false;

        protected double limit_left => Extension.HasFlag(Ext.Left) ? -ExtensionRatio : 0.0;
        protected double limit_right => Extension.HasFlag(Ext.Right) ? 1.0 + ExtensionRatio : 1.0;

        /// <summary>
        /// "Left" according to the inv field
        /// </summary>
        public override Vector2d Start
        {
            get { return inv ? Position2 : Position; }
        }
        /// <summary>
        /// "Right" according to the inv field
        /// </summary>
        public override Vector2d End
        {
            get { return inv ? Position : Position2; }
        }
        public override LineType Type
        {
            get
            {
                return LineType.Blue;
            }
        }
        public override System.Drawing.Color Color => Utils.Constants.BlueLineColor;

        protected StandardLine()
        {
        }
        public StandardLine(Vector2d p1, Vector2d p2, bool inv = false)
        {
            Position = p1;
            Position2 = p2;
            this.inv = inv;
            CalculateConstants();
            Extension = Ext.None;
        }

        /// <summary>
        /// Calculates the line constants, needs called if a point changes.
        /// </summary>
        public virtual void CalculateConstants()
        {
            Difference = Position2 - Position;
            var sqrDistance = Difference.LengthSquared;
            DotScalar = (1 / sqrDistance);
            Distance = Math.Sqrt(sqrDistance);

            DiffNormal = Difference * (1 / Distance);//normalize
            //flip to be the angle towards the top of the line
            DiffNormal = inv ? DiffNormal.PerpendicularRight : DiffNormal.PerpendicularLeft;
            ExtensionRatio = Math.Min(0.25, Zone / Distance);
        }
        public virtual bool Interact(ref SimulationPoint p)
        {
            if (Vector2d.Dot(p.Momentum, DiffNormal) > 0)
            {
                var startDelta = p.Location - this.Position;
                var disty = Vector2d.Dot(DiffNormal, startDelta);
                if (disty > 0 && disty < Zone)
                {
                    var distx = Vector2d.Dot(startDelta, Difference) * DotScalar;
                    if (distx <= limit_right && distx >= limit_left)
                    {
                        var pos = p.Location - disty * DiffNormal;
                        var prev = p.Previous;
                        if (p.Friction != 0)
                        {
                            var friction = DiffNormal.Yx * p.Friction * disty;
                            if (p.Previous.X >= pos.X)
                                friction.X = -friction.X;
                            if (p.Previous.Y >= pos.Y)
                                friction.Y = -friction.Y;
                            p = p.Replace(pos, p.Previous + friction);
                        }
                        else
                        {
                            p = p.Replace(pos);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        public override GameLine Clone()
        {
            return new StandardLine()
            {
                ID = ID,
                Difference = Difference,
                DiffNormal = DiffNormal,
                Distance = Distance,
                DotScalar = DotScalar,
                Extension = Extension,
                ExtensionRatio = ExtensionRatio,
                inv = inv,
                Position = Position,
                Position2 = Position2,
                Trigger = Trigger
            };
        }
    }
}
