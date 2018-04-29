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

namespace linerider.Utils
{
    public class Angle
    {
        private double _degrees;
        private double _radians;
        private double _coscached = double.NaN;
        private double _sincached = float.NaN;

        /// <summary>
        /// Gets or sets the angle in degrees. Guaranteed between 0 and 360
        /// </summary>
        public double Degrees
        {
            get
            {
                if (double.IsNaN(_degrees))
                {
                    _degrees = RadiansToDegrees(_radians);
                    _degrees %= 360;
                    if (_degrees < 0)
                    {
                        _degrees = _degrees % -360;
                        _degrees = 360 + _degrees;
                    }
                }
                return _degrees;
            }
            set
            {
                _degrees = value;
                _degrees %= 360;
                if (_degrees < 0)
                {
                    _degrees = _degrees % -360;
                    _degrees = 360 + _degrees;
                }
                _radians = double.NaN;//invalidate
                _coscached = double.NaN;
                _sincached = double.NaN;
            }
        }

        public double Radians
        {
            get
            {
                if (double.IsNaN(_radians))
                {
                    _radians = DegreesToRadians(Degrees);
                }
                return _radians;
            }
            set
            {
                _radians = value;
                _radians %= Math.PI * 2;
                if (_radians < 0)
                {
                    _radians = _radians % -(Math.PI * 2);
                    _radians = (Math.PI * 2) + _radians;
                }
                _degrees = double.NaN;//invalidate
                _coscached = double.NaN;
                _sincached = double.NaN;
            }
        }
        public double Cos
        {
            get
            {
                if (double.IsNaN(_coscached))
                {
                    _coscached = Math.Cos(Radians);
                }
                return _coscached;
            }
        }

        public double Sin
        {
            get
            {
                if (double.IsNaN(_sincached))
                {
                    _sincached = Math.Sin(Radians);
                }
                return _sincached;
            }
        }

        public Angle(double degrees)
        {
            _degrees = degrees;
            _degrees %= 360;
            if (_degrees < 0)
            {
                _degrees = _degrees % -360;
                _degrees = 360 + _degrees;
            }
            _radians = double.NaN;
        }

        private Angle()
        {
        }
        public Vector2d MovePoint(Vector2d input, double length)
        {
            return new Vector2d(input.X + (length * Cos),input.Y + (length * Sin));
        }
        public static Angle operator +(Angle a1, Angle a2)
        {
            return FromDegrees(a1.Degrees + a2.Degrees);
        }
        public static Angle operator -(Angle a1, Angle a2)
        {
            return FromDegrees(a1.Degrees - a2.Degrees);
        }
        public double Difference(Angle a2)
        {
            return 180.0 - Math.Abs(Math.Abs(Degrees - a2.Degrees) - 180.0);
        }
        public static Angle FromRadians(double radians)
        {
            return new Angle() { Radians = radians };
        }
        public static Angle FromDegrees(double Degrees)
        {
            return new Angle(Degrees);
        }

        public static Angle FromRadians(float radians)
        {
            return new Angle() { Radians = radians };
        }

        public static Angle FromVector(Vector2d p1)
        {
            return FromRadians(Math.Atan2(p1.Y, p1.X));
        }

        public static Angle FromLine(Vector2d p1, Vector2d p2)
        {
            return FromVector(p2 - p1);
        }
        public static Angle FromLine(Line line)
        {
            return FromVector(line.Position2 - line.Position);
        }

        public static Angle FromVector(Vector2 p1)
        {
            return FromRadians(Math.Atan2(p1.Y, p1.X));
        }

        public static Angle FromLine(Vector2 p1, Vector2 p2)
        {
            return FromVector(p2 - p1);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * 0.0174532924f;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 57.2957764f;
        }
    }
}