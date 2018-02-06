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

namespace linerider.Rendering
{
    public class SimulationRenderer
    {
        private TrackRenderer _renderer;

        public bool RequiresUpdate
        {
            get
            {
                return _renderer.RequiresUpdate;
            }
            set
            {
                _renderer.RequiresUpdate = value;
            }
        }
        public SimulationRenderer()
        {
            _renderer = new TrackRenderer();
        }
        private void DrawRider(Camera camera, Track track)
        {
        }
        public void Render(Track track, Camera camera, DrawOptions options)
        {
            Rider drawrider = options.Rider;
            _renderer.Render(track, options);
            //todo contact lines, onion skinning
            GameRenderer.DrawRider(options.ShowContactLines ? 0.4f : 1, options.Rider, true, options.ShowContactLines, options.ShowMomentumVectors, options.Iteration);
        }
        public void AddLine(Line l)
        {
            _renderer.AddLine(l);
            RequiresUpdate = true;
        }
        public void RedrawLine(Line l)
        {
            _renderer.LineChanged(l);
            RequiresUpdate = true;
        }
        public void RemoveLine(Line l)
        {
            _renderer.RemoveLine(l);
            RequiresUpdate = true;
        }
        public void RefreshTrack(Track track)
        {
            _renderer.InitializeTrack(track.Lines);
            _renderer.RequiresUpdate = true;
        }
    }
}