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
namespace linerider.Utils
{
    public class Line
    {
        public Vector2d Position;
        public Vector2d Position2;

        protected Line()
        {
        }
        public Line(Vector2d p1, Vector2d p2)
        {
            Position = p1;
            Position2 = p2;
        }
        public static Line FromAngle(Vector2d p1, Angle angle, double length)
        {
            Line ret;
            ret = new Line(
                p1,
                new Vector2d(
                    p1.X + (length * angle.Cos), 
                    p1.Y + (length * angle.Sin)));
            return ret;
        }
        public double GetLength()
        {
            return (Position2 - Position).Length;
        }
        public Vector2d GetVector()
        {
            return (Position2 - Position);
        }

        public static bool DoesLineIntersectRect(Line l1, DoubleRect rect)
        {
            Vector2d ps1 = l1.Position;
            Vector2d pe1 = l1.Position2;
            if (rect.Contains(ps1.X, ps1.Y) || rect.Contains(pe1.X, pe1.Y))
                return true;
            Vector2d tl = new Vector2d(rect.Left, rect.Top);
            Vector2d tr = tl;
            tr.X += rect.Width;
            Vector2d bl = tl;
            bl.Y += rect.Height;
            Vector2d br = bl;
            bl.X += rect.Width;
            return Intersects(ps1, pe1, tl, bl) ||
                Intersects(ps1, pe1, tl, tr) ||
                Intersects(ps1, pe1, tr, br) ||
                Intersects(ps1, pe1, bl, br);
        }
        public static bool Intersects(Vector2d a1, Vector2d a2, Vector2d b1, Vector2d b2)
        {
            Vector2d p;
            return Intersects(a1, a2, b1, b2, out p);
        }

        public static bool Intersects(Vector2d a1, Vector2d a2, Vector2d b1, Vector2d b2, out Vector2d intersection)
        {
            intersection = new Vector2d(0, 0);

            Vector2d b = a2 - a1;
            Vector2d d = b2 - b1;
            double bDotDPerp = b.X * d.Y - b.Y * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (Math.Abs(bDotDPerp) < double.Epsilon)
                return false;

            Vector2d c = b1 - a1;
            double t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return false;

            double u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return false;

            intersection = a1 + t * b;

            return true;
        }
    }
}