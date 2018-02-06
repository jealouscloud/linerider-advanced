using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Rendering;

namespace linerider.Drawing
{
    public class TrackVBO : VBO<LineVertex>
    {
        /// <summary>
        /// a stupid struct for if i want to change the color format
        /// also rgba
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct color
        {
            public static readonly int Size = Marshal.SizeOf(typeof(color));
            public byte r;
            public byte g;
            public byte b;
            public byte a;
        }
        public Color LineColor = Color.FromArgb(255, 0, 0xCC, 0);
        public KnobState KnobState = KnobState.Hidden;
        public float Scale = 1.0f;
        private GLEnableCap _blendcap = null;
        private color[] colors = new color[default_count];
        private bool _reloadColors = true;
        private Shader shader;
        private int _colorbuffer;
        private List<int> _changedColors = new List<int>();
        public TrackVBO(Shader sh, bool indexed) : base(indexed, LineVertex.Size)
        {
            if (sh == null)
                throw new ArgumentNullException("shader");
            shader = sh;
            GL.GenBuffers(1, out _colorbuffer);
        }
        public override void Dispose()
        {
            GL.DeleteBuffer(_colorbuffer);
        }
        public int AddVertex(LineVertex v, Color color)
        {
            var idx = base.AddVertex(v);
            EnsureColorSize(idx + 1);
            _changedColors.Add(idx);
            colors[idx] = new color() { r = color.R, g = color.G, b = color.B, a = color.A };

            return idx;
        }
        public void SetVertex(int index, LineVertex v, Color color)
        {
            base.SetVertex(index, v);
            _changedColors.Add(index);
            colors[index] = new color() { r = color.R, g = color.G, b = color.B, a = color.A };

        }
        public override int AddVertex(LineVertex v)
        {
            return AddVertex(v, LineColor);
        }
        protected override void BeginDraw()
        {
            var in_vertex = shader.GetAttrib("in_vertex");
            var in_circle = shader.GetAttrib("in_circle");
            var in_ratio = shader.GetAttrib("in_ratio");
            var in_color = shader.GetAttrib("in_color");

            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_circle);
            GL.EnableVertexAttribArray(in_ratio);
            GL.EnableVertexAttribArray(in_color);

            GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, LineVertex.Size, 0);
            GL.VertexAttribPointer(in_circle, 2, VertexAttribPointerType.Float, false, LineVertex.Size, 8);
            GL.VertexAttribPointer(in_ratio, 1, VertexAttribPointerType.Float, false, LineVertex.Size, 8 + 8);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _colorbuffer);
            GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(int), 0);

            if (_reloadColors)
            {
                _changedColors.Clear();
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(colors.Length * color.Size), colors, BufferUsageHint.DynamicDraw);
                _reloadColors = false;
            }
            else if (_changedColors.Count != 0)
            {
                foreach (var change in _changedColors)
                {
                    var ind = colors[change];

                    GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(color.Size * change), new IntPtr(color.Size), ref ind);
                }
                _changedColors.Clear();
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            shader.Use();
            var global = LineColor;
            var u_color = shader.GetUniform("u_color");
            var u_scale = shader.GetUniform("u_scale");
            var u_knobstate = shader.GetUniform("u_knobstate");
            GL.Uniform4(u_color, global.R / 255f, global.G / 255f, global.B / 255f, global.A / 255f);
            GL.Uniform1(u_scale, Scale);
            GL.Uniform1(u_knobstate, (int)KnobState);
            _blendcap = new GLEnableCap(EnableCap.Blend);

            base.BeginDraw();
        }
        protected override void EndDraw()
        {
            var v = shader.GetAttrib("in_vertex");
            var circle = shader.GetAttrib("in_circle");
            var ratio = shader.GetAttrib("in_ratio");
            var in_color = shader.GetAttrib("in_color");

            GL.DisableVertexAttribArray(in_color);
            GL.DisableVertexAttribArray(v);
            GL.DisableVertexAttribArray(circle);
            GL.DisableVertexAttribArray(ratio);
            _blendcap?.Dispose();
            _blendcap = null;
            shader.Stop();
            base.EndDraw();
        }
        private void EnsureColorSize(int size)
        {
            if (size >= colors.Length)
            {
                Array.Resize(ref colors, size + (size / 2));
                _reloadColors = true;
            }
        }
        public static LineVertex[] CreateTrackLine(Vector2d lnstart, Vector2d lnend, float size)
        {
            var d = lnend - lnstart;
            var rad = Math.Atan2(d.Y, d.X);
            var c = new Vector2d(Math.Cos(rad), Math.Sin(rad));
            //create line cap ends
            lnstart += c * -1;
            lnend += c * 1;

            return CreateLine(lnstart, lnend, size);
        }
        public static LineVertex[] CreateLine(Vector2d lnstart, Vector2d lnend, float size)
        {
            LineVertex[] ret = new LineVertex[6];
            var start = (Vector2)lnstart;
            var end = (Vector2)lnend;
            var len = (end - start).Length;

            var l = StaticRenderer.GenerateThickLine(start, end, size);
            ret[1] = new LineVertex() { Position = l[0], circle_uv = new Vector2(0, 1), ratio = size / len };
            ret[0] = new LineVertex() { Position = l[1], circle_uv = new Vector2(1, 1), ratio = size / len };
            ret[2] = new LineVertex() { Position = l[2], circle_uv = new Vector2(1, 0), ratio = size / len };

            ret[3] = new LineVertex() { Position = l[3], circle_uv = new Vector2(1, 1), ratio = size / len };
            ret[4] = new LineVertex() { Position = l[2], circle_uv = new Vector2(0, 1), ratio = size / len };
            ret[5] = new LineVertex() { Position = l[0], circle_uv = new Vector2(1, 0), ratio = size / len };
            return ret;
        }
    }
}