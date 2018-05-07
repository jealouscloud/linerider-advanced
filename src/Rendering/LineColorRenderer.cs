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
        private Dictionary<int, int> _lines = new Dictionary<int, int>();
        const int linesize = 6;
        private LineRenderer _linebuffer;
        public LineColorRenderer()
        {
            _linebuffer = new LineRenderer(Shaders.LineShader);
            _linebuffer.OverrideColor = Color.FromArgb(0);
            _linebuffer.OverridePriority = 0;
        }

        public void Initialize(AutoArray<GameLine> lines)
        {
            Clear();
            LineVertex[] vertices = new LineVertex[lines.Count * linesize];
            var redverts = new AutoArray<GenericVertex>((lines.Count / 2) * 3);
            System.Threading.Tasks.Parallel.For(0, lines.Count, (idx) =>
            {
                var line = (StandardLine)lines[idx];
                var lineverts = CreateDecorationLine(line, line.Color);
                for (int i = 0; i < lineverts.Length; i++)
                {
                    vertices[idx * 6 + i] = lineverts[i];
                }
            });
            var dict = _linebuffer.AddLines(lines, vertices);
            _lines = dict;
        }
        public void Draw(DrawOptions draw)
        {
            _linebuffer.Scale = draw.Zoom;
            _linebuffer.Draw();
        }
        public void Clear()
        {
            _lines.Clear();
            _linebuffer.Clear();
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
        }
        public void LineChanged(StandardLine line, bool hit)
        {
            var colorindex = _lines[line.ID];
            var color = line.GetColor();
            var lineverts = hit ? new LineVertex[6] : CreateDecorationLine(line, color);
            _linebuffer.ChangeLine(colorindex, lineverts);
        }
        public void RemoveLine(StandardLine line)
        {
            var colorindex = _lines[line.ID];
            _linebuffer.RemoveLine(colorindex);
        }
        public void Dispose()
        {
            _linebuffer.Dispose();
        }
        public static LineVertex[] CreateDecorationLine(StandardLine line, Color color)
        {
            var slant = new Vector2d(
                line.DiffNormal.X > 0 ? Math.Ceiling(line.DiffNormal.X) : Math.Floor(line.DiffNormal.X),
                line.DiffNormal.Y > 0 ? Math.Ceiling(line.DiffNormal.Y) : Math.Floor(line.DiffNormal.Y));
            return LineRenderer.CreateTrackLine(line.Position + slant, line.Position2 + slant, 2, Utility.ColorToRGBA_LE(color));
        }
    }
}