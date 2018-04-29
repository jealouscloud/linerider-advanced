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

namespace linerider.Game
{

    /// <summary>
    /// Defines a point on a two-dimensional plane.
    /// </summary>
    public struct GridPoint
    {

        int x, y;

        /// <summary>
        /// Constructs a new Point instance.
        /// </summary>
        /// <param name="x">The X coordinate of this instance.</param>
        /// <param name="y">The Y coordinate of this instance.</param>
        public GridPoint(int x, int y)
            : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets a <see cref="System.Boolean"/> that indicates whether this instance is empty or zero.
        /// </summary>
        public bool IsEmpty { get { return X == 0 && Y == 0; } }

        /// <summary>
        /// Gets or sets the X coordinate of this instance.
        /// </summary>
        public int X { get { return x; } set { x = value; } }

        /// <summary>
        /// Gets or sets the Y coordinate of this instance.
        /// </summary>
        public int Y { get { return y; } set { y = value; } }

        /// <summary>
        /// Returns the Point (0, 0).
        /// </summary>
        public static readonly GridPoint Zero = new GridPoint();

        /// <summary>
        /// Returns the Point (0, 0).
        /// </summary>
        public static readonly GridPoint Empty = new GridPoint();

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left is equal to right; false otherwise.</returns>
        public static bool operator ==(GridPoint left, GridPoint right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left is not equal to right; false otherwise.</returns>
        public static bool operator !=(GridPoint left, GridPoint right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Converts an OpenTK.Point instance to a System.Drawing.Point.
        /// </summary>
        /// <param name="point">
        /// The <see cref="GridPoint"/> instance to convert.
        /// </param>
        /// <returns>
        /// A <see cref="System.Drawing.Point"/> instance equivalent to point.
        /// </returns>
        public static implicit operator System.Drawing.Point(GridPoint point)
        {
            return new System.Drawing.Point(point.X, point.Y);
        }

        /// <summary>
        /// Converts a System.Drawing.Point instance to an OpenTK.Point.
        /// </summary>
        /// <param name="point">
        /// The <see cref="System.Drawing.Point"/> instance to convert.
        /// </param>
        /// <returns>
        /// A <see cref="GridPoint"/> instance equivalent to point.
        /// </returns>
        public static implicit operator GridPoint(System.Drawing.Point point)
        {
            return new GridPoint(point.X, point.Y);
        }

        /// <summary>
        /// Converts an OpenTK.Point instance to a System.Drawing.PointF.
        /// </summary>
        /// <param name="point">
        /// The <see cref="GridPoint"/> instance to convert.
        /// </param>
        /// <returns>
        /// A <see cref="System.Drawing.PointF"/> instance equivalent to point.
        /// </returns>
        public static implicit operator System.Drawing.PointF(GridPoint point)
        {
            return new System.Drawing.PointF(point.X, point.Y);
        }

        /// <summary>
        /// Indicates whether this instance is equal to the specified object.
        /// </summary>
        /// <param name="obj">The object instance to compare to.</param>
        /// <returns>True, if both instances are equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is GridPoint)
                return Equals((GridPoint)obj);

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A <see cref="System.Int32"/> that represents the hash code for this instance./></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = hash * 486187739 + x;
                hash = hash * 486187739 + y;
                return hash;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that describes this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that describes this instance.</returns>
        public override string ToString()
        {
            return String.Format("{{{0}, {1}}}", X, Y);
        }
        /// <summary>
        /// Indicates whether this instance is equal to the specified Point.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True, if both instances are equal; false otherwise.</returns>
        public bool Equals(GridPoint other)
        {
            return X == other.X && Y == other.Y;
        }
    }
}