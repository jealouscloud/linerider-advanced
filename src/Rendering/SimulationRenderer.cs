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
        public void Render(Track track, Timeline timeline, Camera camera, DrawOptions options)
        {
            Rider drawrider = options.Rider;
            _renderer.Render(options);
            if (Settings.Local.OnionSkinning && options.Playback)
            {
                //todo make efficient and clean
                const int onions = 10;
                for (int i = 0; i < onions; i++)
                {
                    var frame = game.Track.Offset - (onions - i);
                    if (frame > 0)
                    {
                        GameRenderer.DrawRider(0.3f, timeline.GetFrame(frame), true, options.ShowContactLines);
                    }
                }
                for (int i = 1; i < onions + 1 && game.Track.Offset + i < timeline.Length; i++)
                {
                    GameRenderer.DrawRider(0.3f, timeline.GetFrame(game.Track.Offset + i), true, options.ShowContactLines);
                }
            }
            if (options.DrawFlag)
                GameRenderer.DrawRider(0.3f, options.FlagRider, true);
            GameRenderer.DrawRider(
                options.ShowContactLines ? 0.4f : 1,
                options.Rider,
                true,
                options.ShowContactLines,
                options.ShowMomentumVectors,
                options.Iteration);

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