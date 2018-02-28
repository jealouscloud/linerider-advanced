using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
using linerider.Game;
namespace linerider.Rendering
{
    public class LineColorRenderer : IDisposable
    {
        const int ShapeSize = 3;
        private struct accelentry
        {
            public int start;
            public int shapes;
        }
        private AutoArray<int> _indices = new AutoArray<int>((LineRenderer.StartingLineCount / 4) * ShapeSize);
        private Dictionary<int, int> _lines = new Dictionary<int, int>();
        private Dictionary<int, accelentry> _accellines = new Dictionary<int, accelentry>();
        private Queue<int> _freeaccel = new Queue<int>();
        private int _accelcount = 0;
        const int nullindex = 0;

        private LineRenderer _linebuffer;
        private GLBuffer<GenericVertex> _accelbuffer;
        private GLBuffer<int> _accelibo;
        public LineColorRenderer()
        {
            _linebuffer = new LineRenderer(Shaders.LineShader);
            _linebuffer.LineColor = Color.FromArgb(0);
            _accelbuffer = new GLBuffer<GenericVertex>(BufferTarget.ArrayBuffer);
            _accelbuffer.Bind();
            _accelbuffer.SetSize((LineRenderer.StartingLineCount / 2) * 3, BufferUsageHint.DynamicDraw);
            _accelbuffer.Unbind();
            _accelibo = new GLBuffer<int>(BufferTarget.ElementArrayBuffer);
            _accelibo.Bind();
            _accelibo.SetSize((LineRenderer.StartingLineCount / 2) * 3, BufferUsageHint.DynamicDraw);
            _accelibo.Unbind();
        }

        private void DrawAccel()
        {
            _accelbuffer.Bind();

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.VertexPointer(2, VertexPointerType.Float, GenericVertex.Size, 0);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, GenericVertex.Size, 8);

            _accelibo.Bind();
            GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
            _accelibo.Unbind();

            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.VertexArray);

