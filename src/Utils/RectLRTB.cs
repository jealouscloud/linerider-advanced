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
using System.Runtime.CompilerServices;
using linerider.Game;
namespace linerider.Utils
{
    /// <summary>
    /// lrtb rectangle for use with physics operations
    /// </summary>
    public struct RectLRTB
    {
        public int top;
        public int left;
        public int right;
        public int bottom;
        /// <summary>
        /// Creates a 3x3 physicsinfo around the point on the simulation grid
        /// </summary>
        /// <param name="start"></param>
        public RectLRTB(ref SimulationPoint start)
        {
            var gp = SimulationGrid.GetGridPoint(start.Location.X, start.Location.Y);
            top = gp.Y - 1;
            bottom = gp.Y + 1;
            left = gp.X - 1;
            right = gp.X + 1;
        }
        /// <summary>
        /// Creates a basic rect where left=right=x and top=bottom=y
        /// </summary>
        /// <param name="gp"></param>
        public RectLRTB(GridPoint gp)
        {
            top = gp.Y;
            bottom = gp.Y;
            left = gp.X;
            right = gp.X;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPoint(GridPoint cell)
        {
            return cell.X >= left && cell.X <= right && cell.Y >= top && cell.Y <= bottom;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(RectLRTB cell)
        {
            return !(cell.bottom < top || bottom < cell.top
            || cell.right < left || right < cell.left);
        }
    }
}