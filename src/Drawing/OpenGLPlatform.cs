//
//  OpenGLPlatform.cs
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

using MatterHackers.Agg;
using NGraphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using LibTessDotNet;
namespace linerider.Drawing
{
	internal class OpenGLCanvas
	{
		#region Methods

		public void BeginRenderingModel(float zoom, double angle, Vector2d translate, bool min_linewidth, float opacity = 1f)
		{
			if (renderingmodel)
				throw new InvalidOperationException("Model rendering is already started");
			renderingmodel = true;
			SaveState();

			GL.Scale(zoom, zoom, 0);
			GL.Translate(translate.X, translate.Y, 0);
			GL.Rotate(angle, 0, 0, 1);
		}

		public void DrawEllipse(Rect frame, Pen pen = null, Brush brush = null)
		{
			var ellipse = StaticRenderer.GenerateEllipse((float)frame.Width / 2, (float)frame.Height / 2, 25);
			var center = Vec2(frame.Center);
			for (int i = 0; i < ellipse.Length; i++)
			{
				ellipse[i] += center;
			}

			if (brush != null)
			{
				if (brush is SolidBrush)
				{
					var c = (brush as SolidBrush).Color;
					GL.Color4(c.R, c.G, c.B, c.A);
				}
				GL.Begin(PrimitiveType.Polygon);
				foreach (var v in ellipse)
				{
					GL.Vertex2(v);
				}
				GL.End();
			}
			if (pen != null)
			{
				StaticRenderer.DrawConnectedLines(ellipse, System.Drawing.Color.FromArgb(pen.Color.Argb), (float)pen.Width);
			}
		}

		public void DrawImage(IImage image, Rect frame, double alpha = 1)
		{
		}

		public void DrawPath(Path path, VBO vbo)
		{
			List<List<Vector2>> shapes = new List<List<Vector2>>();
			List<Vector2> positions = new List<Vector2>();
			List<Vector2> tesselated = new List<Vector2>();
			Point position = new Point();
			foreach (var op in path.Operations)
			{
				var mt = op as MoveTo;
				if (mt != null)
				{
					shapes.Add(positions);
					positions = new List<Vector2>();

					positions.Add(Vec2(mt.Point));
					position = mt.Point;
					continue;
				}
				var lt = op as LineTo;
				if (lt != null)
				{
					positions.Add(Vec2(lt.Point));
					position = lt.Point;
					continue;
				}
				var at = op as ArcTo;
				if (at != null)
				{
					var p = nsvg.nsvg__pathArcTo((Vector2d)Vec2(position), at);
					positions.AddRange(p);
					position = at.Point;
					continue;
				}
				var ct = op as CurveTo;
				if (ct != null)
				{
					if (double.IsNaN(ct.Control1.X) && double.IsNaN(ct.Control1.Y))
					{
						BezierCurveQuadric b = new BezierCurveQuadric(Vec2(position), Vec2(ct.EndPoint),
							Vec2(ct.Control2));
						Vector2 old = b.CalculatePoint(0f);
						positions.Add(old);
						var precision = 0.05f;
						for (float i = precision; i < 1f + precision; i += precision)
						{
							Vector2 j = b.CalculatePoint(i);
							positions.Add(j);
							old = j;
						}
					}
					else
					{
						BezierCurveCubic b = new BezierCurveCubic(Vec2(position), Vec2(ct.EndPoint), Vec2(ct.Control1),
							Vec2(ct.Control2));
						Vector2 old = b.CalculatePoint(0f);
						positions.Add(old);
						var precision = 0.05f;
						for (float i = precision; i < 1f + precision; i += precision)
						{
							Vector2 j = b.CalculatePoint(i);
							positions.Add(j);
						}
					}
					position = ct.EndPoint;
				}
				var cp = op as ClosePath;
				if (cp != null)
				{
					if (positions.Count > 0)
					{
						positions.Add(positions[0]);
						shapes.Add(positions);
					}
					positions = new List<Vector2>();
					position = new Point();
					continue;
				}
			}
			shapes.Add(positions);

			brushcheck(path.Brush);
			LibTessDotNet.Tess t = new LibTessDotNet.Tess();
			List<Vec3> vertices = new List<Vec3>();
			foreach (var s in shapes)
			{
				ContourVertex[] cv = new ContourVertex[s.Count];
				for (int i = 0; i < s.Count; i++)
				{
					var v = s[i];
					cv[i] = new ContourVertex() { Position = new Vec3() { X = v.X, Y = v.Y } };
				}
				t.AddContour(cv);
			}
			var rule = LibTessDotNet.WindingRule.NonZero;
			if (path.Brush != null)
			{
				rule = path.Brush.FillMode == FillMode.EvenOdd ? LibTessDotNet.WindingRule.EvenOdd : LibTessDotNet.WindingRule.NonZero;
			}
			t.Tessellate(rule, LibTessDotNet.ElementType.Polygons, 3);
			for (var i = 0; i < t.ElementCount; i++)
			{
				for (var tri = 0; tri < 3; tri++)
				{
					vertices.Add(t.Vertices[t.Elements[(i * 3) + tri]].Position);
				}
			}
			for (int i = 0; i < vertices.Count; i++)
			{
				tesselated.Add(new Vector2(vertices[i].X, vertices[i].Y));
			}
			var lineshapes = new List<List<Vector2>>();
			foreach (var s in shapes)
			{
				if (s.Count == 0)
					continue;
				List<Vector2> add = new List<Vector2>();
				for (int i = 0; i < s.Count; i++)
				{
					add.Add(new Vector2(s[i].X, s[i].Y));
				}
				lineshapes.Add(add);
			}

			if (path.Brush is SolidBrush || path.Pen != null)
			{
				if (path.Brush is SolidBrush)
				{
					var sb = path.Brush as SolidBrush;
					for (int i = 0; i < tesselated.Count; i++)
					{
						vbo.AddVertex(new Vertex(tesselated[i], color(sb.Color)));
					}
				}
				if (path.Pen != null)
				{
					foreach (var list in lineshapes)
					{
						foreach (var v in TesselateLines(list, path.Pen))
						{
							vbo.AddVertex(v);
						}
					}
				}
			}
		}

