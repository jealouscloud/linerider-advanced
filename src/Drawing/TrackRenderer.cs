//
//  TrackRenderer.cs
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
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Collections.Concurrent;
using System.Linq;
namespace linerider.Drawing
{
	internal class TrackRenderer : GameService
	{

		#region Fields

		public bool RequiresUpdate = true;

		private ConcurrentDictionary<int, linevertices> _lines = new ConcurrentDictionary<int, linevertices>();

		private bool _prepped_blackline = true;

		private bool _prepped_coloredline = true;

		private bool _prepped_knobs = true;

		private bool _prepped_knobs_red = true;

		private bool _prepped_gwells = false;

		private VAO _vao = new VAO(true, false);

		private HashSet<int> collisions = new HashSet<int>();

		private List<StandardLine> lines;

		ConcurrentQueue<int> availablevertices = new ConcurrentQueue<int>();
		ConcurrentQueue<int> linechangedvertices = new ConcurrentQueue<int>();

		private object SyncRoot = new object();

		#endregion Fields

		#region Constructors

		public TrackRenderer()
		{
			_vao.Texture = StaticRenderer.CircleTex;
		}

		#endregion Constructors

		#region Methods

		public List<int> DrawThickLine(Vector2 p, Vector2 p1, float width, Color c)
		{
			List<int> ret = new List<int>();
			var vecs = StaticRenderer.GenerateThickLine(p, p1, width);
			int v1 = AddVertex(new Vertex(vecs[0].X, vecs[0].Y, c));
			int v2 = AddVertex(new Vertex(vecs[1].X, vecs[1].Y, c));
			int v3 = AddVertex(new Vertex(vecs[2].X, vecs[2].Y, c));
			int v4 = AddVertex(new Vertex(vecs[3].X, vecs[3].Y, c));
			ret.Add(v1);
			ret.Add(v2);
			ret.Add(v3);

			ret.Add(v1);
			ret.Add(v4);
			ret.Add(v3);
			return ret;
		}
		private void AddLineIndices(Line l)
		{
			bool r_knobs = _prepped_knobs;
			bool r_knobs_red = _prepped_knobs_red;
			bool r_coloredline = _prepped_coloredline;
			bool r_blackline = _prepped_blackline;
			var lv = _lines[l.ID];
			bool hittest = game.HitTest && game.Track.Animating && collisions.Contains(l.ID);
			// ids.Contains(line.Key);
			if (r_coloredline && !hittest)
				AddIndices(lv.coloredtrackline);
			if (hittest)
				AddIndices(lv.hittestline);
			if (r_blackline && !hittest)
				AddIndices(lv.blacktrackline);
			if (r_knobs)
				AddIndices(lv.knobs);
			if (r_knobs_red)
				AddIndices(lv.redknobs);
			if (_prepped_gwells)
				AddIndices(lv.gwell);
		}
		public void Render(Track track, bool colors, int knobstate, bool gwells)
		{
			using (new GLEnableCap(EnableCap.Texture2D))
			{
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				GameDrawingMatrix.Enter();
				try
				{
					bool r_knobs = knobstate == 1;
					bool r_knobs_red = knobstate == 2;
					bool r_coloredline = colors;
					bool r_blackline = !colors;

					if (RequiresUpdate || _prepped_knobs != r_knobs || _prepped_knobs_red != r_knobs_red ||
						r_coloredline != _prepped_coloredline || r_blackline != _prepped_blackline ||
						(game.HitTest && game.Track.Animating && !track.AllCollidedLines.SetEquals(collisions)))
					{
						lock (SyncRoot)
						{
							RequiresUpdate = false;
							lock (track.AllCollidedLines)
							{
								if (game.HitTest)
									collisions = new HashSet<int>(track.AllCollidedLines);
							}
							_vao.ClearIndices();

							_prepped_blackline = r_blackline;
							_prepped_coloredline = r_coloredline;
							_prepped_knobs = r_knobs;
							_prepped_knobs_red = r_knobs_red;
							_prepped_gwells = gwells;
							foreach (var line in lines)
							{
								AddLineIndices(line);
							}
						}
					}
					_vao.Draw(PrimitiveType.Triangles);
				}
				finally
				{
					GameDrawingMatrix.Exit();
				}
			}
		}

