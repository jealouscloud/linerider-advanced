using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
namespace linerider.Rendering
{
    public class WellRenderer : IDisposable
    {
        private GLBuffer<GenericVertex> _vbo;
        private Dictionary<int, int> _lines = new Dictionary<int, int>();
        private int _vertexcounter = 0;
        const int wellsize = 6;
        public WellRenderer()
        {
            _vbo = new GLBuffer<GenericVertex>(BufferTarget.ArrayBuffer);
            _vbo.Bind();
            _vbo.SetSize(
                LineRenderer.StartingLineCount * wellsize,
                BufferUsageHint.DynamicDraw);
            _vbo.Unbind();
        }
        public void Clear()
        {
            _lines.Clear();
            _vertexcounter = 0;
        }
        public void Draw(DrawOptions draw)
        {
            using (new GLEnableCap(EnableCap.Blend))
            {
                _vbo.Bind();
                GL.EnableClientState(ArrayCap.VertexArray);
                GL.EnableClientState(ArrayCap.ColorArray);
                GL.VertexPointer(2, VertexPointerType.Float, GenericVertex.Size, 0);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, GenericVertex.Size, 8);
                GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexcounter);
                GL.DisableClientState(ArrayCap.ColorArray);
                GL.DisableClientState(ArrayCap.VertexArray);
                _vbo.Unbind();
            }
        }
        public void AddLine(StandardLine line)
        {
            if (_lines.ContainsKey(line.ID))
            {
                LineChanged(line);
                return;
            }
            var well = GetWell(line);
            _vbo.Bind();
            var vertexbase = GetVertexBase();
            _vbo.SetData(well, 0, vertexbase, wellsize);
            _vbo.Unbind();
            _lines.Add(line.ID, vertexbase);
        }
        public void LineChanged(StandardLine line)
        {
            var well = GetWell(line);
            var vertexbase = _lines[line.ID];
            _vbo.Bind();
            _vbo.SetData(well, 0, vertexbase, wellsize);
            _vbo.Unbind();
        }
        public void RemoveLine(StandardLine line)
        {
            var vertexbase = _lines[line.ID];
            var empty = new GenericVertex[wellsize];
            _vbo.Bind();
            _vbo.SetData(empty, 0, vertexbase, wellsize);
            _vbo.Unbind();
        }
        private int GetVertexBase()
        {
            int ret = _vertexcounter;
            _vertexcounter += wellsize;
            EnsureVBOSize(_vertexcounter);
            return ret;
        }
        private void EnsureVBOSize(int size)
        {
            if (size > _vbo.BufferSize)
            {
                _vbo.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        public void Dispose()
        {
            _vbo.Dispose();
        }

        private static GenericVertex[] GetWell(StandardLine line)
        {
            var t = StaticRenderer.CalculateLine(Vector2d.Zero, Angle.FromVector(line.DiffNormal), line.inv ? -StandardLine.Zone : StandardLine.Zone);
            var wellcolor = Color.FromArgb(40, 0, 0, 0);
            var tl = (new GenericVertex((Vector2)(line.Position), wellcolor));
            var tr = (new GenericVertex((Vector2)(line.Position2), wellcolor));
            var bl = (new GenericVertex((Vector2)(line.Position + t), wellcolor));
            var br = (new GenericVertex((Vector2)(line.Position2 + t), wellcolor));
            return new GenericVertex[] { tl, tr, bl, br, bl, tr };
        }
    }
}