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
        private RiderRenderer _riderrenderer;

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
        private LineVAO _linevao;
        public SimulationRenderer()
        {
            _linevao = new LineVAO();
            _renderer = new TrackRenderer();
            _riderrenderer = new RiderRenderer();
        }
        private void DrawRider(Camera camera, Track track)
        {
        }
        public void Render(Track track, Timeline timeline, Camera camera, DrawOptions options)
        {
            Rider drawrider = options.Rider;
            _renderer.Render(options);
            if (Settings.Local.OnionSkinning && options.Playback)
            {
                const int onions = 20;
                for (int i = -onions; i < onions; i++)
                {
                    var frame = game.Track.Offset + i;
                    if (frame > 0 && frame < timeline.Length && i != 0)
                    {
                        _riderrenderer.DrawRider(
                            0.3f,
                            timeline.GetFrame(frame),
                            true);
                    }
                }
            }
            if (options.DrawFlag)
                _riderrenderer.DrawRider(
                    0.3f,
                    options.FlagRider,
                    true);

            _riderrenderer.DrawRider(
                options.ShowContactLines ? 0.4f : 1,
                options.Rider,
                true);
            _riderrenderer.Draw();
            _riderrenderer.Clear();
            if (options.ShowMomentumVectors)
            {
                GameRenderer.DrawMomentum(options.Rider, _linevao);
            }
            if (options.ShowContactLines)
            {
                GameRenderer.DrawContactPoints(options.Rider, options.RiderDiagnosis, _linevao);
            }
            if (_linevao.Array.Count > 0)
            {
                GameDrawingMatrix.Enter();
                _linevao.Scale = GameDrawingMatrix.Scale;
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                _linevao.Draw(PrimitiveType.Triangles);
                GameDrawingMatrix.Exit();
                _linevao.Clear();
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