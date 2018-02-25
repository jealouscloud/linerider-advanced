using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
using linerider.Lines;
using System.Diagnostics;

namespace linerider.Rendering
{
    public class LineRenderer : IDisposable
    {
        public const int StartingLineCount = 10000;
        public Color LineColor = Color.FromArgb(255, 0, 0xCC, 0);
        public KnobState KnobState = KnobState.Hidden;
        public float Scale = 1.0f;
        private Shader _shader;
        private GLBuffer<LineVertex> _vbo;
        private GLBuffer<int> _ibo;
        private AutoArray<int> _indices = new AutoArray<int>(StartingLineCount * linesize);
        private Queue<int> freevertices = new Queue<int>();
        private int _vertexcount = 0;
        const int linesize = 6;
        const int nullindex = 0;
        public LineRenderer(Shader sh)
        {
            if (sh == null)
                throw new ArgumentNullException("shader");
            _shader = sh;
            _vbo = new GLBuffer<LineVertex>(BufferTarget.ArrayBuffer);
            _ibo = new GLBuffer<int>(BufferTarget.ElementArrayBuffer);
            _vbo.Bind();
            _vbo.SetSize(StartingLineCount * linesize, BufferUsageHint.DynamicDraw);
            _vbo.Unbind();

            _ibo.Bind();
            _ibo.SetSize(StartingLineCount * linesize, BufferUsageHint.DynamicDraw);
            _ibo.Unbind();
        }
        /*/
        public void Defragment(Dictionary<int, int> dictionary)
        {
            var newdictionary = new Dictionary<int, int>();
            var newindices = new ArrayWrapper<int>(_indices.Count / 2);
            for (int ix = 0; ix < _indices.Count; ix += linesize)
            {
                if (!IsNulled(ix))
                {
                    newdictionary[ix] = _indices.Count;
                    for (int i = 0; i < linesize; i++)
                    {
                        newindices.Add(_indices.Arr[ix + i]);
                    }
                }
            }
            foreach (var kvp in dictionary)
            {
                if (newdictionary.TryGetValue(kvp.Value, out int old))
                {
                }
            }

            _indices = newindices;
            return;
        }*/
        public void Clear()
        {
            _vertexcount = 0;
            _indices.Empty();
            freevertices.Clear();
        }
        public void Dispose()
        {
            _vbo.Dispose();
            _ibo.Dispose();
        }
        private int GetVertexBase()
        {
            if (freevertices.Count != 0)
                return freevertices.Dequeue();
            int ret = _vertexcount;
            _vertexcount += linesize;
            EnsureVBOSize(_vertexcount);
            return ret;
        }
        public Dictionary<int, int> AddLines(List<GameLine> lines, Color color)
        {
            Dictionary<int, int> ret = new Dictionary<int, int>(lines.Count);
            LineVertex[] vertices = new LineVertex[lines.Count * linesize];
            int rgba = Utility.ColorToRGBA_LE(color);
            int startidx = _indices.Count;
            int startvert = _vertexcount;
            for (int ix = 0; ix < lines.Count; ix++)
            {
                var baseoffset = (ix * linesize);
                var line = lines[ix];
                float width = 2 * line.Width;

                var lineverts = CreateTrackLine(
                    line.Position,
                    line.Position2,
                    width,
                    rgba);
                for (int i = 0; i < linesize; i++)
                {
                    _indices.Add(startvert + baseoffset + i);
                    vertices[baseoffset + i] = lineverts[i];
                }
                ret.Add(line.ID, startidx + baseoffset);
            }
            _vbo.Bind();
            EnsureVBOSize(_vertexcount + vertices.Length);
            _vbo.SetData(vertices, 0, _vertexcount, vertices.Length);
            _vbo.Unbind();
            _vertexcount += vertices.Length;

            _ibo.Bind();
            EnsureIBOSize(_indices.Count + vertices.Length);
            _ibo.SetData(_indices.unsafe_array, startidx, startidx, vertices.Length);
            _ibo.Unbind();
            return ret;
        }
        /// <summary>
        /// Adds the specified 6 vertex line
        /// </summary>
        /// <returns>the first index in the ibo used</returns>
        public int AddLine(LineVertex[] line)
        {
            if (line.Length != linesize)
                throw new Exception(
                    "Lines are expected to have " + linesize + " vertices");
            _vbo.Bind();
            _ibo.Bind();
            var vertbase = GetVertexBase();
            int ret = _indices.Count;
            _vbo.SetData(line, 0, vertbase, linesize);
            for (int i = 0; i < linesize; i++)
            {
                _indices.Add(vertbase + i);
            }
            EnsureIBOSize(_indices.Count);
            _ibo.SetData(
                _indices.unsafe_array,
                _indices.Count - linesize,
                _indices.Count - linesize,
                linesize);
            _vbo.Unbind();
            _ibo.Unbind();
            return ret;
        }
        public void RemoveLine(int ibo_index)
        {
            int vertstart = _indices.unsafe_array[ibo_index];
            bool alreadyremoved = IsNulled(ibo_index);
            for (int i = 0; i < linesize; i++)
            {
                _indices.unsafe_array[ibo_index + i] = nullindex;
            }
            _ibo.Bind();
            _ibo.SetData(
                _indices.unsafe_array,
                ibo_index,
                ibo_index,
                linesize);
            _ibo.Unbind();
            if (alreadyremoved)
            {
                // Debug.WriteLine("linerenderer remove line thats nulled" + ibo_index);
                return;
            }
            freevertices.Enqueue(vertstart);
            // we dont empty from the vbo cause theres no need, the ibo doesnt
            // point to it and we might need the space later
        }
        public void ChangeLine(int ibo_index, LineVertex[] line)
        {
            if (line.Length != linesize)
                throw new Exception(
                    "Lines are expected to have " + linesize + " vertices");
            int vertbase = _indices.unsafe_array[ibo_index];
            _vbo.Bind();
            /// nulled out
            bool wasremoved = false;
            if (IsNulled(ibo_index))
            {
                vertbase = GetVertexBase();
                wasremoved = true;
            }
            _vbo.SetData(line, 0, vertbase, linesize);
            if (wasremoved)
            {
                for (int i = 0; i < linesize; i++)
                {
                    _indices.unsafe_array[ibo_index + i] = vertbase + i;
                }
                _ibo.Bind();
                _ibo.SetData(
                    _indices.unsafe_array,
                    ibo_index,
                    ibo_index,
                    linesize);
                _ibo.Unbind();
            }
            _vbo.Unbind();
        }
        protected void BeginDraw()
        {
            _vbo.Bind();
            _shader.Use();
            var in_vertex = _shader.GetAttrib("in_vertex");
            var in_circle = _shader.GetAttrib("in_circle");
            var in_ratio = _shader.GetAttrib("in_ratio");
            var in_color = _shader.GetAttrib("in_color");
            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_circle);
            GL.EnableVertexAttribArray(in_ratio);
            GL.EnableVertexAttribArray(in_color);
            GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, LineVertex.Size, 0);
            GL.VertexAttribPointer(in_circle, 2, VertexAttribPointerType.Float, false, LineVertex.Size, 8);
            GL.VertexAttribPointer(in_ratio, 1, VertexAttribPointerType.Float, false, LineVertex.Size, 8 + 8);
            GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, LineVertex.Size, 8 + 8 + 4);
            var global = LineColor;
            var u_color = _shader.GetUniform("u_color");
            var u_scale = _shader.GetUniform("u_scale");
            var u_knobstate = _shader.GetUniform("u_knobstate");
            GL.Uniform4(u_color, global.R / 255f, global.G / 255f, global.B / 255f, global.A / 255f);
            GL.Uniform1(u_scale, Scale);
            GL.Uniform1(_shader.GetUniform("u_alphachannel"), 0);
            GL.Uniform1(u_knobstate, (int)KnobState);

        }
        public void Draw()
        {
            if (_indices.Count == 0)
                return;
            BeginDraw();
            _ibo.Bind();
            using (new GLEnableCap(EnableCap.Blend))
            {
                GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
            }
            _ibo.Unbind();
            EndDraw();
        }
        protected void EndDraw()
        {
            var v = _shader.GetAttrib("in_vertex");
            var circle = _shader.GetAttrib("in_circle");
            var ratio = _shader.GetAttrib("in_ratio");
            var in_color = _shader.GetAttrib("in_color");

            GL.DisableVertexAttribArray(in_color);
            GL.DisableVertexAttribArray(v);
            GL.DisableVertexAttribArray(circle);
            GL.DisableVertexAttribArray(ratio);
            _shader.Stop();
            _vbo.Unbind();
        }
        private void EnsureVBOSize(int size)
        {
            if (size > _vbo.BufferSize)
            {
                // double the buffer size. this is expensive, so avoid doing it
                // as much as possible
                _vbo.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        private void EnsureIBOSize(int size)
        {
            if (size > _ibo.BufferSize)
            {
                // double the buffer size. this is expensive, so avoid doing it
                // as much as possible
                _ibo.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        /// <summary>
        /// checks if a line in the index buffer was 'removed'
        /// basically nulled out
        /// </summary>
        private bool IsNulled(int index)
        {
            for (int i = 0; i < linesize; i++)
            {
                if (_indices.unsafe_array[index + i] != nullindex)
                    return false;
            }
            return true;
        }
        public static LineVertex[] CreateTrackLine(Vector2d lnstart, Vector2d lnend, float size, int color = 0)
        {
            var d = lnend - lnstart;
            var rad = Angle.FromVector(d);
            var c = new Vector2d(rad.Cos, rad.Sin);
            //create line cap ends
            lnstart += c * (-1 * (size / 2));
            lnend += c * (1 * (size / 2));

            return CreateLine(lnstart, lnend, size, rad, color);
        }
        public static LineVertex[] CreateLine(Vector2d lnstart, Vector2d lnend, float size, int color = 0)
        {
            var d = lnend - lnstart;
            var rad = Angle.FromVector(d);

            return CreateLine(lnstart, lnend, size, rad, color);
        }
        public static LineVertex[] CreateLine(Vector2d lnstart, Vector2d lnend, float size, Angle angle, int color = 0)
        {
            LineVertex[] ret = new LineVertex[6];
            var start = (Vector2)lnstart;
            var end = (Vector2)lnend;
            var len = (end - start).Length;

            var l = Utility.GetThickLine(start, end, angle, size);
            ret[0] = new LineVertex() { Position = l[0], circle_uv = new Vector2(0, 0), ratio = size / len, color = color };
            ret[1] = new LineVertex() { Position = l[1], circle_uv = new Vector2(0, 1), ratio = size / len, color = color };
            ret[2] = new LineVertex() { Position = l[2], circle_uv = new Vector2(1, 1), ratio = size / len, color = color };

            ret[3] = new LineVertex() { Position = l[2], circle_uv = new Vector2(1, 1), ratio = size / len, color = color };
            ret[4] = new LineVertex() { Position = l[3], circle_uv = new Vector2(1, 0), ratio = size / len, color = color };
            ret[5] = new LineVertex() { Position = l[0], circle_uv = new Vector2(0, 0), ratio = size / len, color = color };
            return ret;
        }
    }
}