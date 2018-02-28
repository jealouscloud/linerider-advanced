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

namespace linerider.Rendering
{
    public class SimulationRenderer : GameService
    {
        private TrackRenderer _trackrenderer;
        private RiderRenderer _riderrenderer;

        public bool RequiresUpdate
        {
            get
            {
                return _trackrenderer.RequiresUpdate;
            }
            set
            {
                _trackrenderer.RequiresUpdate = value;
            }
        }
        private LineVAO _linevao;
        public SimulationRenderer()
        {
            _linevao = new LineVAO();
            _trackrenderer = new TrackRenderer();
            _riderrenderer = new RiderRenderer();
        }
        private void DrawRider(Camera camera, Track track)
        {
        }
        public void Render(Track track, Timeline timeline, Camera camera, DrawOptions options)
        {
            Rider drawrider = options.Rider;
            _trackrenderer.Render(options);
            if (Settings.Local.OnionSkinning && options.PlaybackMode)
            {
                const int onions = 20;
                for (int i = -onions; i < onions; i++)
                {
                    var frame = game.Track.Offset + i;
                    if (frame > 0 && frame < game.Track.FrameCount && i != 0)
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
                options.ShowContactLines ? 0.5f : 1,
                options.Rider,
                true);
            if (options.ShowMomentumVectors)
            {
                GameRenderer.DrawMomentum(options.Rider, _linevao);
                // todo create a preference and uncommon this feature:
                /*if (!options.IsRunning)
                {
                    var frame = timeline.GetFrame(game.Track.Offset + 1, 0);
                    _riderrenderer.DrawRider(0.1f, frame);
                }*/
            }
            if (options.ShowContactLines)
            {
                GameRenderer.DrawContactPoints(options.Rider, options.RiderDiagnosis, _linevao);
            }
            _riderrenderer.Draw();
            _riderrenderer.Clear();
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
            _trackrenderer.AddLine(l);
        }
        public void RedrawLine(GameLine l)
        {
            _trackrenderer.LineChanged(l);
        }
        public void RemoveLine(GameLine l)
        {
            _trackrenderer.RemoveLine(l);
        }
        public void RefreshTrack(Track track)
        {
            _trackrenderer.InitializeTrack(track);
        }
    }
}