		public void DrawRectangle(Rect frame, Pen pen = null, Brush brush = null)
		{
			brushcheck(brush);
			if (brush != null)
			{
				if (brush is SolidBrush)
				{
					GL.Color4(color((brush as SolidBrush).Color));
				}
				GL.Begin(PrimitiveType.Quads);
				GL.Vertex2(frame.X, frame.Y);
				GL.Vertex2(frame.X + frame.Width, frame.Y);
				GL.Vertex2(frame.X + frame.Width, frame.Y + frame.Height);
				GL.Vertex2(frame.X, frame.Y + frame.Height);
				GL.End();
			}
			if (pen != null)
			{
				StaticRenderer.RenderLine(new Vector2d(frame.X, frame.Y), new Vector2d(frame.X + frame.Width, frame.Y), System.Drawing.Color.FromArgb(pen.Color.Argb), (float)pen.Width);
				StaticRenderer.RenderLine(new Vector2d(frame.X + frame.Width, frame.Y), new Vector2d(frame.X + frame.Width, frame.Y + frame.Height), System.Drawing.Color.FromArgb(pen.Color.Argb), (float)pen.Width);
				StaticRenderer.RenderLine(new Vector2d(frame.X + frame.Width, frame.Y + frame.Height), new Vector2d(frame.X, frame.Y + frame.Height), System.Drawing.Color.FromArgb(pen.Color.Argb), (float)pen.Width);
				StaticRenderer.RenderLine(new Vector2d(frame.X, frame.Y + frame.Height), new Vector2d(frame.X, frame.Y), System.Drawing.Color.FromArgb(pen.Color.Argb), (float)pen.Width);
			}
		}

		public void DrawText(string text, Rect frame, Font font, TextAlignment alignment = TextAlignment.Left, Pen pen = null, Brush brush = null)
		{
		}

		public void EndRenderingModel()
		{
			if (!renderingmodel)
				throw new InvalidOperationException("Modelrendering has not started");
			renderingmodel = false;
			RestoreState();
		}

		public Size MeasureText(string text, Font font)
		{
			return new Size(0, 0);
		}

		public void RestoreState()
		{
			GL.PopMatrix();
		}