            _accelbuffer.Unbind();
        }
        public void Draw(DrawOptions draw)
        {
            DrawAccel();
            _linebuffer.Scale = draw.Zoom;
            _linebuffer.Draw();
        }
        public void Clear()
        {
            _lines.Clear();
            _linebuffer.Clear();

            _accelcount = 0;
            _indices.Empty();
            _freeaccel.Clear();
            _accellines.Clear();
        }
        public void AddLine(StandardLine line)
        {
            if (_lines.ContainsKey(line.ID))
            {
                LineChanged(line, false);
                return;
            }
            var color = line.GetColor();
            var lineverts = CreateDecorationLine(line, color);
            int start = _linebuffer.AddLine(lineverts);
            _lines.Add(line.ID, start);
            if (line is RedLine r)
            {
                _accellines.Add(line.ID, new accelentry()
                {
                    start = _indices.Count,
                    shapes = 0
                });
                DrawAccel(r);
            }
        }
        public void LineChanged(StandardLine line, bool hit)
        {
            var colorindex = _lines[line.ID];
            var color = line.GetColor();
            var lineverts = hit ? new LineVertex[6] : CreateDecorationLine(line, color);
            _linebuffer.ChangeLine(colorindex, lineverts);
            if (line is RedLine r)
            {
                DrawAccel(r);
            }
        }
        public void RemoveLine(StandardLine line)
        {
            var colorindex = _lines[line.ID];
            _linebuffer.RemoveLine(colorindex);

            if (line is RedLine r)
            {
                var accel = _accellines[line.ID];
                for (int ix = 0; ix < accel.shapes; ix++)
                {
                    int offset = accel.start + (ix * ShapeSize);
                    if (IsNulled(offset))
                    {
                        continue;//nulled out
                    }
                    _freeaccel.Enqueue(_indices.unsafe_array[offset]);
                    for (int i = 0; i < ShapeSize; i++)
                    {
                        _indices.unsafe_array[accel.start + (ix * ShapeSize) + i] = 0;
                    }
                }
                _accelibo.Bind();
                _accelibo.SetData(
                    _indices.unsafe_array,
                    accel.start,
                    accel.start,
                    accel.shapes * ShapeSize);
                _accelibo.Unbind();
            }
        }
        public void Dispose()
        {
            _linebuffer.Dispose();
            _accelbuffer.Dispose();
        }
        /// <summary>
        /// Redraws the red line accel indicator.
        /// </summary>
        private void DrawAccel(RedLine line)
        {
            var entry = _accellines[line.ID];
            var newdecor = GetAccelDecor(line);
            int shapes = newdecor.Length / ShapeSize;

            for (int ix = 0; ix < entry.shapes; ix++)
            {
                int offset = entry.start + (ix * ShapeSize);
                if (IsNulled(offset))
                {
                    continue;//nulled out
                }
                _freeaccel.Enqueue(_indices.unsafe_array[offset]);
                for (int i = 0; i < ShapeSize; i++)
                {
                    _indices.unsafe_array[offset + i] = 0;
                }
            }
            bool growing = shapes > entry.shapes;
            _accelbuffer.Bind();
            for (int ix = 0; ix < shapes; ix++)
            {
                var vertexbase = GetVertexBase();
                _accelbuffer.SetData(
                    newdecor,
                    ShapeSize * ix,
                    vertexbase,
                    ShapeSize);
                for (int i = 0; i < ShapeSize; i++)
                {
                    if (growing)
                    {
                        _indices.Add(vertexbase + i);
                    }
                    else
                    {
                        int offset = entry.start + (ix * ShapeSize) + i;
                        _indices.unsafe_array[offset] = vertexbase + i;
                    }
                }
            }
            _accelbuffer.Unbind();

            _accelibo.Bind();
            _accelibo.SetData(
                _indices.unsafe_array,
                entry.start,
                entry.start,
                entry.shapes * ShapeSize);
            if (growing)
            {
                int startindex = _indices.Count - (shapes * ShapeSize);
                EnsureIBOSize(_indices.Count);
                _accelibo.SetData(
                    _indices.unsafe_array,
                    startindex,
                    startindex,
                    shapes * ShapeSize);
                _accellines[line.ID] = new accelentry()
                {
                    shapes = shapes,
                    start = startindex
                };
            }
            _accelibo.Unbind();
        }
        private int GetVertexBase()
        {
            if (_freeaccel.Count != 0)
                return _freeaccel.Dequeue();
            int ret = _accelcount;
            _accelcount += ShapeSize;
            EnsureVBOSize(_accelcount);
            return ret;
        }
        private void EnsureVBOSize(int size)
        {
            if (size > _accelbuffer.BufferSize)
            {
                // double the buffer size. this is expensive, so avoid doing it
                // as much as possible
                _accelbuffer.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        private void EnsureIBOSize(int size)
        {
            if (size > _accelibo.BufferSize)
            {
                // double the buffer size. this is expensive, so avoid doing it
                // as much as possible
                _accelibo.SetSize(size * 2, BufferUsageHint.DynamicDraw);
            }
        }
        /// <summary>
        /// checks if a line in the index buffer was 'removed'
        /// basically nulled out
        /// </summary>
        private bool IsNulled(int index)
        {
            for (int i = 0; i < ShapeSize; i++)
            {
                if (_indices.unsafe_array[index + i] != nullindex)
                    return false;
            }
            return true;
        }
        private LineVertex[] CreateDecorationLine(StandardLine line, Color color)
        {
            var slant = new Vector2d(
                line.DiffNormal.X > 0 ? Math.Ceiling(line.DiffNormal.X) : Math.Floor(line.DiffNormal.X),
                line.DiffNormal.Y > 0 ? Math.Ceiling(line.DiffNormal.Y) : Math.Floor(line.DiffNormal.Y));
            return LineRenderer.CreateTrackLine(line.Position + slant, line.Position2 + slant, 2, Utility.ColorToRGBA_LE(color));
        }
        private GenericVertex[] GetAccelDecor(RedLine line)
        {
            var linecolor = Constants.RedLineColor;
            var multiplier = ((RedLine)line).Multiplier;
            GenericVertex[] ret = new GenericVertex[3 * multiplier];
            for (int ix = 0; ix < multiplier; ix++)
            {
                var angle = MathHelper.RadiansToDegrees(Math.Atan2((double)line.Difference.Y, (double)line.Difference.X));
                Turtle tort = new Turtle(line.Position2);
                var basex = 8 + (ix * 2);
                tort.Move(angle, -basex);
                ret[(ix * 3)] = new GenericVertex((float)tort.X, (float)tort.Y, linecolor);
                tort.Move(90, line.inv ? -8 : 8);
                ret[(ix * 3) + 1] = new GenericVertex((float)tort.X, (float)tort.Y, linecolor);
                tort.Point = line.Position2;
                tort.Move(angle, -(ix * 2));
                ret[(ix * 3) + 2] = new GenericVertex((float)tort.X, (float)tort.Y, linecolor);
            }
            return ret;
        }
    }
}