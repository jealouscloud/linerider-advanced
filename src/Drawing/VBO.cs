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
    public class VBO<T> : IDisposable
    where T : struct
    {
        public readonly bool Indexed = false;
        protected const int default_count = 500;
        protected T[] vertices = new T[default_count];
        protected int[] indices = new int[default_count];
        protected int vCount = 0;
        protected int iCount = 0;

        private readonly int _vbo;
        private readonly int _ibo;
        private List<int> ChangedVerticies = new List<int>();
        private List<int> ChangedIndices = new List<int>();
        protected bool _reloadVertices = true;
        protected bool _reloadIndices = true;
        private int _stride = 0;
        public VBO(bool indexed, int vertexsize)
        {
            _stride = vertexsize;
            Indexed = indexed;
            GL.GenBuffers(1, out _vbo);
            if (indexed)
                GL.GenBuffers(1, out _ibo);
        }
        public virtual void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            if (Indexed)
                GL.DeleteBuffer(_ibo);
        }
        public void EnsureIndexSize(int size)
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
        public void EnsureVertexSize(int size)
        {
            if (size >= vertices.Length)
            {
                Array.Resize(ref vertices, size + (size / 2));
                _reloadVertices = true;
            }
        }


        public void SetVertex(int index, T v)
        {
            vertices[index] = v;
            ChangedVerticies.Add(index);
        }
        public T GetVertex(int index)
        {
            return vertices[index];
        }
        public void SetIndices(List<int> ind)
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
        /// <summary>
        /// Frees the specifed indices from the index buffer if theyre on the end.
        /// </summary>
        public bool TryFreeIndices(int index, int count)
        {
            if (!Indexed)
                throw new InvalidOperationException("Non indexed VBO");
            if (index + count >= iCount)
            {
                // we don't have to do any more than this because drawarrays takes a count param
                iCount = index;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Frees the specifed vertices from the vertex buffer if theyre on the end.
        /// </summary>
        public bool TryFreeVertices(int index, int count)
        {
            if (index + count >= vCount)
            {
                // we don't have to do any more than this because drawarrays takes a count param
                vCount = index;
                return true;
            }
            return false;
        }
        public void SetIndex(int index, int index2)
        {
            indices[index] = index2;
            ChangedIndices.Add(index);
        }

        public int GetIndex(int index)
        {
            return indices[index];
        }
        public int AddIndex(int index)
        {
            if (!Indexed)
                throw new InvalidOperationException("Non indexed VBO");
            EnsureIndexSize(iCount + 1);
            indices[iCount] = index;
            ChangedIndices.Add(iCount);
            return iCount++;
        }
        public void UpdateIndices()
        {
            ChangedIndices.Clear();
            _reloadIndices = true;

        }
        public void UpdateVertices()
        {
            ChangedVerticies.Clear();
            _reloadVertices = true;
        }

        public virtual int AddVertex(T v)
        {
            EnsureVertexSize(vCount + 1);

            vertices[vCount] = v;

            ChangedVerticies.Add(vCount);
            return vCount++;
        }
        public void SetBufferSize(int size)
        {
            if (Indexed)
            {
                EnsureIndexSize(size);
            }
            EnsureVertexSize(size);
        }
        public void ClearIndices()
        {
            iCount = 0;
        }
        public virtual void Clear()
        {
            if (vertices.Length > default_count)
                vertices = new T[default_count];
            if (indices.Length > default_count)
                indices = new int[default_count];
            vCount = 0;
            iCount = 0;
            ChangedIndices = new List<int>();
            ChangedVerticies = new List<int>();
        }
        protected virtual void BeginDraw()
        {
        }
        protected virtual void EndDraw()
        {
        }
        protected virtual void DrawGL(PrimitiveType mode)
        {
            if (Indexed)
                GL.DrawElements(mode, iCount, DrawElementsType.UnsignedInt, 0);
            else
                GL.DrawArrays(mode, 0, vCount);
        }
        public void Draw(PrimitiveType mode)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            if (Indexed)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);

            if (ChangedVerticies.Count != 0 && !_reloadVertices)
            {
                unsafe
                {
                    foreach (var change in ChangedVerticies)
                    {
                        var vert = vertices[change];
                        GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(_stride * change),
                            new IntPtr(_stride), ref vert);
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

                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertices.Length * _stride), vertices, BufferUsageHint.DynamicDraw);
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
                BeginDraw();
                DrawGL(mode);
                EndDraw();
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            if (Indexed)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
    }
}