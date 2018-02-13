//
//  SimulationGrid.cs
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
using linerider.Rendering;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using linerider.Game;
using linerider.Utils;
namespace linerider
{

    public partial class SimulationGrid
    {

        private static bool CheckBounds(Rectangle r, int x, int y)
        {
            return x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom;
        }

        public static GridPoint GetGridPoint(double posx, double posy)
        {
            int x = (int)Math.Floor(posx / CellSize);
            int y = (int)Math.Floor(posy / CellSize);
            return new GridPoint(x, y);
        }
        public static CellLocation CellInfo(double posx, double posy)
        {
            var gp = GetGridPoint(posx, posy);
            return new CellLocation(gp, new Vector2d(posx - (CellSize * gp.X), posy - (CellSize * gp.Y)));
        }
        public static List<CellLocation> GetGridPositions(StandardLine line, int gridversion)
        {
            return GetGridPositions(line.Position,line.Position2,gridversion);
        }
        public static List<CellLocation> GetGridPositions(Vector2d linestart, Vector2d lineend, int gridversion)
        {
            Vector2d diff = lineend - linestart;
            var ret = new List<CellLocation>();
            var cell = CellInfo(linestart.X, linestart.Y);
            var gridend = CellInfo(lineend.X, lineend.Y);

            ret.Add(cell);
            if ((diff.X == 0 && diff.Y == 0) || (cell.X == gridend.X && cell.Y == gridend.Y))
                return ret;

            int p1X = Math.Min(cell.X, gridend.X),
                p2X = Math.Max(cell.X, gridend.X),
                p1Y = Math.Min(cell.Y, gridend.Y),
                p2Y = Math.Max(cell.Y, gridend.Y);
            var box = Rectangle.FromLTRB(p1X, p1Y, p2X, p2Y);
            var current = linestart;
            if (gridversion == 62)
            {
                while (true)
                {
                    double maxstepx, maxstepy;
                    if (cell.X < 0)
                        maxstepy = diff.X > 0 ? (CellSize + cell.Remainder.X) : (-CellSize - cell.Remainder.X);
                    else
                        maxstepy = diff.X > 0 ? (CellSize - cell.Remainder.X) : (-(cell.Remainder.X + 1));

                    if (cell.Y < 0)
                        maxstepx = diff.Y > 0 ? (CellSize + cell.Remainder.Y) : (-CellSize - cell.Remainder.Y);
                    else
                        maxstepx = diff.Y > 0 ? (CellSize - cell.Remainder.Y) : (-(cell.Remainder.Y + 1));
                    var stepx = diff.X * maxstepx * (1 / diff.Y);
                    var stepy = diff.Y * maxstepy * (1 / diff.X);
                    current.X += (Math.Abs(stepx) < Math.Abs(maxstepy)) ? stepx : maxstepy;
                    current.Y += (Math.Abs(stepy) < Math.Abs(maxstepx)) ? stepy : maxstepx;
                    cell = CellInfo(current.X, current.Y);
                    if (!CheckBounds(box, cell.X, cell.Y))
                        return ret;
                    ret.Add(cell);
                }
            }
            else if (gridversion == 61) //eh
            {
                ret = GetGridPositions61(linestart,lineend);
            }
            return ret;
        }
        private static List<CellLocation> GetGridPositions61(Vector2d start, Vector2d end)
        {
            Vector2d diff = end - start;
            var ret = new List<CellLocation>();
            var cell = CellInfo(start.X, start.Y);
            var gridend = CellInfo(end.X, end.Y);

            ret.Add(cell);
            if ((diff.X == 0 && diff.Y == 0) || (cell.X == gridend.X && cell.Y == gridend.Y))
                return ret;

            int p1X = Math.Min(cell.X, gridend.X),
                p2X = Math.Max(cell.X, gridend.X),
                p1Y = Math.Min(cell.Y, gridend.Y),
                p2Y = Math.Max(cell.Y, gridend.Y);
            var box = Rectangle.FromLTRB(p1X, p1Y, p2X, p2Y);
            var current = start;
            double slope = 0;
            double _loc13 = 0;
            double isbelowactualY = 0;
            if (diff.X != 0 && diff.Y != 0)
            {
                slope = diff.Y / diff.X;
                _loc13 = 1.0 / slope;
                isbelowactualY = start.Y - slope * start.X;
            } // end if
            while (true)
            {
                double difY, difX;
                difX = -cell.Remainder.X + (diff.X > 0 ? CellSize : -1);
                difY = -cell.Remainder.Y + (diff.Y > 0 ? CellSize : -1);
                if (diff.X == 0)
                {
                    current.Y = current.Y + difY;
                }
                else if (diff.Y == 0)
                {
                    current.X = current.X + difX;
                }
                else
                {
                    var whyyy = Math.Round(slope * (current.X + difX) + isbelowactualY);
                    if (Math.Abs(whyyy - current.Y) < Math.Abs(difY))
                    {
                        current.X = current.X + difX;
                        current.Y = whyyy;
                    }
                    else if (Math.Abs(whyyy - current.Y) == Math.Abs(difY))
                    {
                        current.X = current.X + difX;
                        current.Y = current.Y + difY;
                    }
                    else
                    {
                        current.X = Math.Round((current.Y + difY - isbelowactualY) * _loc13);
                        current.Y = current.Y + difY;
                    }
                }
                cell = CellInfo(current.X, current.Y);
                if (CheckBounds(box, cell.X, cell.Y))
                {
                    ret.Add(cell);
                    continue;
                }
                return ret;
            }
        }
    }
}
