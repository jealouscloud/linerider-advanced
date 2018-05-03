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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK;
using linerider.Utils;
using linerider.Game;
using System.Diagnostics;

namespace linerider.Game
{
    /// <summary>
    /// a grid class specifically for tool operations etc
    /// this grid accurately places lines in cells, compared to the sparse
    /// placement in the simulation grid.
    /// </summary>
    public class EditorGrid
    {
        private readonly ResourceSync Sync = new ResourceSync();
        private readonly Dictionary<int, EditorCell> Cells = new Dictionary<int, EditorCell>(4096);
        private object _syncRoot = new object();
        public const int CellSize = 32;
        private int GetCellKey(int x, int y)
        {
            unchecked
            {
                int hash = 27;
                hash = hash * 486187739 + x;
                hash = hash * 486187739 + y;
                return hash;
            }
        }
        public void Clear()
        {
            Cells.Clear();
        }
        public EditorCell GetCell(int x, int y)
        {
            EditorCell cell;
            var pos = GetCellKey(x, y);
            if (!Cells.TryGetValue(pos, out cell))
                return null;
            return cell;

        }

        public EditorCell GetCellFromPoint(Vector2d pos)
        {
            return GetCell((int)Math.Floor(pos.X / CellSize), (int)Math.Floor(pos.Y / CellSize));
        }
        private void Register(GameLine l, int x, int y)
        {
            var key = GetCellKey(x, y);
            EditorCell cell;
            if (!Cells.TryGetValue(key, out cell))
            {
                cell = new EditorCell();
                Cells[key] = cell;
            }
            cell.AddLine(l);
        }

        private void Unregister(GameLine l, int x, int y)
        {
            EditorCell cell;
            var pos = GetCellKey(x, y);
            if (!Cells.TryGetValue(pos, out cell))
                return;
            cell.RemoveLine(l.ID);
        }
        public void AddLine(GameLine line)
        {
            var pts = GetPointsOnLine(line.Position.X / CellSize,
                line.Position.Y / CellSize,
                line.Position2.X / CellSize,
                line.Position2.Y / CellSize);
            using (Sync.AcquireWrite())
            {
                foreach (var pos in pts)
                {
                    Register(line, pos.X, pos.Y);
                }
            }
        }
        public void RemoveLine(GameLine line)
        {
            var pts = GetPointsOnLine(line.Position.X / CellSize,
                line.Position.Y / CellSize,
                line.Position2.X / CellSize,
                line.Position2.Y / CellSize);

            using (Sync.AcquireWrite())
            {
                foreach (var pt in pts)
                {
                    Unregister(line, pt.X, pt.Y);
                }
            }
        }
        public EditorCell LinesInRect(DoubleRect rect)
        {
            int starty = (int)Math.Floor(rect.Top / CellSize);
            int startx = (int)Math.Floor(rect.Left / CellSize);
            int endy = (int)Math.Floor((rect.Top + rect.Height) / CellSize);
            int endx = (int)Math.Floor((rect.Left + rect.Width) / CellSize);
            EditorCell ret = new EditorCell();
            using (Sync.AcquireWrite())
            {
                for (int x = startx; x <= endx; x++)
                {
                    for (int y = starty; y <= endy; y++)
                    {
                        var cell = GetCell(x, y);
                        if (cell != null)
                        {
                            ret.Combine(cell);
                        }
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Raytrace the specified x0, y0, x1 and y1.
        /// </summary>
        /// <returns>The raytrace.</returns>
        /// <param name="x0">X0.</param>
        /// <remarks>From http://playtechs.blogspot.ca/2007/03/raytracing-on-grid.html</remarks>
        public static IEnumerable<GridPoint> raytrace(double x0, double y0, double x1, double y1)
        {
            double dx = Math.Abs(x1 - x0);
            double dy = Math.Abs(y1 - y0);

            int x = (int)(Math.Floor(x0));
            int y = (int)(Math.Floor(y0));

            int n = 1;
            int x_inc, y_inc;
            double error;
            HashSet<GridPoint> hs = new HashSet<GridPoint>();
            if (dx == 0)
            {
                x_inc = 0;
                error = double.PositiveInfinity;
            }
            else if (x1 > x0)
            {
                x_inc = 1;
                n += (int)(Math.Floor(x1)) - x;
                error = (Math.Floor(x0) + 1 - x0) * dy;
            }
            else
            {
                x_inc = -1;
                n += x - (int)(Math.Floor(x1));
                error = (x0 - Math.Floor(x0)) * dy;
            }

            if (dy == 0)
            {
                y_inc = 0;
                error -= double.PositiveInfinity;
            }
            else if (y1 > y0)
            {
                y_inc = 1;
                n += (int)(Math.Floor(y1)) - y;
                error -= (Math.Floor(y0) + 1 - y0) * dx;
            }
            else
            {
                y_inc = -1;
                n += y - (int)(Math.Floor(y1));
                error -= (y0 - Math.Floor(y0)) * dx;
            }

            for (; n > 0; --n)
            {
                hs.Add(new Game.GridPoint(x, y));

                if (error > 0)
                {
                    y += y_inc;
                    error -= dx;
                }
                else
                {
                    x += x_inc;
                    error += dy;
                }
            }
            return hs.ToArray();
        }
        /// <summary>
        /// Takes the line (in double precision floating point) and finds all points on a 1x1 grid it interacts with.
        /// </summary>
        /// <remarks>The Vector2d and doublerect classes can be replaced with your own.</remarks>
        public static IEnumerable<GridPoint> GetPointsOnLine(double x0, double y0, double x1, double y1)
        {
            return raytrace(x0, y0, x1, y1);
        }
    }
}