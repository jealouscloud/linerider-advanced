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
using System.Diagnostics;
using linerider.Utils;
using linerider.Drawing;
using linerider.Lines;

namespace linerider.Rendering
{
    internal class TrackRenderer : GameService
    {
        private enum LineActionType
        {
            Add,
            Change,
            Remove
        }
        public bool RequiresUpdate = true;

        private LineDecorator _decorator;
        private LineRenderer _physvbo;
        private LineRenderer _sceneryvbo;
        /// <summary>
        /// A dictionary of [line id] -> [index of first vertex]
        /// </summary>
        private Dictionary<int, int> _physlines;

        /// <summary>
        /// A dictionary of [line id] -> [index of first IBO index]
        /// </summary>
        /// <remarks>
        /// We have a seperate scenery vbo for rendering it under the sim one.
        /// </remarks>
        private Dictionary<int, int> _scenerylines;

        /// <summary>
        /// We use an action queue system instead of instantly adding to the renderer
        /// for the sake of multithreading safety
        /// </summary>
        private Queue<Tuple<LineActionType, GameLine>> _lineactions;
        private ResourceSync _sync;
        public TrackRenderer()
        {
            _sync = new ResourceSync();
            _lineactions = new Queue<Tuple<LineActionType, GameLine>>();
            _physlines = new Dictionary<int, int>();
            _scenerylines = new Dictionary<int, int>();
            _decorator = new LineDecorator();
            _physvbo = new LineRenderer(Shaders.LineShader);
            _physvbo.LineColor = Constants.DefaultLineColor;

            _sceneryvbo = new LineRenderer(Shaders.LineShader);
            _sceneryvbo.LineColor = Color.Black;
        }
        public void Render(DrawOptions options)
        {
            using (new GLEnableCap(EnableCap.Texture2D))
            {
                UpdateBuffers();
                GL.BlendFunc(
                    BlendingFactorSrc.SrcAlpha,
                    BlendingFactorDest.OneMinusSrcAlpha);
                GameDrawingMatrix.Enter();
                _physvbo.Scale = options.Zoom;
                _physvbo.KnobState = options.KnobState;

                _sceneryvbo.Scale = options.Zoom;
                //green lines dont get lifelock
                if (options.KnobState != KnobState.Hidden)
                    _sceneryvbo.KnobState = KnobState.Shown;
                else
                    _sceneryvbo.KnobState = KnobState.Hidden;

                if (options.NightMode)
                {
                    _sceneryvbo.LineColor = Constants.DefaultNightLineColor;
                    _physvbo.LineColor = Constants.DefaultNightLineColor;
                }
                else
                {
                    _sceneryvbo.LineColor = Constants.DefaultLineColor;
                    _physvbo.LineColor = Constants.DefaultLineColor;
                }
                if (options.LineColors)
                {
                    _sceneryvbo.LineColor = Constants.SceneryLineColor;
                }
                _sceneryvbo.Draw();
                _decorator.DrawUnder(options);
                _physvbo.Draw();
                GameDrawingMatrix.Exit();
            }
        }
        /// <summary>
        /// Clears the renderer and initializes it with new lines.
        /// </summary>
        public void InitializeTrack(Track track)
        {
            using (_sync.AcquireWrite())
            {
                _lineactions.Clear();
                _physvbo.Clear();
                _sceneryvbo.Clear();
                _decorator.Clear();
                _physlines.Clear();
                _scenerylines.Clear();
                _physlines.Clear();
            }
            List<GameLine> scenery = new List<GameLine>(track.SceneryLines);
            List<GameLine> phys = new List<GameLine>(track.BlueLines + track.RedLines);
            var sorted = track.GetSortedLines();

            // iterate backwards for render order on top.
            // list is returned by id 3 2 1, we want 3 on top.
            for (int i = sorted.Length - 1; i >= 0; i--)
            {
                var line = sorted[i];
                if (line.Type == LineType.Scenery)
                {
                    scenery.Add(line);
                }
                else
                {
                    phys.Add(line);
                    _decorator.AddLine((StandardLine)line);
                }
            }
            if (scenery.Count != 0)
            {
                _scenerylines = _sceneryvbo.AddLines(
                    scenery,
                    Color.FromArgb(0));
            }
            if (phys.Count != 0)
            {
                _physlines = _physvbo.AddLines(
                    phys,
                    Color.FromArgb(0));
            }
            RequiresUpdate = true;
        }
        public void AddLine(GameLine line)
        {
            RequiresUpdate = true;
            using (_sync.AcquireWrite())
            {
                _lineactions.Enqueue(
                    new Tuple<LineActionType, GameLine>(
                        LineActionType.Add,
                        line));
            }
        }
        public void LineChanged(GameLine line)
        {
            RequiresUpdate = true;
            using (_sync.AcquireWrite())
            {
                _lineactions.Enqueue(
                    new Tuple<LineActionType, GameLine>(
                        LineActionType.Change,
                        line));
            }
        }
        public void RemoveLine(GameLine line)
        {
            RequiresUpdate = true;
            using (_sync.AcquireWrite())
            {
                _lineactions.Enqueue(
                    new Tuple<LineActionType, GameLine>(
                        LineActionType.Remove,
                        line));
            }
        }
        private void UpdateBuffers()
        {
            using (_sync.AcquireWrite())
            {
                RequiresUpdate = false;
                while (_lineactions.Count != 0)
                {
                    var dequeued = _lineactions.Dequeue();
                    var line = dequeued.Item2;
                    switch (dequeued.Item1)
                    {
                        case LineActionType.Add:
                            if (line.Type == LineType.Scenery)
                            {
                                AddLine(
                                    line,
                                    _sceneryvbo,
                                    _scenerylines);
                            }
                            else
                            {
                                AddLine(
                                    line,
                                    _physvbo,
                                    _physlines);
                                _decorator.AddLine((StandardLine)line);
                            }
                            break;
                        case LineActionType.Remove:
                            if (line.Type == LineType.Scenery)
                            {
                                RemoveLine(
                                    line,
                                    _sceneryvbo,
                                    _scenerylines);
                            }
                            else
                            {
                                RemoveLine(
                                    line,
                                    _physvbo,
                                    _physlines);
                                _decorator.RemoveLine((StandardLine)line);
                            }
                            break;
                        case LineActionType.Change:
                            if (line.Type == LineType.Scenery)
                            {
                                LineChanged(
                                    line,
                                    _sceneryvbo,
                                    _scenerylines);
                            }
                            else
                            {
                                int color = 0;
                                var stl = (StandardLine)line;
                                bool hit = Settings.Local.HitTest
                                    ? game.Track.Timeline.HitTest.IsHit(stl.ID)
                                    : false;
                                if (hit)
                                    color = Utility.ColorToRGBA_LE(stl.Color);

                                LineChanged(
                                    line,
                                    _physvbo,
                                    _physlines,
                                    color);

                                _decorator.LineChanged(stl, hit);
                            }
                            break;
                    }
                }
                _lineactions.Clear();
            }
        }
        private void AddLine(
            GameLine line,
            LineRenderer renderer,
            Dictionary<int, int> lookup)
        {
            if (lookup.ContainsKey(line.ID))
            {
                LineChanged(line, renderer, lookup);
                return;
            }
            int color = 0;
            if (line is StandardLine stl && stl.Trigger != null)
                color = unchecked((int)0xff4f95ff);
            var lineverts = LineRenderer.CreateTrackLine(
                line.Position,
                line.Position2,
                2 * line.Width,
                color);
            int start = renderer.AddLine(lineverts);
            lookup.Add(line.ID, start);
        }
        private void LineChanged(
            GameLine line,
            LineRenderer renderer,
            Dictionary<int, int> lookup,
            int coloroverride = 0)
        {
            float width = 2 * line.Width;
            if (coloroverride == 0 && line is StandardLine stl)
            {
                if (stl.Trigger != null)
                    coloroverride = Utility.ColorToRGBA_LE(
                        Constants.TriggerLineColor);
            }
            var lineverts = LineRenderer.CreateTrackLine(
                line.Position,
                line.Position2,
                width,
                coloroverride);
            renderer.ChangeLine(lookup[line.ID], lineverts);
        }
        private void RemoveLine(
            GameLine line,
            LineRenderer renderer,
            Dictionary<int, int> lookup)
        {
            int start = lookup[line.ID];
            renderer.RemoveLine(start);
            // preserve the id in the lookup in event of undo
        }
    }
}