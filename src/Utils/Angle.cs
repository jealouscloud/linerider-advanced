//
//  Angle.cs
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

using OpenTK;
using System;

namespace linerider.Utils
{
    public class Angle
    {
        private float _degrees;
        private float _radians;

        /// <summary>
        /// Gets or sets the angle in degrees. Guaranteed between 0 and 360
        /// </summary>
        public float Degrees
        {
            get
            {
                if (float.IsNaN(_degrees))
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
                _radians = float.NaN;//invalidate
            }
        }

        public float Radians
        {
            get
            {
                if (float.IsNaN(_radians))
                {
                    _radians = DegreesToRadians(Degrees);
                }
                return _radians;
            }
            set
            {
                _radians = value;
                _radians %= (float)Math.PI * 2;
                if (_radians < 0)
                {
                    _radians = _radians % -((float)Math.PI * 2);
                    _radians = ((float)Math.PI * 2) + _radians;
                }
                _degrees = float.NaN;//invalidate
            }
        }

        public Angle(float degrees)
        {
            _degrees = degrees;
            _degrees %= 360;
            if (_degrees < 0)
            {
                _degrees = _degrees % -360;
                _degrees = 360 + _degrees;
            }
            _radians = float.NaN;
        }

        public Angle(double degrees)
        {
            _degrees = (float)degrees;
            _degrees %= 360;
            if (_degrees < 0)
            {
                _degrees = _degrees % -360;
                _degrees = 360 + _degrees;
            }
            _radians = float.NaN;
        }

        private Angle()
        {
        }
        public static Angle FromRadians(double radians)
        {
            return new Angle() { Radians = (float)radians };
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

        public static Angle FromVector(Vector2 p1)
        {
            return FromRadians(Math.Atan2(p1.Y, p1.X));
        }

        public static Angle FromLine(Vector2 p1, Vector2 p2)
        {
            return FromVector(p2 - p1);
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * 0.0174532924f;
        }

        private static float RadiansToDegrees(float radians)
        {
            return radians * 57.2957764f;
        }
    }
}