		public void SaveState()
		{
			GL.PushMatrix();
		}

		public void Transform(Transform transform)
		{
			var matrix = new Matrix4d(
				transform.A, transform.B, 0, 0,
				transform.C, transform.D, 0, 0,
				0, 0, 1, 0,
			   transform.E, transform.F, 0, 1);
			GL.MultMatrix(ref matrix);
		}

		#endregion Methods

		#region Fields
		private bool renderingmodel = false;

		#endregion Fields

		private void brushcheck(Brush brush)
		{
			if (brush == null)
				return;
			if (!(brush is SolidBrush))
			{
				throw new NotSupportedException("Brush shaders are not implemented");
			}
		}

		private System.Drawing.Color color(Color c)
		{
			return System.Drawing.Color.FromArgb(c.Argb);
		}

		private List<Vertex> TesselateLines(List<Vector2> points, Pen p)
		{
			List<ContourVertex> vecs = new List<ContourVertex>();
			List<Vertex> ret = new List<Vertex>();
			if (points.Count < 2)
				return ret;
			var co = color(p.Color);
			MatterHackers.Agg.VertexSource.PathStorage ps = new MatterHackers.Agg.VertexSource.PathStorage();
			ps.remove_all();
			ps.MoveTo(points[0].X, points[0].Y);

			for (int i = 1; i < points.Count; i++)
			{
				ps.LineTo(points[i].X, points[i].Y);
			}
			ps.end_poly();

			MatterHackers.Agg.VertexSource.Stroke str = new MatterHackers.Agg.VertexSource.Stroke(ps, p.Width);
			switch (p.Join)
			{
				case LineJoin.Round:
					str.line_join(MatterHackers.Agg.VertexSource.LineJoin.Round);
					str.inner_join(MatterHackers.Agg.VertexSource.InnerJoin.Round);

					break;

				case LineJoin.Miter:
					str.line_join(MatterHackers.Agg.VertexSource.LineJoin.Miter);
					str.inner_join(MatterHackers.Agg.VertexSource.InnerJoin.Miter);
					str.inner_miter_limit(p.MiterLimit);
					str.miter_limit(p.MiterLimit);
					break;
			}
			switch (p.Cap)
			{
				case LineCaps.Butt:
					str.line_cap(MatterHackers.Agg.VertexSource.LineCap.Butt);
					break;

				case LineCaps.Round:
					str.line_cap(MatterHackers.Agg.VertexSource.LineCap.Round);
					break;

				case LineCaps.Square:
					str.line_cap(MatterHackers.Agg.VertexSource.LineCap.Square);
					break;
			}
			str.rewind(0);
			double x, y;
			LibTessDotNet.Tess t = new LibTessDotNet.Tess();

			ShapePath.FlagsAndCommand cmd;
			do
			{
				cmd = str.vertex(out x, out y);
				if (ShapePath.is_vertex(cmd))
				{
					vecs.Add(new ContourVertex() { Position = new Vec3() { X = (float)x, Y = (float)y } });
				}
				if (ShapePath.is_end_poly(cmd))
				{
					t.AddContour(vecs.ToArray());
					vecs.Clear();
				}
			} while (!ShapePath.is_stop(cmd));
			/*
            foreach (var v in vertices)
            {
                if (!ShapePath.is_close(v.command) && !ShapePath.is_stop(v.command))
                    vecs.Add(new Vector2((float)v.position.x, (float)v.position.y));
            }*/
			if (vecs.Count != 0)
				t.AddContour(vecs.ToArray());
			t.Tessellate(LibTessDotNet.WindingRule.NonZero, LibTessDotNet.ElementType.Polygons, 3);
			for (var i = 0; i < t.ElementCount; i++)
			{
				for (var tri = 0; tri < 3; tri++)
				{
					var v = t.Vertices[t.Elements[(i * 3) + tri]].Position;
					ret.Add(new Vertex(v.X, v.Y, co));
				}
			}
			return ret;
		}

		private Point topoint(Vector2 p)
		{
			return new Point(p.X, p.Y);
		}

		private Vector2 Vec2(Point p)
		{
			return new Vector2((float)p.X, (float)p.Y);
		}
	}
}