		/// <summary>
		/// Updates viewport.
		/// </summary>
		/// <param name="lines">Lines in current viewport, in the following order</param>
		/// <param name="colors"></param>
		/// <param name="knobstate"></param>
		public void UpdateViewport(List<Line> vpu)
		{
			availablevertices = new ConcurrentQueue<int>();
			collisions.Clear();
			_vao.Clear();
			_lines.Clear();
			RequiresUpdate = true;
			Invalidate();
			lines = new List<StandardLine>();
			if (vpu != null)
			{
				foreach (var v in vpu)
				{
					if (v is StandardLine)
					{
						AddLine((StandardLine)v);
						lines.Add((StandardLine)v);
					}
				}
			}
		}

		public void AddLine(StandardLine l)
		{
			lock (SyncRoot)
			{
				if (_lines.TryAdd(l.ID, CreateLine(l)))
				{
					AddLineIndices(l);
					lines.Add(l);
				}
			}
		}

		public void RemoveLine(StandardLine line)
		{
			lock (SyncRoot)
			{
				linevertices l;
				if (_lines.TryRemove(line.ID, out l))
				{
					lines.Remove(line);
					FreeVertices(l.coloredtrackline);
					FreeVertices(l.hittestline);
					FreeVertices(l.blacktrackline);
					FreeVertices(l.knobs);
					FreeVertices(l.redknobs);
					FreeVertices(l.gwell);
					Invalidate();
				}
			}
		}

		public void LineChanged(StandardLine line)
		{
			if (line == null)
				return;
			lock (SyncRoot)
			{
				linevertices lv;
				if (_lines.TryGetValue(line.ID, out lv))
				{
					LineChangedFreeVertices(lv.coloredtrackline);

					var newcoloredline = DrawTrackLine(line, true);//chance the colored line could change in type, like 3x multiplier

					LineChangedFreeVertices(lv.blacktrackline);
					DrawBasicTrackLine((Vector2)line.Position, (Vector2)line.Position2,
						Settings.Default.NightMode ? Color.White : Color.Black);

					LineChangedFreeVertices(lv.knobs);
					RenderCircle((Vector2)line.Position, Settings.Default.NightMode ? Color.Black : Color.White, 0.75f,
						100);
					RenderCircle((Vector2)line.Position2, Settings.Default.NightMode ? Color.Black : Color.White,
						0.75f, 100);


					LineChangedFreeVertices(lv.redknobs);
					RenderCircle((Vector2)line.Position, Color.Red, 0.75f, 100);
					RenderCircle((Vector2)line.Position2, Color.Red, 0.75f, 100);

					Color linecolor = line.GetLineType() == LineType.Red
						? Color.FromArgb(0xCC, 0, 0)
						: Color.FromArgb(0, 0x66, 0xFF);
					LineChangedFreeVertices(lv.hittestline);
					DrawBasicTrackLine((Vector2)line.Position, (Vector2)line.Position2, linecolor);

					LineChangedFreeVertices(lv.gwell);
					DrawGWell(line as StandardLine);

                    if (lv.coloredtrackline.Count != newcoloredline.Count)//line indices changed, remind the vao
                    {
                        RequiresUpdate = true;
                        lv.coloredtrackline = newcoloredline;
                        _lines[line.ID] = lv;
                    }
                }
			}
		}

		private void ToggleArray(ref int start, List<int> indices, bool show)
		{
			if (indices == null)
				return;
			for (int i = 0; i < indices.Count; i++)
			{
				_vao.SetIndex(i + start, show ? indices[i] : -1);
			}
			start += indices.Count;
		}
		private linevertices CreateLine(StandardLine l)
		{
			linevertices lv = new linevertices();
			lv.coloredtrackline = DrawTrackLine(l, true);
			lv.blacktrackline = DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, Settings.Default.NightMode ? Color.White : Color.Black);
			lv.knobs = new List<int>(6 * 2);

			lv.knobs.AddRange(RenderCircle((Vector2)l.Position, Settings.Default.NightMode ? Color.Black : Color.White, 0.75f, 100));
			lv.knobs.AddRange(RenderCircle((Vector2)l.Position2, Settings.Default.NightMode ? Color.Black : Color.White, 0.75f, 100));
			lv.redknobs = new List<int>(6 * 2);
			lv.redknobs.AddRange(RenderCircle((Vector2)l.Position, Color.Red, 0.75f, 100));
			lv.redknobs.AddRange(RenderCircle((Vector2)l.Position2, Color.Red, 0.75f, 100));
			Color linecolor = l.GetLineType() == LineType.Red ? Color.FromArgb(0xCC, 0, 0) : Color.FromArgb(0, 0x66, 0xFF);
			lv.hittestline = DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, linecolor);

