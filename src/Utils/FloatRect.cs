//
//  FloatRect.cs
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
    public struct FloatRect : IEquatable<FloatRect>
    {
        public float Left;
        public float Top;
        public float Width;
        public float Height;
        public Vector2 Vector
        {
            get
            {
                return new Vector2(Left, Top);
            }
        }
        public Vector2 Size
        {
            get
            {
                return new Vector2(Width, Height);
            }
        }
        public float Right
        {
            get
            {
                return Left + Width;
            }
        }
        public float Bottom
        {
            get
            {
                return Top + Height;
            }
        }
        public FloatRect(float left, float top, float width, float height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }
        public FloatRect(Vector2 position, Vector2 size)
        {
            this = new FloatRect(position.X, position.Y, size.X, size.Y);
        }

        public Vector2 EllipseClamp(Vector2 position)
        {
            var center = Vector + (Size / 2);
            var a = Width / 2;
            var b = Height / 2;
            var p = position - center;
            var d = p.X * p.X / (a * a) + p.Y * p.Y / (b * b);

            if (d > 1)
            {
                Angle angle = Angle.FromLine(center, position);
                double t = Math.Atan((Width / 2) * Math.Tan(angle.Radians) / (Height / 2));
                if (angle.Degrees < 270 && angle.Degrees >= 90)
                {
                    t += Math.PI;
                }
                Vector2 ptfPoint =
                   new Vector2((float)(center.X + (Width / 2) * Math.Cos(t)),
                               (float)(center.Y + (Height / 2) * Math.Sin(t)));

                position = ptfPoint;
            }
            return position;
        }

        public Vector2 Clamp(Vector2 v)
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

        public FloatRect Inflate(float width, float height)
        {
            var rect = this;

            rect.Left -= width;
            rect.Top -= height;
            rect.Width += 2 * width;
            rect.Height += 2 * height;
            return rect;
        }

        public bool Contains(float x, float y)
        {
            float num = Math.Min(this.Left, this.Left + this.Width);
            float num2 = Math.Max(this.Left, this.Left + this.Width);
            float num3 = Math.Min(this.Top, this.Top + this.Height);
            float num4 = Math.Max(this.Top, this.Top + this.Height);
            return x >= num && x < num2 && y >= num3 && y < num4;
        }
        public bool Intersects(FloatRect rect)
        {
            FloatRect floatRect;
            return this.Intersects(rect, out floatRect);
        }
        public bool Intersects(FloatRect rect, out FloatRect overlap)
        {
            float val = Math.Min(this.Left, this.Left + this.Width);
            float val2 = Math.Max(this.Left, this.Left + this.Width);
            float val3 = Math.Min(this.Top, this.Top + this.Height);
            float val4 = Math.Max(this.Top, this.Top + this.Height);
            float val5 = Math.Min(rect.Left, rect.Left + rect.Width);
            float val6 = Math.Max(rect.Left, rect.Left + rect.Width);
            float val7 = Math.Min(rect.Top, rect.Top + rect.Height);
            float val8 = Math.Max(rect.Top, rect.Top + rect.Height);
            float num = Math.Max(val, val5);
            float num2 = Math.Max(val3, val7);
            float num3 = Math.Min(val2, val6);
            float num4 = Math.Min(val4, val8);
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
                    "[FloatRect] Left(",
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
        public bool Equals(FloatRect other)
        {
            return this.Left == other.Left && this.Top == other.Top && this.Width == other.Width && this.Height == other.Height;
        }
        public override int GetHashCode()
        {
            return (int)((uint)this.Left ^ ((uint)this.Top << 13 | (uint)this.Top >> 19) ^ ((uint)this.Width << 26 | (uint)this.Width >> 6) ^ ((uint)this.Height << 7 | (uint)this.Height >> 25));
        }
        public static bool operator ==(FloatRect r1, FloatRect r2)
        {
            return r1.Equals(r2);
        }
        public static bool operator !=(FloatRect r1, FloatRect r2)
        {
            return !r1.Equals(r2);
        }
        public static FloatRect operator /(FloatRect r1, float r2)
        {
            r1.Top /= r2;
            r1.Left /= r2;
            r1.Width /= r2;
            r1.Height /= r2;
            return r1;
        }
        public static FloatRect operator *(FloatRect r1, float r2)
        {
            r1.Top *= r2;
            r1.Left *= r2;
            r1.Width *= r2;
            r1.Height *= r2;
            return r1;
        }
    }
}

