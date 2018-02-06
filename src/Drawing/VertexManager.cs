using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using linerider.Utils;
namespace linerider.Drawing
{
    /// <summary>
    /// Small helper class for automatically freeing unused vertices in a vbo
    /// </summary>
    internal class VertexManager<T>
    where T : struct
    {
        public int WastedIndices { get; private set; }
        Queue<int> _availablevertices = new Queue<int>();
        private VBO<T> _vbo;
        public VertexManager(VBO<T> vbo)
        {
            _vbo = vbo;
        }
        /// <summary>
        /// Adds a triangle in any 3 free vertices or allocates some.
        /// </summary>
        /// <returns>the start index of the triangle</returns>
        public int AddVertex(T a)
        {
            int idx;
            if (_availablevertices.Count != 0)
            {
                idx = _availablevertices.Dequeue();
                _vbo.SetVertex(idx, a);
            }
            else
            {
                idx = _vbo.AddVertex(a);
            }
            return idx;
        }
        public void SetVertex(int index, T vertex)
        {
            _vbo.SetVertex(index, vertex);
        }
        public void FreeVertices(int index, int count)
        {
            if (!_vbo.TryFreeVertices(index, count))
            {
                var empty = new T();
                _vbo.SetVertex(index, empty);
            }
        }
        public void FreeIndices(int index, int count)
        {
            if (!_vbo.TryFreeIndices(index, count))
            {
                WastedIndices++;
                _vbo.SetIndex(index, 0);
            }
        }
    }
}