			lv.gwell = DrawGWell(l);
			return lv;
		}
		private void AddIndices(List<int> indices)
		{
			if (indices != null)
			{
				foreach (var index in indices)
				{
					_vao.AddIndex(index);
				}
			}
		}
		private void Invalidate()
		{
			RequiresUpdate = true;
			_prepped_blackline = true;
			_prepped_coloredline = true;
			_prepped_knobs = true;
			_prepped_knobs_red = true;//both knobs being true basically means it has to invalidate
		}
		private void FreeVertices(List<int> indices)
		{
			if (indices != null)
			{
				HashSet<int> hs = new HashSet<int>();
				for (int i = 0; i < indices.Count; i++)
				{
					if (hs.Add(indices[i]))
						availablevertices.Enqueue(indices[i]);
				}
			}
		}
		private void LineChangedFreeVertices(List<int> indices)
		{
			if (indices != null)
			{
				HashSet<int> hs = new HashSet<int>();
				for (int i = 0; i < indices.Count; i++)
				{
					if (hs.Add(indices[i]))
						linechangedvertices.Enqueue(indices[i]);
				}
			}
		}
		private List<int> DrawBasicTrackLine(Vector2 p1, Vector2 p2, Color linecolor)
		{
			List<int> ret = new List<int>(6 + 6 + 6);
			ret.AddRange(RenderCircle(p1, linecolor, 1, 20));
			ret.AddRange(RenderCircle(p2, linecolor, 1, 20));
			ret.AddRange(DrawThickLine(p1, p2, 2, linecolor));
			return ret;
		}

		private List<int> DrawGWell(StandardLine line)
		{
			List<int> ret = new List<int>();
			var p1 = (Vector2)line.Position;
			var p2 = (Vector2)line.Position2;
			Vector2[] vecs = new Vector2[4];
			var angle = Tools.Angle.FromLine(p1, p2);
			angle.Radians += 1.5708f; //90 degrees as const, radians so no conversion between degrees for a radians only calculation
			var t = StaticRenderer.CalculateLine(Vector2.Zero, angle, line.inv ? -StandardLine.Zone : StandardLine.Zone);
			vecs[0] = p1 + t;
			vecs[1] = p2 + t;
			vecs[2] = p2;
			vecs[3] = p1;
			var c = Color.FromArgb(40, 0, 0, 0);
			int v1 = AddVertex(new Vertex(vecs[0].X, vecs[0].Y, c));
			int v2 = AddVertex(new Vertex(vecs[1].X, vecs[1].Y, c));
			int v3 = AddVertex(new Vertex(vecs[2].X, vecs[2].Y, c));
			int v4 = AddVertex(new Vertex(vecs[3].X, vecs[3].Y, c));
			ret.Add(v1);
			ret.Add(v2);
			ret.Add(v3);

			ret.Add(v1);
			ret.Add(v4);
			ret.Add(v3);
			return ret;
		}

		private List<int> DrawTrackLine(StandardLine line, bool colors)
		{
			List<int> ret = new List<int>((6 * 6) + 3);
			var type = line.GetLineType();
			Color c = Settings.Default.NightMode ? Color.White : Color.Black;
			if (colors)
			{
				switch (type)
				{
					case LineType.Blue:
						{
							c = Color.FromArgb(0, 0x66, 0xFF);
							var l = line;
							var loc3 = (float)(l.Perpendicular.X > 0 ? (Math.Ceiling(l.Perpendicular.X)) : (Math.Floor(l.Perpendicular.X)));
							var loc4 = (float)(l.Perpendicular.Y > 0 ? (Math.Ceiling(l.Perpendicular.Y)) : (Math.Floor(l.Perpendicular.Y)));
							Vector2 p1 = new Vector2((float)l.Position.X + loc3, (float)l.Position.Y + loc4),
								p2 = new Vector2((float)l.Position2.X + loc3, (float)l.Position2.Y + loc4);
							ret.AddRange(DrawBasicTrackLine(p1, p2, c));
						}
						break;

					case LineType.Red:
						{
							c = Color.FromArgb(0xCC, 0, 0);
							var l = line as RedLine;
							var loc3 = (float)(l.Perpendicular.X > 0 ? (Math.Ceiling(l.Perpendicular.X)) : (Math.Floor(l.Perpendicular.X)));
							var loc4 = (float)(l.Perpendicular.Y > 0 ? (Math.Ceiling(l.Perpendicular.Y)) : (Math.Floor(l.Perpendicular.Y)));
							for (int ix = 0; ix < l.Multiplier; ix++)
							{
								var angle = MathHelper.RadiansToDegrees(Math.Atan2(l.diff.Y, l.diff.X));
								Turtle t = new Turtle(l.Position2);
								var basex = 8 + (ix * 2);
								t.Move(angle, -basex);
								var v0 = AddVertex(new Vertex((float)t.X, (float)t.Y, c));
								t.Move(90, l.inv ? -8 : 8);
								var v1 = AddVertex(new Vertex((float)t.X, (float)t.Y, c));
								t.Point = l.Position2;
								t.Move(angle, -(ix * 2));
								var v2 = AddVertex(new Vertex((float)t.X, (float)t.Y, c));
								ret.Add(v0);
								ret.Add(v1);
								ret.Add(v2);
							}
							Vector2 p1 = new Vector2((float)l.Position.X + loc3, (float)l.Position.Y + loc4), p2 = new Vector2((float)l.Position2.X + loc3, (float)l.Position2.Y + loc4);
							ret.AddRange(DrawBasicTrackLine(p1, p2, c));
						}
						break;
				}
			}
			Color linecolor = Settings.Default.NightMode ? Color.White : Color.Black;
			var linep1 = line.Position;
			var linep2 = line.Position2;
			if (line.Trigger != null && line.Trigger.Enabled && colors)
			{
				linecolor = Color.FromArgb(0xFF, 0x95, 0x4F);
			}
#if drawextension
                if (l.Extension != StandardLine.ExtensionDirection.None)
                {
                    linerider.Tools.Angle angle = Tools.Angle.FromLine(l.Position, l.Position2);
                    switch (l.Extension)
                    {
                        case StandardLine.ExtensionDirection.Left:
                            {
                                Turtle turtle = new Turtle(l.CompliantPosition);
                                turtle.Move(angle.Degrees,l.inv ? 4 : -4);
                                if (l.inv)
                                {
                                    linep2 = turtle.Point;
                                }
                                else
                                {
                                    linep1 = turtle.Point;
                                }
                            }
                            break;
                        case StandardLine.ExtensionDirection.Right:
                            {
                                Turtle turtle = new Turtle(l.CompliantPosition2);
                                turtle.Move(angle.Degrees, !l.inv ? 4 : -4);
                                if (l.inv)
                                {
                                    linep1 = turtle.Point;
                                }
                                else
                                {
                                    linep2 = turtle.Point;
                                }
                            }
                            break;
                        case StandardLine.ExtensionDirection.Both:
                            {
                                Turtle turtle = new Turtle(l.CompliantPosition);
                                turtle.Move(angle.Degrees, -2);
                                if (l.inv)
                                {
                                    linep2 = turtle.Point;
                                }
                                else
                                {
                                    linep1 = turtle.Point;
                                }

                                turtle = new Turtle(l.CompliantPosition2);
                                turtle.Move(angle.Degrees, !l.inv ? 4 : -4);
                                if (l.inv)
                                {
                                    linep1 = turtle.Point;
                                }
                                else
                                {
                                    linep2 = turtle.Point;
                                }
                            }
                            break;
                    }
                }
#endif
			ret.AddRange(DrawBasicTrackLine((Vector2)linep1, (Vector2)linep2, linecolor));
			return ret;
		}

		private List<int> RenderCircle(Vector2 p, Color c, float radius, int segments)
		{
			List<int> ret = new List<int>();
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y - radius, c, 1, 1)));
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y + radius, c, 1, 0)));
			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y + radius, c, 0, 0)));

			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y + radius, c, 0, 0)));
			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y - radius, c, 0, 1)));
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y - radius, c, 1, 1)));
			return ret;
		}
		private int AddVertex(Vertex v)
		{
			int index = 0;
			if (linechangedvertices.TryDequeue(out index))
			{
				_vao.SetVertex(index, v);
			}
			else if (availablevertices.TryDequeue(out index))
			{
				_vao.SetVertex(index, v);
			}
			else
			{
				index = _vao.AddVertex(v);
			}
			return index;
		}

		#endregion Methods

		#region Structs

		private struct linevertices
		{

			#region Fields

			public List<int> blacktrackline;
			public List<int> coloredtrackline;
			public List<int> hittestline;
			public List<int> knobs;
			public List<int> redknobs;
			public List<int> gwell;

			#endregion Fields

		}

		#endregion Structs

	}
}