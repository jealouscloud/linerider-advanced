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
    internal class LineDecorator : IDisposable
    {        
        private LineColorRenderer _linecolorrenderer;
        private WellRenderer _wellrenderer;
        public LineDecorator()
        {
            _linecolorrenderer = new LineColorRenderer();
            _wellrenderer = new WellRenderer();
        }
        public void Draw(DrawOptions options)
        {
            if (options.LineColors)
            {
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
            _wellrenderer.Clear();
        }
        public void AddLine(StandardLine line)
        {
            _linecolorrenderer.AddLine(line);
            _wellrenderer.AddLine(line);
        }
        public void LineChanged(StandardLine line)
        {
            _linecolorrenderer.LineChanged(line);
            _wellrenderer.LineChanged(line);
        }
        public void RemoveLine(StandardLine line)
        {
            _linecolorrenderer.RemoveLine(line);
            _wellrenderer.RemoveLine(line);
        }
        public void Dispose()
        {
            _wellrenderer.Dispose();
            _linecolorrenderer.Dispose();
        }
    }
}