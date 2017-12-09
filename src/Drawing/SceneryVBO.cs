//
//  SceneryVBO.cs
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
	internal class SceneryVBO
	{
		private VBO _vbo = new VBO(true, false);
		Queue<int> availablevertices = new Queue<int>();
		Queue<int> linechangedvertices = new Queue<int>();
		public SceneryVBO()
		{
			_vbo.Texture = StaticRenderer.CircleTex;
			const int onemegabyte = 1024 * 1024;
			_vbo.EnsureVertexSize((onemegabyte * 4) / Vertex.Size);
			_vbo.EnsureIndexSize((onemegabyte * 2) / sizeof(int));
		}
		public void Clear()
		{
			_vbo.Clear();
			availablevertices = new Queue<int>();
			linechangedvertices = new Queue<int>();
		}
		public void Draw()
		{
			_vbo.Draw(PrimitiveType.Triangles);
		}
		public void FreeVertices(List<int> indices)
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
		public void LineChangedFreeVertices(List<int> indices)
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
		private List<int> DrawThickLine(Vector2 p, Vector2 p1, float width, Color c)
		{
			List<int> ret = new List<int>(6);
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
		private List<int> RenderCircle(Vector2 p, Color c, float radius, int segments)
		{
			List<int> ret = new List<int>(6);
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y - radius, c, 1, 1)));
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y + radius, c, 1, 0)));
			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y + radius, c, 0, 0)));

			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y + radius, c, 0, 0)));
			ret.Add(AddVertex(new Vertex(p.X - radius, p.Y - radius, c, 0, 1)));
			ret.Add(AddVertex(new Vertex(p.X + radius, p.Y - radius, c, 1, 1)));
			return ret;
		}
		public void Update()
		{
			_vbo.UpdateVertices();
			_vbo.UpdateIndices();
		}
		public List<int> DrawBasicTrackLine(Vector2 p1, Vector2 p2, Color linecolor)
		{
			List<int> ret = new List<int>(6 + 6 + 6);
			ret.AddRange(RenderCircle(p1, linecolor, 1, 20));
			ret.AddRange(RenderCircle(p2, linecolor, 1, 20));
			ret.AddRange(DrawThickLine(p1, p2, 2, linecolor));
			return ret;
		}
		public void SetIndices(List<int> indices)
		{
			_vbo.SetIndices(indices);
		}
		public void SetIndices(int start, List<int> indices)
		{
			for (int i = 0; i < indices.Count; i++)
			{
				_vbo.SetIndex(start+i, indices[i]);
			}
		}
		public int AddIndices(List<int> indices)
		{
			var ret = -1;
			foreach (var index in indices)
			{
				var res = _vbo.AddIndex(index);
				if (ret == -1)
					ret = res;
			}
			return ret;
		}
		public bool TryRemoveIndices(int start, int length)
		{
			if (start + length == _vbo.IndexCount)
			{
				_vbo.IndexCount -= length;
				return true;
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					_vbo.SetIndex(start + i, 0);//currently we preserve it in the even of an undo. currently useless with just green lines, but we will see
				}
				return false;
			}
			
		}
		private int AddVertex(Vertex v)
		{
			int index = 0;
			if (linechangedvertices.Count != 0)
			{
				index = linechangedvertices.Dequeue();
				_vbo.SetVertex(index, v);
			}
			if (availablevertices.Count != 0)
			{
				index = availablevertices.Dequeue();
				_vbo.SetVertex(index, v);
			}
			else
			{
				index = _vbo.AddVertex(v);
			}
			return index;
		}
	}
}
