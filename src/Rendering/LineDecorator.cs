using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using linerider.Utils;
using linerider.Drawing;
using linerider.Game;
namespace linerider.Rendering
{
    internal class LineDecorator : IDisposable
    {
        private LineColorRenderer _linecolorrenderer;
        private LineAccelRenderer _lineaccelrenderer;
        private WellRenderer _wellrenderer;
        public LineDecorator()
        {
            _linecolorrenderer = new LineColorRenderer();
            _lineaccelrenderer = new LineAccelRenderer();
            _wellrenderer = new WellRenderer();
        }
        public void DrawUnder(DrawOptions options)
        {
            if (options.LineColors)
            {
                _lineaccelrenderer.Draw(options);
                _linecolorrenderer.Draw(options);
            }
            if (options.GravityWells)
            {
                _wellrenderer.Draw(options);
            }
        }
        public void Clear()
        {
            _linecolorrenderer.Clear();
            _lineaccelrenderer.Clear();
            _wellrenderer.Clear();
        }
        public void Initialize(AutoArray<GameLine> lines)
        {
            var red = new List<RedLine>(lines.Count / 4);
            foreach (var line in lines.unsafe_array)
            {
                if (line.Type == LineType.Red)
                    red.Add((RedLine)line);
            }
            _linecolorrenderer.Initialize(lines);
            _lineaccelrenderer.Initialize(red);
            _wellrenderer.Initialize(lines);
        }
        public void AddLine(StandardLine line)
        {
            _linecolorrenderer.AddLine(line);
            if (line is RedLine red)
                _lineaccelrenderer.AddLine(red);
            _wellrenderer.AddLine(line);
        }
        public void LineChanged(StandardLine line, bool hit = false)
        {
            if (!hit)
            {
                if (line is RedLine red)
                    _lineaccelrenderer.LineChanged(red, false);
                _linecolorrenderer.LineChanged(line, false);
            }
            else
            {
                if (line is RedLine red)
                    _lineaccelrenderer.RemoveLine(red);
                _linecolorrenderer.RemoveLine(line);
            }
            _wellrenderer.LineChanged(line);
        }
        public void RemoveLine(StandardLine line)
        {
            _linecolorrenderer.RemoveLine(line);
            if (line is RedLine red)
                _lineaccelrenderer.RemoveLine(red);
            _wellrenderer.RemoveLine(line);
        }
        public void Dispose()
        {
            _wellrenderer.Dispose();
            _linecolorrenderer.Dispose();
            _lineaccelrenderer.Dispose();
        }
    }
}