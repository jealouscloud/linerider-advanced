//
//  FastGrid.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK;
namespace linerider.Game
{
	public class FastGrid
	{
		#region Fields

		private SortedList<int, SortedList<int, Chunk>> Chunks = new SortedList<int, SortedList<int, Chunk>>();
		private object _syncRoot = new object();
		private const int SquareSize = 128;
		private FastGrid _bluegrid;
		public FastGrid()
		{
			_bluegrid = new FastGrid(true);
		}
		protected FastGrid(bool bluegrid)
		{
		}
		#endregion Fields

		#region Methods
		private void Register(Line l, int x, int y)
		{
			SortedList<int, Chunk> xrow;
			lock (_syncRoot)
			{
				if (!Chunks.ContainsKey(x))
				{
					xrow = new SortedList<int, Chunk>();
					Chunks[x] = xrow;
				}
				else
				{
					xrow = Chunks[x];
				}
				Chunk yrow;
				if (!xrow.ContainsKey(y))
				{
					yrow = new Chunk();
					xrow[y] = yrow;
				}
				else
				{
					yrow = xrow[y];
				}
				yrow[l.ID] = l;
			}
		}

		private void Unregister(Line l, int x, int y)
		{
			lock (_syncRoot)
			{
				if (!Chunks.ContainsKey(x))
				{
					return;
				}
				var xrow = Chunks[x];
				if (xrow.ContainsKey(y))
				{
					xrow[y].Remove(l.ID);
				}
			}
		}
		public void AddLine(Line line)
		{
			if (_bluegrid != null && line is StandardLine)
				_bluegrid.AddLine(line);
			var pts = GetPointsOnLine(line.Position.X / SquareSize,
				line.Position.Y / SquareSize,
				line.Position2.X / SquareSize,
				line.Position2.Y / SquareSize);

			lock (_syncRoot)
			{
				foreach (var pt in pts)
				{
					Register(line, pt.X, pt.Y);
				}
			}
		}
		public void RemoveLine(Line line)
		{
			if (_bluegrid != null && line is StandardLine)
				_bluegrid.RemoveLine(line);
			
			var pts = GetPointsOnLine(line.Position.X / SquareSize,
				line.Position.Y / SquareSize,
				line.Position2.X / SquareSize,
				line.Position2.Y / SquareSize);

			lock (_syncRoot)
			{
				foreach (var pt in pts)
				{
					Unregister(line, pt.X, pt.Y);
				}
			}
		}

		public Chunk PointToChunk(Vector2d point)
		{
			var x = (int)Math.Floor(point.X / SquareSize);
			var y = (int)Math.Floor(point.Y / SquareSize);
			SortedList<int, Chunk> row;
			lock (_syncRoot)
			{
				if (Chunks.TryGetValue(x, out row))
				{
					Chunk chunk;
					if (row.TryGetValue(y, out chunk))
					{
						return chunk;
					}
				}
			}

			return null;
		}
		public List<Chunk> UsedSolidChunksInRect(FloatRect rect)
		{
			return _bluegrid.UsedChunksInRect(rect);
		}
		public List<Chunk> UsedChunksInRect(FloatRect rect)
		{
			int yfirst = (int)Math.Floor(rect.Top / SquareSize);
			int xfirst = (int)Math.Floor(rect.Left / SquareSize);
			int ylast = (int)Math.Floor((rect.Top + rect.Height) / SquareSize);
			int xlast = (int)Math.Floor((rect.Left + rect.Width) / SquareSize);
			var ret = new List<Chunk>(((xlast - xfirst) * (ylast - yfirst)) / 2);
			int start = 0;

			lock (_syncRoot)
			{
				for (int i = 0; i < Chunks.Keys.Count; i++)
				{
					if (Chunks.Keys[i] >= xfirst)
					{
						if (Chunks.Keys[i] > xlast)
							return ret;
						start = i;
						break;
					}
				}
				for (int i = start; i < Chunks.Keys.Count; i++)
				{
					if (Chunks.Keys[i] > xlast)
					{
						return ret;
					}
					var row = Chunks.Values[i];
					var ixstart = row.Keys.Count;
					for (int ix = 0; ix < row.Keys.Count; ix++)
					{
						if (row.Keys[ix] >= yfirst)
						{
							if (row.Keys[ix] > ylast)
								break;
							ixstart = ix;
							break;
						}
					}
					for (int ix = ixstart; ix < row.Keys.Count; ix++)
					{
						if (row.Keys[ix] > ylast)
							break;
						ret.Add(row.Values[ix]);
					}
				}
			}
			return ret;
		}
		public List<Line> LinesInChunks(List<Chunk> c)
		{
			var hs = new HashSet<Line>(new Linecomparer());
			foreach (var chunk in c)
			{
				hs.UnionWith(chunk.Values);
			}
			List<Line> ret = new List<Line>();
			ret.AddRange(hs);
			return ret;
		}
		public List<Line> SortedLinesInChunks(List<Chunk> c)
		{
			var ss = new SortedSet<Line>(new Linecomparer() { reverse = false });
			foreach (var chunk in c)
			{
				ss.UnionWith(chunk.Values);
			}
			List<Line> ret = new List<Line>();
			ret.AddRange(ss);
			return ret;
		}
		private static Vector2d AngleLock(Vector2d pt, Vector2d pos, Vector2d diff)
		{
			var angle = Math.Atan2(diff.Y, diff.X);
			var delta = pt - pos;
			var ret = new Vector2d(Math.Cos(angle), Math.Sin(angle));
			return (new Vector2d(ret.X, ret.Y) * Vector2d.Dot(delta, ret)) + pos;
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
		#endregion Methods

		#region Classes
		public class Linecomparer : IComparer<Line>, IEqualityComparer<Line>
		{
			#region Methods
			public bool reverse = true;
			public int Compare(Line x, Line y)
			{
					return reverse ? y.ID - x.ID : x.ID - y.ID;
			}

			public bool Equals(Line x, Line y)
			{
				return x.ID == y.ID;
			}

			public int GetHashCode(Line x)
			{
				return x.ID;
			}

			#endregion Methods
		}
		public class Chunk : SortedList<int, Line>
		{

			#region Properties

			public SortedList<int, Line> Lines
			{
				get { return this; }
			}

			#endregion Properties

			#region Constructors

			public Chunk()
							: base(new descintcomparer())
			{
			}

			#endregion Constructors

			#region Classes

			public class descintcomparer : IComparer<int>
			{

				#region Methods

				public int Compare(int x, int y)
				{
					return y - x;
				}

				#endregion Methods

			}

			#endregion Classes

		}

		#endregion Classes
	}
}