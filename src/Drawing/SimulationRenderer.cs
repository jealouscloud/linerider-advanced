using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using linerider.Drawing;
using linerider.Game;
using OpenTK;
using System.IO;
using System.Threading;
using Gwen.Controls;
using linerider.Tools;
using linerider.Audio;
using linerider.Utils;

namespace linerider
{
    public class SimulationRenderer
    {
        public class DrawOptions
        {
            public bool Playback = false;
            public bool Paused = false;
            public bool LineColors = true;
            public bool GravityWells = false;
            public int KnobState = 0;
            public float Blend = 1;
            public Rider Rider;
            public bool ShowContactLines = false;
            public bool ShowMomentumVectors = false;
            public int Iteration = 6;
            public bool IsRunning
            {
                get
                {
                    return Playback && !Paused;
                }
            }
        }
        private TrackRenderer _renderer;
        private SceneryRenderer _sceneryrenderer;
        private FloatRect _trackrect;

        public bool RequiresUpdate
        {
            get
            {
                return _renderer.RequiresUpdate || _sceneryrenderer.RequiresUpdate;
            }
            set
            {
                _renderer.RequiresUpdate = _sceneryrenderer.RequiresUpdate = value;
            }
        }
        public SimulationRenderer()
        {
            _renderer = new TrackRenderer();
            _sceneryrenderer = new SceneryRenderer();
        }
        private void DrawRider(Camera camera, Track track)
        {
        }
        public void Render(Track track, Camera camera, DrawOptions options)
        {
            Rider drawrider = options.Rider;
            FloatRect rect = camera.GetViewport().ToFloatRect();
            var needsredraw = (!_trackrect.Contains(rect.Left, rect.Top) ||
                               !_trackrect.Contains(rect.Left + rect.Width, rect.Top + rect.Height));
            if (!needsredraw)
            {
                var viewport = rect.Inflate(rect.Width, rect.Height);
                if (viewport.Width < _trackrect.Width / 3 && viewport.Height < _trackrect.Height / 3)
                    needsredraw = true;
            }

            if (needsredraw || RequiresUpdate)
            {
                var viewport = rect.Inflate(rect.Width, rect.Height);
                List<Line> lines = track.GetLinesInRect(viewport, false, true);
                _trackrect = viewport;
                _renderer.UpdateViewport(lines);
            }
            _renderer.Render(track, options.LineColors, options.KnobState, options.GravityWells);
            _sceneryrenderer.Render(options.LineColors);
            //todo contact lines, onion skinning
            GameRenderer.DrawRider(options.ShowContactLines ? 0.4f : 1, options.Rider, true, options.ShowContactLines, options.ShowMomentumVectors, options.Iteration);
        }
        public void AddLine(Line l)
        {
            if (l is SceneryLine)
            {
                _sceneryrenderer.AddLine(l);
            }
            else
            {
                _renderer.AddLine((StandardLine)l);
            }
            RequiresUpdate = true;
        }
        public void RedrawLine(Line l)
        {
            if (l is SceneryLine)
            {
                _sceneryrenderer.LineChanged(l);
            }
            else
            {
                _renderer.LineChanged((StandardLine)l);
            }
            RequiresUpdate = true;
        }
        public void RemoveLine(Line l)
        {
            if (l is SceneryLine)
            {
                _sceneryrenderer.RemoveLine(l);
            }
            else
            {
                _renderer.RemoveLine((StandardLine)l);
            }
            RequiresUpdate = true;
        }
        public void RefreshTrack(Track track)
        {
            _sceneryrenderer.InitializeTrack(track.Lines);
            _sceneryrenderer.RequiresUpdate = true;
            _renderer.UpdateViewport(null);
            _renderer.RequiresUpdate = true;
        }
    }
}