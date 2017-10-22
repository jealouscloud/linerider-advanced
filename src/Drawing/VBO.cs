//
//  VBO.cs
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

using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;

using System.Drawing;
namespace linerider.Drawing
{
    public class VBO : IDisposable
    {
		private Vertex[] vertices = new Vertex[500];
		private int[] indices = new int[500];
		private int vCount = 0;
		private int iCount = 0;
        private List<byte> alphas = new List<byte>();
		private List<int> ChangedVerticies = new List<int>();
		private List<int> ChangedIndices = new List<int>();
        private bool _reloadVertices = true;
		private bool _reloadIndices = true;
        private int _vbo;
        private int _ibo;
        public readonly bool Indexed = false;
        public int Texture = 0;
        public bool Opacity = false;
        public bool Locking = false;
        private object SyncRoot = new object();
        private float _opacity = 1.0f;
        public float GetOpacity
        {
            get
            {
                return _opacity;
            }
        }
		public int IndexCount
		{
			get
			{
				lock (SyncRoot)
				{
					return iCount;
				}
			}
			set
			{
				lock (SyncRoot)
				{
					iCount = value;
				}
			}
		}
        public VBO(bool indexed, bool useopacity)
        {
            Opacity = useopacity;
            Indexed = indexed;
            GL.GenBuffers(1, out _vbo);
            GL.GenBuffers(1, out _ibo);
        }
		public void Dispose()
        {
			lock (SyncRoot)
			{
				GL.DeleteBuffer(_vbo);
				GL.DeleteBuffer(_ibo);
			}
        }
		public void EnsureIndexSize(int size)
        {
			lock (SyncRoot)
			{
				if (Indexed)
				{
					if (size >= indices.Length)
					{
						Array.Resize(ref indices, size + (size / 2));
						_reloadIndices = true;
					}
				}
			}
        }
        public void EnsureVertexSize(int size)
        {
			lock(SyncRoot)
			{
				if (size >= vertices.Length)
				{
					Array.Resize(ref vertices, size + (size / 2));
					_reloadVertices = true;
				}
			}
        }

        public void SetOpacity(float opacity)
        {
            lock (SyncRoot)
			{
				if (!Opacity)
					throw new InvalidOperationException("Opacity isnt supported in this vao");
				if (_opacity == opacity)
					return;
				if (alphas == null || alphas.Count == 0)
				{
					alphas = new List<byte>(vCount);
					for (int i = 0; i < vCount; i++)
					{
						alphas.Add(vertices[i].a);
					}
				}
				for (int i = 0; i < vCount; i++)
				{
					var v = vertices[i];
					v.a = (byte)(Math.Min(255, alphas[i] * opacity));
					vertices[i] = v;
				}
				_opacity = opacity;
                UpdateVertices();
            }
        }

        public void SetVertex(int index, Vertex v)
		{
			lock (SyncRoot)
			{
				vertices[index] = v;
				ChangedVerticies.Add(index);
			}
		}
		public Vertex GetVertex(int index)
		{
			return vertices[index];
		}
        public void SetIndices(List<int> ind)
        {
			lock (SyncRoot)
			{
				if (!Indexed)
					throw new InvalidOperationException("Non indexed VBO");
				if (ind == null)
					ind = new List<int>();
				iCount = 0;
				EnsureIndexSize(ind.Count);
				foreach (var v in ind)
				{
					indices[iCount++] = v;
				}
				_reloadIndices = true;
			}
        }

        public void SetIndex(int index, int index2)
        {
			lock (SyncRoot)
			{
				indices[index] = index2;
				ChangedIndices.Add(index);
			}
        }

        public int GetIndex(int index)
        {
            return indices[index];
        }
		public int AddIndex(int index)
		{
			lock (SyncRoot)
			{
				if (!Indexed)
					throw new InvalidOperationException("Non indexed VAO");
				EnsureIndexSize(iCount + 1);
				indices[iCount] = index;
				ChangedIndices.Add(iCount);
				return iCount++;
			}
		}
		public void UpdateIndices()
		{
			lock (SyncRoot)
			{
				ChangedIndices.Clear();
				_reloadIndices = true;
			}
		}
		public void UpdateVertices()
		{
			lock (SyncRoot)
			{
				ChangedVerticies.Clear();
				_reloadVertices = true;
			}
		}

        public int AddVertex(Vertex v)
        {
			lock (SyncRoot)
			{
				EnsureVertexSize(vCount + 1);

				vertices[vCount] = v;

				ChangedVerticies.Add(vCount);
				if (Opacity)
					alphas.Add(v.a);
				return vCount++;
			}
        }

        public void ClearIndices()
        {
			lock (SyncRoot)
			{
				iCount = 0;
			}
        }
        public void Clear()
        {
            lock (SyncRoot)
            {
                if (Opacity)
                    alphas.Clear();
				vCount = 0;
				iCount = 0;
				ChangedIndices.Clear();
				ChangedVerticies.Clear();
            }
        }

        public void Draw(PrimitiveType mode)
        {
            lock (SyncRoot)
            {
                using (new GLEnableCap(EnableCap.Texture2D))
                using (new GLEnableCap(EnableCap.Blend))
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                    if (Indexed)
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GLEnableCap blend = null;
                    GL.BindTexture(TextureTarget.Texture2D, Texture);
                    if (Texture != 0)
                    {
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    }
                    if (ChangedVerticies.Count != 0 && !_reloadVertices)
                    {
                        unsafe
                        {
                            foreach (var change in ChangedVerticies)
                            {
                                var vert = vertices[change];
                                GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(Vertex.Size * change),
                                    new IntPtr(Vertex.Size), ref vert);
                            }
                            ChangedVerticies.Clear();
                        }
                    }
                    if (ChangedIndices.Count != 0 && !_reloadIndices)
                    {
                        unsafe
                        {
                            foreach (var change in ChangedIndices)
                            {
                                var ind = indices[change];
                                GL.BufferSubData(BufferTarget.ElementArrayBuffer, new IntPtr(sizeof(int) * change), new IntPtr(sizeof(int)), ref ind);
                            }
                            ChangedIndices.Clear();
                        }
                    }
                    if (_reloadVertices)
                    {
                        ChangedVerticies.Clear();
                        GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertices.Length * Vertex.Size), vertices, BufferUsageHint.DynamicDraw);
                        _reloadVertices = false;
                    }

                    if (_reloadIndices && Indexed)
                    {
                        ChangedIndices.Clear();
                        GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indices.Length * sizeof(int)), indices, BufferUsageHint.StreamDraw);
                        _reloadIndices = false;
                    }
                    if (vCount != 0 && (!Indexed || iCount != 0))
                    {
                        GL.VertexPointer(2, VertexPointerType.Float, Vertex.Size, 0);
                        GL.ColorPointer(4, ColorPointerType.UnsignedByte, Vertex.Size, 8);
                        GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.Size, 12);
                        if (Indexed)
                            GL.DrawElements(mode, iCount, DrawElementsType.UnsignedInt, 0);
                        else
                            GL.DrawArrays(mode, 0, vCount);
                    }
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                    GL.DisableClientState(ArrayCap.TextureCoordArray);
                    GL.DisableClientState(ArrayCap.ColorArray);
                    GL.DisableClientState(ArrayCap.VertexArray);
                    if (blend != null)
                    {
                        blend.Dispose();
                    }
                }
            }
        }
    }
}