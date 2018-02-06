//
//  TrackRenderer.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
    internal class TrackRenderer
    {
        public static Shader LineShader;
        public bool RequiresUpdate = true;

        private LineDecorator _decorator;
        private TrackVBO _simvbo;
        private TrackVBO _sceneryvbo;
        private VertexManager<LineVertex> _sceneryvertman;
        /// <summary>
        /// A dictionary of [line id] -> [index of first vertex]
        /// </summary>
        private Dictionary<int, int> _lines = new Dictionary<int, int>();

        // We have a seperate scenery vbo for explicitly rendering it under the sim one.
        /// <summary>
        /// A dictionary of [line id] -> [index of first IBO index]
        /// </summary>
        private Dictionary<int, int> _scenerylines = new Dictionary<int, int>();
        private ResourceSync _sync;
        public TrackRenderer()
        {
            _sync = new ResourceSync();
            if (LineShader == null)
            {
                LineShader = new Shader(GameResources.simline_vert, GameResources.simline_frag);
            }
            _simvbo = new TrackVBO(LineShader, false);
            _simvbo.LineColor = Color.Black;
            _decorator = new LineDecorator();

            _sceneryvbo = new TrackVBO(LineShader, true);
            _sceneryvbo.LineColor = Color.Black;
            _sceneryvertman = new VertexManager<LineVertex>(_sceneryvbo);
        }
        public void Render(Track track, DrawOptions options)
        {
            using (new GLEnableCap(EnableCap.Texture2D))
            {
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GameDrawingMatrix.Enter();
                _simvbo.Scale = options.Zoom;
                _simvbo.KnobState = options.KnobState;

                _sceneryvbo.Scale = options.Zoom;
                _sceneryvbo.KnobState = options.KnobState == KnobState.LifeLock ? KnobState.Shown : options.KnobState;//green lines dont get lifelock

                if (options.NightMode)
                {
                    _sceneryvbo.LineColor = Color.White;
                    _simvbo.LineColor = Color.White;
                }
                else
                {
                    _sceneryvbo.LineColor = Color.Black;
                    _simvbo.LineColor = Color.Black;
                }
                if (options.LineColors)
                {
                    _sceneryvbo.LineColor = Line.SceneryLineColor;
                }
                else
                {
                }
                _sceneryvbo.Draw(PrimitiveType.Triangles);
                _decorator.Draw(options);
                _simvbo.Draw(PrimitiveType.Triangles);
                GameDrawingMatrix.Exit();
            }
        }

        /// <summary>
        /// Updates viewport.
        /// </summary>
        /// <param name="lfines">Lines in current viewport, in the following order</param>
        /// <param name="colors"></param>
        /// <param name="knobstate"></param>
        public void InitializeTrack(IEnumerable<Line> vpu)
        {
            _simvbo.Clear();
            _decorator.Clear();
            _lines.Clear();
            _scenerylines.Clear();
            _lines.Clear();
            _sceneryvertman = new VertexManager<LineVertex>(_sceneryvbo);
            if (vpu != null)
            {
                foreach (var v in vpu)
                {
                    AddLine(v);
                }
            }
        }

        public void AddLine(Line line)
        {
            var type = line.GetLineType();
            switch (type)
            {
                case LineType.Blue:
                case LineType.Red:
                    AddSimLine((StandardLine)line);
                    break;
                case LineType.Scenery:
                    AddSceneryLine((SceneryLine)line);
                    break;
            }
        }
        public void RemoveLine(Line line)
        {
            var type = line.GetLineType();
            switch (type)
            {
                case LineType.Blue:
                case LineType.Red:
                    RemoveSimLine((StandardLine)line);
                    break;
                case LineType.Scenery:
                    RemoveSceneryLine((SceneryLine)line);
                    break;
            }
        }
        public void LineChanged(Line line)
        {
            var type = line.GetLineType();
            switch (type)
            {
                case LineType.Blue:
                case LineType.Red:
                    SimLineChanged((StandardLine)line);
                    break;
                case LineType.Scenery:
                    SceneryLineChanged((SceneryLine)line);
                    break;
            }
        }
        #region scenery
        private void AddSceneryLine(SceneryLine line)
        {
            if (_scenerylines.ContainsKey(line.ID))
            {
                System.Diagnostics.Debug.WriteLine("Line ID collision in scenery renderer");
                SceneryLineChanged(line);
                return;
            }
            var lineverts = TrackVBO.CreateTrackLine(line.Position, line.Position2, 2 * line.Width);
            int start = -1;
            for (int i = 0; i < lineverts.Length; i++)
            {
                var idx = _sceneryvbo.AddIndex(_sceneryvertman.AddVertex(lineverts[i]));
                if (start == -1)
                    start = idx;
            }
            _scenerylines.Add(line.ID, start);
        }
        private void SceneryLineChanged(SceneryLine line)
        {
            var lineverts = TrackVBO.CreateTrackLine(line.Position, line.Position2, 2);
            int start = _scenerylines[line.ID];
            for (int i = 0; i < lineverts.Length; i++)
            {
                _sceneryvbo.SetVertex(_sceneryvbo.GetIndex(start + i), lineverts[i]);
            }
        }
        private void RemoveSceneryLine(SceneryLine line)
        {
            var start = _scenerylines[line.ID];
            _sceneryvertman.FreeVertices(_sceneryvbo.GetIndex(start), 6);
            _sceneryvertman.FreeIndices(start, 6);
            _scenerylines.Remove(line.ID);
        }
        #endregion
        #region simulation
        private void AddSimLine(StandardLine line)
        {
            if (_lines.ContainsKey(line.ID))
            {
                System.Diagnostics.Debug.WriteLine("Line ID collision in sim renderer");
                LineChanged(line);
                return;
            }
            var lineverts = TrackVBO.CreateTrackLine(line.Position, line.Position2, 2);
            int start = -1;
            for (int i = 0; i < lineverts.Length; i++)
            {
                var index = _simvbo.AddVertex(lineverts[i], Color.Black);
                if (start == -1)
                    start = index;
            }
            _lines.Add(line.ID, start);
            _decorator.AddLine(line);
        }
        private void SimLineChanged(StandardLine line)
        {
            var lineverts = TrackVBO.CreateTrackLine(line.Position, line.Position2, 2);
            int start = _lines[line.ID];
            for (int i = 0; i < lineverts.Length; i++)
            {
                _simvbo.SetVertex(start + i, lineverts[i], Color.Black);
            }
            _decorator.LineChanged(line);
        }
        private void RemoveSimLine(StandardLine line)
        {
            var l = _lines[line.ID];
            var empty = new LineVertex();
            if (!_simvbo.TryFreeVertices(l, 6))
            {
                for (int i = 0; i < 6; i++)
                {
                    _simvbo.SetVertex(l + i, empty);
                }
            }
            _decorator.RemoveLine(line);
        }
        #endregion
    }
}