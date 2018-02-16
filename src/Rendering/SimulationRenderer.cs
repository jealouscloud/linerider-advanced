using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using linerider.Drawing;
using linerider.Game;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Threading;
using Gwen.Controls;
using linerider.Tools;
using linerider.Audio;
using linerider.Utils;
using linerider.Lines;

namespace linerider.Rendering
{
    public class SimulationRenderer : GameService
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
            List<GenericVertex> verts = new List<GenericVertex>(300);
            if (options.ShowMomentumVectors)
            {
                GameRenderer.DrawMomentum(options.Rider, verts);
            }
            if (options.ShowContactLines)
            {
                GameRenderer.DrawContactPoints(options.Rider, options.RiderDiagnosis, verts);
            }
            if (verts.Count > 0)
            {
                VAO vao = new VAO(false, false);
                vao.Texture = StaticRenderer.CircleTex;
                vao.AddVerticies(verts);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                vao.Draw(PrimitiveType.Triangles);
            }
        }
        public void AddLine(GameLine l)
        {
            _renderer.AddLine(l);
        }
        public void RedrawLine(GameLine l)
        {
            _renderer.LineChanged(l);
        }
        public void RemoveLine(GameLine l)
        {
            _renderer.RemoveLine(l);
        }
        public void RefreshTrack(Track track)
        {
            _renderer.InitializeTrack(track);
            _renderer.RequiresUpdate = true;
        }
    }
}