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
using System.Diagnostics;

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
            Vector2d scalar = new Vector2d(Math.Cos(rad), Math.Sin(rad));
            var ret = start + (scalar * Vector2d.Dot(end - start, scalar));
            switch (a.Degrees)
            {
                case 270:
                case 90:
                    ret.X = start.X;
                    break;
                case 0:
                case 180:
                    ret.Y = start.Y;
                    break;
            }
            return ret;
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
        public static Vector2d Rotate(Vector2d point, Vector2d origin, Angle angle)
        {
            //translate to origin
            var left = point.X - origin.X;
            var top = point.Y - origin.Y;
            // apply rotation
            var rotatedX = left * angle.Cos - top * angle.Sin;
            var rotatedY = left * angle.Sin + top * angle.Cos;

            return new Vector2d(rotatedX + origin.X, rotatedY + origin.Y);
        }
        public static Vector2 Rotate(Vector2 point, Vector2 origin, Angle angle)
        {
            //translate to origin
            var left = point.X - origin.X;
            var top = point.Y - origin.Y;
            // apply rotation
            var rotatedX = left * angle.Cos - top * angle.Sin;
            var rotatedY = left * angle.Sin + top * angle.Cos;

            return new Vector2((float)rotatedX + origin.X, (float)rotatedY + origin.Y);
        }
        /// <summary>
        /// returns tl tr br bl of the rotated rectangle
        /// </summary>
        public static Vector2d[] RotateRect(DoubleRect rect, Vector2d origin, Angle angle)
        {
            Vector2d[] ret = new Vector2d[4];
            ret[0] = Rotate(new Vector2d(rect.Left, rect.Top), origin, angle);
            ret[1] = Rotate(new Vector2d(rect.Right, rect.Top), origin, angle);
            ret[2] = Rotate(new Vector2d(rect.Right, rect.Bottom), origin, angle);
            ret[3] = Rotate(new Vector2d(rect.Left, rect.Bottom), origin, angle);
            return ret;
        }
        /// <summary>
        /// returns tl tr br bl of the rotated rectangle
        /// </summary>
        public static Vector2[] RotateRect(FloatRect rect, Vector2 origin, Angle angle)
        {
            Vector2[] ret = new Vector2[4];
            ret[0] = Rotate(new Vector2(rect.Left, rect.Top), origin, angle);
            ret[1] = Rotate(new Vector2(rect.Right, rect.Top), origin, angle);
            ret[2] = Rotate(new Vector2(rect.Right, rect.Bottom), origin, angle);
            ret[3] = Rotate(new Vector2(rect.Left, rect.Bottom), origin, angle);

            return ret;
        }
        /// <summary>
        /// Returns tl, tr, br, bl of a line with [width] thickness.
        /// </summary>
        public static Vector2[] GetThickLine(Vector2 p, Vector2 p1, Angle angle, float width)
        {
            angle.Radians -= 1.5708f; //90 degrees
            var t = new Vector2(
                (float)(angle.Cos * (width / 2)),
                (float)(angle.Sin * (width / 2)));
            return new Vector2[] { p - t, p + t, p1 + t, p1 - t };
        }

        /// <summary>
        /// Returns tl, tr, br, bl of a line with [width] thickness.
        /// </summary>
        public static Vector2d[] GetThickLine(Vector2d p, Vector2d p1, Angle angle, double width)
        {
            angle.Radians -= 1.5708f; //90 degrees
            var t = new Vector2d(
                angle.Cos * (width / 2),
                angle.Sin * (width / 2));
            return new Vector2d[] { p - t, p + t, p1 + t, p1 - t };
        }
        /// <summary>
        /// Returns either p1 or p2 based on their distance from the input
        /// </summary>
        public static Vector2d CloserPoint(Vector2d input, Vector2d p1, Vector2d p2)
        {
            var a = Math.Abs((input - p1).LengthSquared);
            var b = Math.Abs((input - p2).LengthSquared);
            return a < b ? p1 : p2;
        }
        /// <summary>
        /// A lightwate random number generator without the need for init
        /// </summary>
        public static int fastrand(int seed)
        {
            seed = (214013 * seed + 2531011);
            return (seed >> 16) & 0x7FFF;
        }
        public static double LengthFast(Vector2d vector)
        {
            return 1.0 / MathHelper.InverseSqrtFast(vector.LengthSquared);
        }
        public static double leftness(Vector2d[] rect, int a, int b, ref Vector2d point)
        {
            return ((rect[b].X - rect[a].X) * (point.Y - rect[a].Y) - (rect[b].Y - rect[a].Y) * (point.X - rect[a].X));
        }
        public static bool isLeft(Vector2d[] rect, int a, int b, ref Vector2d point)
        {
            return ((rect[b].X - rect[a].X) * (point.Y - rect[a].Y) - (rect[b].Y - rect[a].Y) * (point.X - rect[a].X)) > 0;
        }
        public static bool isLeft(Vector2 a, Vector2 b, Vector2 point)
        {
            return ((b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X)) > 0;
        }
        public static bool PointInRectangle(Vector2d tl, Vector2d tr, Vector2d br, Vector2d bl, Vector2d p)
        {
            return PointInRectangle(new Vector2d[] { tl, tr, br, bl }, p);
        }
        public static bool PointInRectangle(Vector2d[] rect, Vector2d p)
        {
            return !(isLeft(rect, 0, 3, ref p) ||
            isLeft(rect, 3, 2, ref p) ||
            isLeft(rect, 2, 1, ref p) ||
            isLeft(rect, 1, 0, ref p));
            // return !(isLeft(tl, bl, p) || isLeft(bl, br, p) || isLeft(br, tr, p) || isLeft(tr, tl, p));
        }
        /// <summary>
        /// Converts the color to a little endian rgba integer
        /// </summary>
        public static int ColorToRGBA_LE(Color color)
        {
            return (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }
        /// <summary>
        /// Converts the color to a little endian rgba integer
        /// </summary>
        public static int ColorToRGBA_LE(int rgb, byte alpha)
        {
            var color = Color.FromArgb(rgb);
            return (alpha << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }
        /// <summary>
        /// Converts the color to a little endian rgba integer
        /// </summary>
        public static int ChangeAlpha(int rgba, byte alpha)
        {
            rgba &= unchecked((int)0x00FFFFFF);
            return rgba | (alpha << 24);
        }
    }
}