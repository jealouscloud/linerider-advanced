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
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using linerider.Utils;
using linerider.Drawing;

namespace linerider
{
    public static class Utility
    {

        public static Vector2d SnapToDegrees(Vector2d start, Vector2d end, double degrees = 15)
        {
            var angle = Math.Round(Angle.FromLine(start, end).Degrees / degrees) * degrees;
            return AngleLock(start, end, Angle.FromDegrees(angle));
        }
        public static Vector2d AngleLock(Vector2d start, Vector2d end, Angle a)
        {
            Turtle tort = new Turtle(start);
            tort.Move(a.Degrees, (end - start).Length);
            return tort.Point;
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
        public static bool PointInTriangle(Vector2d A, Vector2d B, Vector2d C, Vector2d P)
        {
            // Compute vectors        
            Vector2d v0 = C - A;
            Vector2d v1 = B - A;
            Vector2d v2 = P - A;

            // Compute dot products
            double dot00 = Vector2d.Dot(v0, v0);
            double dot01 = Vector2d.Dot(v0, v1);
            double dot02 = Vector2d.Dot(v0, v2);
            double dot11 = Vector2d.Dot(v1, v1);
            double dot12 = Vector2d.Dot(v1, v2);

            // Compute barycentric coordinates
            double invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            if (u >= 0 && v >= 0 && (u + v) < 1)
            { return true; }
            else { return false; }
        }
        //https://gamedev.stackexchange.com/questions/110229/how-do-i-efficiently-check-if-a-point-is-inside-a-rotated-rectangle
        public static bool PointInRectangle(Vector2d tl, Vector2d tr, Vector2d br, Vector2d bl, Vector2d p)
        {
            return (PointInTriangle(tl, tr, bl, p) || PointInTriangle(tr, bl, br, p));
        }
    }
}