//
//  Tool.cs
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
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using linerider.Utils;
using linerider.Rendering;

namespace linerider
{
    public static class Utility
    {

        public static Vector2d SnapToDegrees(Vector2d start, Vector2d end)
        {
            var degrees = (start - end).Length > 1 ? 15 : 45;
            return SnapToDegrees(start, end, degrees);
        }
        public static Vector2d SnapToDegrees(Vector2d start, Vector2d end, double degrees = 15)
        {
            var angle = Math.Round(Angle.FromLine(start, end).Degrees / degrees) * degrees;
            return AngleLock(start, end, Angle.FromDegrees(angle));
        }
        public static Vector2d AngleLock(Vector2d start, Vector2d end, Angle a)
        {
            var rad = a.Radians;
            Vector2d scalar = new Vector2d(Math.Cos(rad),Math.Sin(rad));
            return start + (scalar * Vector2d.Dot(end - start,scalar));
        }

        public static Vector2d LengthLock(Vector2d start, Vector2d end, double length)
        {
            var diff = end - start;
            if (diff.Length != length)
            {
                var angle = Math.Atan2(diff.Y, diff.X);
                Turtle turtle = new Turtle(start);
                turtle.Move(Angle.FromRadians(angle).Degrees, length);
                return turtle.Point;
            }
            return end;
        }
        /// <summary>
        /// Returns either p1 or p2 based on their distance from the input
        /// </summary>
        public static Vector2d CloserPoint(Vector2d input, Vector2d p1, Vector2d p2)
        {
            var a = Math.Abs(input.LengthSquared - p1.LengthSquared);
            var b = Math.Abs(input.LengthSquared - p2.LengthSquared);
            return a < b ? p1 : p2;
        }
        public static bool isLeft(Vector2d a, Vector2d b, Vector2d point)
        {
            return ((b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X)) > 0;
        }
        public static bool isLeft(Vector2 a, Vector2 b, Vector2 point)
        {
            return ((b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X)) > 0;
        }
        public static bool PointInRectangle(Vector2d tl, Vector2d tr, Vector2d br, Vector2d bl, Vector2d p)
        {
            return !(isLeft(tl, bl, p) || isLeft(bl, br, p) || isLeft(br, tr, p) || isLeft(tr, tl, p));
        }
        /// <summary>
        /// Converts the color to a little endian rgba integer
        /// </summary>
        public static int ColorToRGBA_LE(Color color)
        {
            return (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }
    }
}