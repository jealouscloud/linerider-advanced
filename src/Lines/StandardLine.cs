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
        public Vector2d Perpendicular;
        protected double invSqrDis;
        public double Distance;
        public ExtensionDirection Extension;
        protected double ExtensionRatio;
        protected double Limleft;
        protected double Limright;

        public bool inv = false;
        public StandardLine Next;
        public StandardLine Prev;

        /// <summary>
        /// Gets/sets the property Position and complies to the inv field.
        /// </summary>
        public Vector2d CompliantPosition
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
        public Vector2d CompliantPosition2
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
            switch (i)
            {

                case ExtensionDirection.None:
                    {
                        Limleft = 0;
                        Limright = 1;
                        break;
                    }
                case ExtensionDirection.Left:
                    {
                        Limleft = -ExtensionRatio;
                        Limright = 1;
                        break;
                    }
                case ExtensionDirection.Right:
                    {
                        Limleft = 0;
                        Limright = 1 + ExtensionRatio;
                        break;
                    }
                case ExtensionDirection.Both:
                    {
                        Limleft = -ExtensionRatio;
                        Limright = 1 + ExtensionRatio;
                        break;
                    }
            }
        }
        /// <summary>
        /// Calculates the line constants, needs called if a point changes.
        /// </summary>
        public override void CalculateConstants()
        {
            diff = Position2 - Position;
            var sqrDistance = diff.LengthSquared;
            invSqrDis = (1 / sqrDistance);
            Distance = Math.Sqrt(sqrDistance);
            var invDist = 1 / Distance;

            Perpendicular = diff * invDist;//normalize
            Perpendicular = inv ? Perpendicular.PerpendicularRight : Perpendicular.PerpendicularLeft;
            ExtensionRatio = Math.Min(0.25, Zone / Distance);
        }
        public override bool Interact(DynamicObject obj)
        {
            if (!((obj.Momentum.X * Perpendicular.X) + (obj.Momentum.Y * Perpendicular.Y) > 0)) return false;

            var fp = Position;
            var diffx = obj.Position.X - fp.X;
            var diffy = obj.Position.Y - fp.Y;
            var cmpy = Perpendicular.X * diffx + Perpendicular.Y * diffy;
            if (!(cmpy > 0) || !(cmpy < Zone)) return false;

            var cmpx = (diffx * diff.X + diffy * diff.Y) * invSqrDis;

            if (!(cmpx >= Limleft) || !(cmpx <= Limright)) return false;

            obj.Position = new Vector2d(obj.Position.X - (cmpy * Perpendicular.X),
                obj.Position.Y - (cmpy * Perpendicular.Y));

            obj.Prev.X = obj.Prev.X +
                         Perpendicular.Y * obj.Friction * cmpy * (obj.Prev.X < obj.Position.X ? 1 : -1);
            obj.Prev.Y = obj.Prev.Y -
                         Perpendicular.X * obj.Friction * cmpy * (obj.Prev.Y < obj.Position.Y ? -1 : 1);
            if (Trigger != null)
            {
                var track = game.Track;
                if (track.Playing && !track.ActiveTriggers.Contains(Trigger))
                {
                    track.ActiveTriggers.Add(Trigger);
                }
            }
            return true;
        }
    }

}
