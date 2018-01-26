//
//  Turtle.cs
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
using System.Text;
using OpenTK;
namespace linerider.Utils
{
    class Turtle
    {
        private Vector2d _point = new Vector2d(0, 0);
        /// <summary>
        /// The current point the turtle is at.
        /// Calling setter resets angle;
        /// </summary>
        public Vector2d Point
        {
            get
            {
                return _point;
            }
            set
            {
                _point = value;
                _degrees = 0;
            }
        }
        /// <summary>
        /// The current point the turtle is at.
        /// </summary>
        public Vector2d PointNoReset
        {
            get
            {
                return _point;
            }
            set
            {
                _point = value;
            }
        }
        public double X { get { return _point.X; } }
        public double Y { get { return _point.Y; } }
        public double Degrees
        {
            get
            {
                return new Angle(_degrees).Degrees;
            }
        }
        private double _degrees = 0;
        public Turtle(Vector2d startpoint)
        {
            Point = startpoint;
        }

        private static Vector2d CalculateLine(Vector2d position, double degrees, double length)
        {
         //   degrees %= 360;
            if (degrees < 0)
            {
          //      degrees = degrees % -360;
       //         degrees = 360 + degrees;
            }
            var ret = position;
            var radians = MathHelper.DegreesToRadians(degrees);
            var sin = Math.Sin(radians);
            var cos = Math.Cos(radians);
            ret.X = ret.X + (length * cos);
            ret.Y = ret.Y + (length * sin);
            return ret;
        }
        /// <summary>
        /// moves relative to the previous command, retaining angle of the last move command
        /// </summary>
        public void Move(double degrees, double length)
        {
            _degrees += degrees;
            _point = CalculateLine(Point, _degrees, length);
        }
        /// <summary>
        /// moves relative to the previous command, using the static angle used
        /// </summary>
        public void MoveStaticDegrees(double degrees, double length)
        {
            _degrees += degrees;
            _point = CalculateLine(Point, degrees, length);
        }
    }
}
