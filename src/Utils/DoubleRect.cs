//
//  DoubleRect.cs
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
using OpenTK.Graphics;
namespace linerider.Utils
{
    public struct DoubleRect : IEquatable<DoubleRect>
    {
        public double Left;
        public double Top;
        public double Width;
        public double Height;
        public Vector2d Vector
        {
            get
            {
                return new Vector2d(Left, Top);
            }
        }
        public Vector2d Size
        {
            get
            {
                return new Vector2d(Width, Height);
            }
        }
        public double Right
        {
            get
            {
                return Left + Width;
            }
        }
        public double Bottom
        {
            get
            {
                return Top + Height;
            }
        }
        public DoubleRect(double left, double top, double width, double height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }
        public DoubleRect(Vector2d Position, Vector2d Size)
        {
            this.Left = Position.X;
            this.Top = Position.Y;
            this.Width = Size.X;
            this.Height = Size.Y;
        }
        public static DoubleRect FromLRTB(double left, double right, double top, double bottom)
        {
            return new DoubleRect(left, top, right - left, bottom - top);
        }

        public FloatRect ToFloatRect()
        {
            return new FloatRect((float)Left, (float)Top, (float)Width, (float)Height);
        }

        public Vector2d EllipseClamp(Vector2d position)
        {
            var center = Vector + (Size / 2);
            var xrad = Width / 2;
            var yrad = Height / 2;

            var diff = position - center;
            if ((((diff.X * diff.X) / (xrad * xrad)) + ((diff.Y * diff.Y) / (yrad * yrad))) > 1.0)
            {
                var m = Math.Atan2(diff.Y * xrad / yrad, diff.X);
                return new Vector2d(
                    center.X + xrad * Math.Cos(m),
                    center.Y + yrad * Math.Sin(m));
            }
            return position;
        }

        public Vector2d Clamp(Vector2d v)
        {
            if (!Contains(v.X, v.Y))
            {
                var l = Left;
                var t = Top;
                var r = l + Width;
                var b = t + Height;
                v.X = MathHelper.Clamp(v.X, l, r);
                v.Y = MathHelper.Clamp(v.Y, t, b);
            }
            return v;
        }

        public DoubleRect Inflate(double width, double height)
        {
            var rect = this;

            rect.Left -= width;
            rect.Top -= height;
            rect.Width += 2 * width;
            rect.Height += 2 * height;
            return rect;
        }
        public DoubleRect Scale(double scale)
        {
            var rect = this;

            var width = (Right - Left) * scale;
            rect.Left -= (width / 2) - (Width / 2);
            rect.Width = width;

            var height = (Bottom - Top) * scale;
            rect.Top -= (height / 2) - (Height / 2);
            rect.Height = height;
            return rect;
        }
        public DoubleRect Scale(double x, double y)
        {
            var rect = this;

            var width = (Right - Left) * x;
            rect.Left -= (width / 2) - (Width / 2);
            rect.Width = width;

            var height = (Bottom - Top) * y;
            rect.Top -= (height / 2) - (Height / 2);
            rect.Height = height;
            return rect;
        }

        public bool Contains(double x, double y)
        {
            double num = Math.Min(this.Left, this.Left + this.Width);
            double num2 = Math.Max(this.Left, this.Left + this.Width);
            double num3 = Math.Min(this.Top, this.Top + this.Height);
            double num4 = Math.Max(this.Top, this.Top + this.Height);
            return x >= num && x < num2 && y >= num3 && y < num4;
        }
        public bool Intersects(DoubleRect rect)
        {
            DoubleRect floatRect;
            return this.Intersects(rect, out floatRect);
        }
        public bool Intersects(DoubleRect rect, out DoubleRect overlap)
        {
            double val = Math.Min(this.Left, this.Left + this.Width);
            double val2 = Math.Max(this.Left, this.Left + this.Width);
            double val3 = Math.Min(this.Top, this.Top + this.Height);
            double val4 = Math.Max(this.Top, this.Top + this.Height);
            double val5 = Math.Min(rect.Left, rect.Left + rect.Width);
            double val6 = Math.Max(rect.Left, rect.Left + rect.Width);
            double val7 = Math.Min(rect.Top, rect.Top + rect.Height);
            double val8 = Math.Max(rect.Top, rect.Top + rect.Height);
            double num = Math.Max(val, val5);
            double num2 = Math.Max(val3, val7);
            double num3 = Math.Min(val2, val6);
            double num4 = Math.Min(val4, val8);
            if (num < num3 && num2 < num4)
            {
                overlap.Left = num;
                overlap.Top = num2;
                overlap.Width = num3 - num;
                overlap.Height = num4 - num2;
                return true;
            }
            overlap.Left = 0f;
            overlap.Top = 0f;
            overlap.Width = 0f;
            overlap.Height = 0f;
            return false;
        }
        public override string ToString()
        {
            return string.Concat(new object[]
                {
                    "[DoubleRect] Left(",
                    this.Left,
                    ") Top(",
                    this.Top,
                    ") Width(",
                    this.Width,
                    ") Height(",
                    this.Height,
                    ")"
                });
        }
        public override bool Equals(object obj)
        {
            return obj is FloatRect && obj.Equals(this);
        }
        public bool Equals(DoubleRect other)
        {
            return this.Left == other.Left && this.Top == other.Top && this.Width == other.Width && this.Height == other.Height;
        }
        public override int GetHashCode()
        {
            return (int)((uint)this.Left ^ ((uint)this.Top << 13 | (uint)this.Top >> 19) ^ ((uint)this.Width << 26 | (uint)this.Width >> 6) ^ ((uint)this.Height << 7 | (uint)this.Height >> 25));
        }
        public static bool operator ==(DoubleRect r1, DoubleRect r2)
        {
            return r1.Equals(r2);
        }
        public static bool operator !=(DoubleRect r1, DoubleRect r2)
        {
            return !r1.Equals(r2);
        }
        public static DoubleRect operator /(DoubleRect r1, double r2)
        {
            r1.Top /= r2;
            r1.Left /= r2;
            r1.Width /= r2;
            r1.Height /= r2;
            return r1;
        }
        public static DoubleRect operator *(DoubleRect r1, double r2)
        {
            r1.Top *= r2;
            r1.Left *= r2;
            r1.Width *= r2;
            r1.Height *= r2;
            return r1;
        }
    }
}

