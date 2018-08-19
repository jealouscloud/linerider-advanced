using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
using linerider.Game;
using System.Diagnostics;

namespace linerider.Rendering
{
    public class LineRenderer : IDisposable
    {
        public const int StartingLineCount = 10000;
        public byte OverridePriority = 0;
        public Color OverrideColor = Color.Black;
        public KnobState KnobState = KnobState.Hidden;
        public bool Overlay = false;
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
        public void Clear()
        {
            _vertexcount = 0;
            _indices.Clear();
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
        public Dictionary<int, int> AddLines(AutoArray<GameLine> lines, LineVertex[] vertices)
        {
            Dictionary<int, int> ret = new Dictionary<int, int>(lines.Count);
            int startidx = _indices.Count;
            int startvert = _vertexcount;
            _indices.EnsureCapacity(vertices.Length);
            for (int ix = 0; ix < lines.Count; ix++)
            {
                var baseoffset = (ix * linesize);
                for (int i = 0; i < linesize; i++)
                {
                    _indices.Add(startvert + baseoffset + i);
                }
                ret.Add(lines[ix].ID, startidx + baseoffset);
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
            if (IsNulled(ibo_index))
                return;
            int vertstart = _indices.unsafe_array[ibo_index];
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
            freevertices.Enqueue(vertstart);
        }
        public void ChangeLine(int ibo_index, LineVertex[] line)
        {
            if (line.Length != linesize)
                throw new Exception(
                    "Lines are expected to have " + linesize + " vertices");
            int vertbase = _indices.unsafe_array[ibo_index];
            _vbo.Bind();
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
            var in_color = _shader.GetAttrib("in_color");
            var in_circle = _shader.GetAttrib("in_circle");
            var in_selectflags = _shader.GetAttrib("in_selectflags");
            var in_linesize = _shader.GetAttrib("in_linesize");
            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_color);
            GL.EnableVertexAttribArray(in_circle);
            GL.EnableVertexAttribArray(in_selectflags);
            GL.EnableVertexAttribArray(in_linesize);
            int counter = 0;
            GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, LineVertex.Size, counter);
            counter += 8;
            GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, LineVertex.Size, counter);
            counter += 4;
            GL.VertexAttribPointer(in_circle, 2, VertexAttribPointerType.Byte, false, LineVertex.Size, counter);
            counter += 2;
            GL.VertexAttribPointer(in_selectflags, 1, VertexAttribPointerType.Byte, false, LineVertex.Size, counter);
            counter += 2;
            GL.VertexAttribPointer(in_linesize, 2, VertexAttribPointerType.Float, false, LineVertex.Size, counter);
            counter += 8;
            var global = OverrideColor;
            if (!Overlay)
                GL.Uniform4(_shader.GetUniform("u_color"),
                    global.R / 255f, global.G / 255f, global.B / 255f, OverridePriority / 255f);
            GL.Uniform1(_shader.GetUniform("u_scale"), Scale);
            GL.Uniform1(_shader.GetUniform("u_alphachannel"), 0);
            GL.Uniform1(_shader.GetUniform("u_overlay"), Overlay ? 1 : 0);
            GL.Uniform1(_shader.GetUniform("u_knobstate"), (int)KnobState);
            GL.Uniform4(_shader.GetUniform("u_knobcolor"), Constants.DefaultKnobColor);

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
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_vertex"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_color"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_circle"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_selectflags"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_linesize"));
            _shader.Stop();
            _vbo.Unbind();
        }
        private void EnsureVBOSize(int size)
        {
            if (size > _vbo.BufferSize)
            {
                _vbo.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        private void EnsureIBOSize(int size)
        {
            if (size > _ibo.BufferSize)
            {
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
        public static LineVertex[] CreateTrackLine(Vector2d lnstart, Vector2d lnend, float size, int color = 0, byte selectflags = 0)
        {
            var d = lnend - lnstart;
            var rad = Angle.FromVector(d);
            var c = new Vector2d(rad.Cos, rad.Sin);
            //create line cap ends
            lnstart += c * (-1 * (size / 2));
            lnend += c * (1 * (size / 2));

            return CreateLine(lnstart, lnend, size, rad, color, selectflags);
        }
        public static LineVertex[] CreateLine(Vector2d lnstart, Vector2d lnend, float size, int color = 0, byte selectflags = 0)
        {
            var d = lnend - lnstart;
            var rad = Angle.FromVector(d);

            return CreateLine(lnstart, lnend, size, rad, color, selectflags);
        }
        public static LineVertex[] CreateLine(Vector2d lnstart, Vector2d lnend, float size, Angle angle, int color = 0, byte selectflags = 0)
        {
            LineVertex[] ret = new LineVertex[6];
            var start = (Vector2)lnstart;
            var end = (Vector2)lnend;
            var len = (end - start).Length;

            var l = Utility.GetThickLine(start, end, angle, size);
            var scale = size / 2;
            ret[0] = new LineVertex() { Position = l[0], u = 0, v = 0, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };
            ret[1] = new LineVertex() { Position = l[1], u = 0, v = 1, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };
            ret[2] = new LineVertex() { Position = l[2], u = 1, v = 1, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };

            ret[3] = new LineVertex() { Position = l[2], u = 1, v = 1, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };
            ret[4] = new LineVertex() { Position = l[3], u = 1, v = 0, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };
            ret[5] = new LineVertex() { Position = l[0], u = 0, v = 0, ratio = size / len, color = color, scale = scale, selectionflags = selectflags };
            return ret;
        }
    }
}