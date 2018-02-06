using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using linerider.Utils;
using linerider.Drawing;
namespace linerider.Rendering
{
    internal class LineDecorator
    {
        private struct lineentry
        {
            public int colorindex;
            public int gwellindex;
            public int accelstart;
            public int accelcount;
        }
        private TrackVBO _linecolorvbo;
        private GenericVBO _accelvbo;
        private GenericVBO _gwellvbo;
        private VertexManager<GenericVertex> _gwellvertman;
        private VertexManager<GenericVertex> _accelvertman;
        private Dictionary<int, lineentry> _lines = new Dictionary<int, lineentry>();
        public LineDecorator()
        {
            _linecolorvbo = new TrackVBO(TrackRenderer.LineShader, false);
            _linecolorvbo.LineColor = Color.FromArgb(0);
            _gwellvbo = new GenericVBO(true, false);
            _accelvbo = new GenericVBO(true, false);
            _gwellvertman = new VertexManager<GenericVertex>(_gwellvbo);
            _accelvertman = new VertexManager<GenericVertex>(_accelvbo);
        }
        public void Draw(DrawOptions options)
        {
            _linecolorvbo.Scale = options.Zoom;
            if (options.LineColors)
            {
                _accelvbo.Draw(PrimitiveType.Triangles);
                _linecolorvbo.Draw(PrimitiveType.Triangles);
            }
            if (options.GravityWells)
            {
                _gwellvbo.Draw(PrimitiveType.Triangles);
            }
        }
        public void Clear()
        {
            _linecolorvbo.Clear();
            _accelvbo.Clear();
            _gwellvbo.Clear();
            _gwellvertman = new VertexManager<GenericVertex>(_gwellvbo);
            _accelvertman = new VertexManager<GenericVertex>(_accelvbo);
            _lines.Clear();
        }
        public void AddLine(StandardLine line)
        {
            var ltype = line.GetLineType();
            var coloredline = CreateDecorationLine(line);
            var linecolor = line.GetColor();
            int colorstart = -1;
            for (int i = 0; i < coloredline.Length; i++)
            {
                var index = _linecolorvbo.AddVertex(coloredline[i], linecolor);
                if (colorstart == -1)
                    colorstart = index;
            }
            var t = StaticRenderer.CalculateLine(Vector2d.Zero, Angle.FromVector(line.DiffNormal), line.inv ? -StandardLine.Zone : StandardLine.Zone);
            var wellcolor = Color.FromArgb(40, 0, 0, 0);
            var tl = _gwellvertman.AddVertex(new GenericVertex((Vector2)(line.Position), wellcolor));
            var tr = _gwellvertman.AddVertex(new GenericVertex((Vector2)(line.Position2), wellcolor));
            var bl = _gwellvertman.AddVertex(new GenericVertex((Vector2)(line.Position + t), wellcolor));
            var br = _gwellvertman.AddVertex(new GenericVertex((Vector2)(line.Position2 + t), wellcolor));
            int gwellstart =
            _gwellvbo.AddIndex(tl);
            _gwellvbo.AddIndex(tr);
            _gwellvbo.AddIndex(bl);

            _gwellvbo.AddIndex(br);
            _gwellvbo.AddIndex(bl);
            _gwellvbo.AddIndex(tr);

            int accelstart = -1;
            int accelcount = 0;
            if (ltype == LineType.Red)
            {
                var accel = GetAccelDecor((RedLine)line);
                foreach (var v in accel)
                {
                    var vertid = _accelvertman.AddVertex(v);
                    int idxid = _accelvbo.AddIndex(vertid);
                    if (accelstart == -1)
                    {
                        accelstart = idxid;
                    }
                }
                accelcount = accel.Length;
            }
            lineentry entry = new lineentry() { colorindex = colorstart, gwellindex = gwellstart, accelstart = accelstart, accelcount = accelcount };
            _lines.Add(line.ID, entry);
        }
        public void LineChanged(StandardLine line)
        {
            var l = _lines[line.ID];
            bool entrychanged = false;
            var coloredline = CreateDecorationLine(line);
            var color = line.GetColor();
            for (int i = 0; i < 6; i++)
            {
                _linecolorvbo.SetVertex(l.colorindex + i, coloredline[i], color);
            }

            var t = StaticRenderer.CalculateLine(Vector2d.Zero, Angle.FromVector(line.DiffNormal), line.inv ? -StandardLine.Zone : StandardLine.Zone);
            var c = Color.FromArgb(40, 0, 0, 0);
            var tl = new GenericVertex((Vector2)(line.Position), c);
            var tr = new GenericVertex((Vector2)(line.Position2), c);
            var bl = new GenericVertex((Vector2)(line.Position + t), c);
            var br = new GenericVertex((Vector2)(line.Position2 + t), c);
            //order based on addline, sloppily not standardizing
            _gwellvertman.SetVertex(_gwellvbo.GetIndex(l.gwellindex + 0), tl);
            _gwellvertman.SetVertex(_gwellvbo.GetIndex(l.gwellindex + 1), tr);
            _gwellvertman.SetVertex(_gwellvbo.GetIndex(l.gwellindex + 2), bl);
            _gwellvertman.SetVertex(_gwellvbo.GetIndex(l.gwellindex + 3), br);

            var ltype = line.GetLineType();
            if (ltype == LineType.Red)
            {
                var newaccel = GetAccelDecor((RedLine)line);
                if (l.accelstart != -1 && l.accelcount != newaccel.Length)
                {
                    //free verts first 
                    _accelvertman.FreeVertices(_accelvbo.GetIndex(l.accelstart), l.accelcount);
                    _accelvertman.FreeIndices(l.accelstart, l.accelcount);
                    //now reallocate
                    bool firstindex = true;
                    foreach (var v in newaccel)
                    {
                        var vertid = _accelvertman.AddVertex(v);
                        int idxid = _accelvbo.AddIndex(vertid);
                        if (firstindex)
                        {
                            firstindex = false;
                            l.accelstart = idxid;
                        }
                    }
                    l.accelcount = newaccel.Length;
                    //dont forget to update the dictionary
                    entrychanged = true;
                }
                else
                {
                    //we can just update them
                    for (int i = 0; i < l.accelcount; i++)
                    {
                        _accelvertman.SetVertex(_accelvbo.GetIndex(l.accelstart + i), newaccel[i]);
                    }
                }
            }
            if (entrychanged)
            {
                _lines[line.ID] = l;
            }
        }
        public void RemoveLine(StandardLine line)
        {
            var l = _lines[line.ID];
            var empty = new LineVertex();
            for (int i = 0; i < 6; i++)
            {
                _linecolorvbo.SetVertex(l.colorindex + i, empty);
            }
            _gwellvertman.FreeVertices(_gwellvbo.GetIndex(l.gwellindex), 6);
            _gwellvertman.FreeIndices(l.gwellindex, 6);
            if (l.accelstart != -1)
            {
                _accelvertman.FreeVertices(_accelvbo.GetIndex(l.accelstart), l.accelcount);
                _accelvertman.FreeIndices(l.accelstart, l.accelcount);
            }
        }
        private LineVertex[] CreateDecorationLine(StandardLine line)
        {
            var ltype = line.GetLineType();
            var slant = new Vector2d(
                line.DiffNormal.X > 0 ? Math.Ceiling(line.DiffNormal.X) : Math.Floor(line.DiffNormal.X),
                line.DiffNormal.Y > 0 ? Math.Ceiling(line.DiffNormal.Y) : Math.Floor(line.DiffNormal.Y));
            return TrackVBO.CreateTrackLine(line.Position + slant, line.Position2 + slant, 2);
        }
        private GenericVertex[] GetAccelDecor(RedLine line)
        {
            var linecolor = Line.RedLineColor;
            var multiplier = ((RedLine)line).Multiplier;
            GenericVertex[] ret = new GenericVertex[3 * multiplier];
            for (int ix = 0; ix < multiplier; ix++)
            {
                var angle = MathHelper.RadiansToDegrees(Math.Atan2((double)line.diff.Y, (double)line.diff.X));
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