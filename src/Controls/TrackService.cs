//
//  GLTrack.cs
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
    /// <summary>The interface for communicating with the game track object</summary>
    public class TrackService : GameService
    {
        internal class Tracklocation
        {
            public int Frame;
            public int Iteration;
            public Rider State;
        }

        public readonly Stopwatch Fpswatch = new Stopwatch();
        internal readonly FPSCounter FpsCounter = new FPSCounter();
        /// <summary>
        /// indicates if we're in playbackmode
        /// </summary>
        public bool PlaybackMode;
        public bool Paused;
        public float Zoom = 4.0f;
        public Camera Camera;
        public List<LineTrigger> ActiveTriggers = new List<LineTrigger>();
        private float _oldZoom = 1.0f;
        private Tracklocation _flag;
        private Track _track;
        private int _startFrame;
        private ResourceSync _tracksync = new ResourceSync();
        private ResourceSync _playbacksync = new ResourceSync();
        /// <summary>
        /// The current number of frames since start (incl flag)
        /// </summary>
        public int Offset { get; private set; }
        public int CurrentFrame => PlaybackMode ? Offset + _startFrame : 0;
        public int LineCount => _track.Lines.Count;
        public bool SimulationNeedsDraw = false;
        public PlaybackBufferManager BufferManager;

        public bool RequiresUpdate
        {
            get
            {
                return renderer.RequiresUpdate;
            }
            set
            {
                renderer.RequiresUpdate = value;
            }
        }
        public bool ZeroStart
        {
            get { return _track.ZeroStart; }
            set { _track.ZeroStart = value; }
        }

        public FloatRect RiderRect => _track.RiderRect;
        private Tracklocation _renderrider = null;
        public Rider RenderRider
        {
            get
            {
                return _renderrider.State;
            }
        }
        /// <summary>
        /// The ID of the last frame in the playback buffer
        /// aka _track.RiderStates.Count - 1
        /// </summary>
        /// <returns></returns>
        public int EndFrameID
        {
            get
            {
                return _track.RiderStates.Count - 1;
            }
        }
        public string Name
        {
            get
            {
                return _track.Name;
            }
            set
            {
                //todo remove this setter
                _track.Name = value;
            }
        }
        /// Returns the real rider state associated with the current frame

        public UndoManager UndoManager;
        public bool Playing => PlaybackMode && !Paused;
        /// <summary>
        /// The offset between 0-6 of the currently rendered iteration
        /// </summary>
        public int IterationsOffset
        {
            get
            {
                return _renderrider.Iteration;
            }
            set
            {
                if (IterationsOffset > 6 || IterationsOffset < 0)
                    throw new Exception("iteration num out of range");

                _renderrider.Iteration = value;
            }
        }

        public TrackService()
        {
            Camera = new Camera();
            _track = new Track();
            _track.ActiveTriggers = ActiveTriggers;
            Offset = 0;
            BufferManager = new PlaybackBufferManager();
            _renderrider = new Tracklocation() { Frame = 0, State = _track.RiderStates[0], Iteration = 6 };
            UndoManager = new UndoManager();
        }
        /// <summary>
        /// Function to be called after updating the playback buffer
        /// </summary>
        public void UpdateRenderRider()
        {
            if (_renderrider.Iteration < 6 && _renderrider.Frame > 0)
            {
                _renderrider.State = _track.Tick(_track.RiderStates[_renderrider.Frame], _renderrider.Iteration);
            }
            else
            {
                _renderrider.State = _track.RiderStates[_renderrider.Frame];
            }
        }
        public Rider GetStart()
        {
            return _track.GetStart();
        }
        public void Reset()
        {
            _track.Reset();
            SetFrame(0);
        }
        public bool FastGridCheck(double x, double y)
        {
            var chunk = _track.RenderCells.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        public bool GridCheck(double x, double y)
        {
            var chunk = _track.Grid.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        /// <summary>
        /// Enters a state where no other thread can modify the playback state [riderstates]
        /// </summary>
        public ResourceSync.ResourceLock EnterPlayback()
        {
            return _playbacksync.AcquireRead();
        }
        public void RedrawLine(Line l)
        {
            renderer.RedrawLine(l);
        }

        /// <summary>
        /// Function for indicating the physics of the track have changed, so inform buffermanager
        /// </summary>
        public void TrackUpdated()
        {
            if (PlaybackMode)
                BufferManager.Update();
        }
        SimulationRenderer renderer = new SimulationRenderer();
        public void Render(float blend)
        {
            SimulationRenderer.DrawOptions drawOptions = new SimulationRenderer.DrawOptions();
            drawOptions.Blend = blend;
            drawOptions.GravityWells = Settings.Local.RenderGravityWells;
            drawOptions.KnobState = 0;
            if (game.SelectedTool is SelectTool)
            {
                drawOptions.KnobState = ((SelectTool)game.SelectedTool).CanLifelock ? 2 : 1;
            }
            drawOptions.LineColors = Settings.Local.PreviewMode || !Playing || Settings.Local.ColorPlayback;
            drawOptions.Paused = Paused;
            drawOptions.Playback = PlaybackMode;
            drawOptions.Rider = RenderRider;
            drawOptions.ShowContactLines = Settings.Local.DrawContactPoints;
            drawOptions.ShowMomentumVectors = Settings.Local.MomentumVectors;
            if (Settings.SmoothPlayback && Playing && Offset > 0 && blend < 1)
            {
                //interpolate between last frame and current one
                drawOptions.Rider = _track.RiderStates[Offset - 1].Lerp(RenderRider, blend);
            }
            renderer.Render(_track, Camera, drawOptions);
        }

        internal void RestoreFlag(Tracklocation flag)
        {
            _flag = flag;
        }

        public void Flag()
        {
            using (EnterPlayback())
            {
                if (PlaybackMode)
                {
                    _flag = new Tracklocation { State = _track.RiderStates[Offset], Frame = CurrentFrame };
                }
                else
                {
                    _flag = null;
                }
            }
            game.Canvas.DisableFlagTooltip();
            game.Invalidate();
        }
        /// <summary>
        /// Redraws the entire track in the renderer.
        /// useful for night mode toggle
        /// </summary>
        public void RefreshTrack()
        {
            renderer.RefreshTrack(_track);
        }

        public void NextFrame()
        {
            SetFrame(++Offset);
        }

        public void PreviousFrame()
        {
            SetFrame(Math.Max(0, Offset - 1));
        }

        public void Stop()
        {
            if (PlaybackMode)
            {
                PlaybackMode = false;
                Paused = false;

                Zoom = _oldZoom;
                Camera.Pop();
                Reset();
                game.Scheduler.UpdatesPerSecond = 40;
                foreach (var v in ActiveTriggers)
                {
                    v.Reset();
                }
                ActiveTriggers.Clear();
                if (Settings.Local.EnableSong)
                {
                    AudioService.Stop();
                }
                game.Canvas.HidePlaybackUI();
                game.Invalidate();
                game.Track.SimulationNeedsDraw = true;
            }
        }

        public void TogglePause()
        {
            if (PlaybackMode)
            {
                Paused = !Paused;
                game.Canvas.UpdatePauseUI();
                game.Canvas.UpdateIterationUI();
                game.Scheduler.Reset();

                if (Settings.Local.EnableSong)
                {
                    if (Paused)
                    {
                        AudioService.Pause();
                    }
                    else
                    {
                        game.UpdateSongPosition(CurrentFrame / 40f);
                    }
                }
                SimulationNeedsDraw = true;
            }
        }


        public void Start(bool ignoreflag = false, bool clearcollidedlines = true, bool startmusic = true, bool ghoststart = false)
        {
            //todo collisino line clearing removed
            if (!PlaybackMode)
            {
                _oldZoom = Zoom;
                Camera.Push();
            }
            using (EnterPlayback())
            {
                FpsCounter.Reset(40);
                Fpswatch.Restart();
                PlaybackMode = true;
                Paused = false;
                _startFrame = 0;
                if (_flag == null || ignoreflag)
                {
                    _track.Reset();
                    Offset = 0;
                }
                else
                {
                    _track.Reset(_flag.State);
                    _startFrame = _flag.Frame;
                }
                var cameracenter = RenderRider.CalculateCenter();

                Camera.SetFrame(RenderRider.CalculateCenter(), false);
                UpdateCamera();
                game.UpdateCursor();
                game.Scheduler.UpdatesPerSecond =
                    (int)(Math.Round(Settings.Local.DefaultPlayback * 40));
                game.Canvas.ShowPlaybackUI();
                switch (Settings.PlaybackZoomType)
                {
                    case 0: //current
                        break;

                    case 1: //default
                        game.Track.Zoom = 2;
                        break;

                    case 2: //specific
                        game.Track.Zoom = Settings.PlaybackZoomValue;
                        break;
                }
                game.Scheduler.Reset();
                Fpswatch.Restart();
                if (ghoststart && _flag != null)
                {
                    foreach (var v in ActiveTriggers)
                    {
                        v.Reset();
                    }
                    ActiveTriggers.Clear();
                    using (CreateTrackReader())
                    {
                        _track.Reset();
                        Offset = 0;
                        for (int i = 0; i < _flag.Frame; i++)
                        {
                            _track.AddFrame();
                        }
                        SetFrame(EndFrameID);
                    }
                    for (var i = 0; i < _track.RiderStates[Offset].Body.Length; i++)
                    {
                        if (_track.RiderStates[Offset].Body[i].Location != _flag.State.Body[i].Location ||
                            _track.RiderStates[Offset].Body[i].Previous != _flag.State.Body[i].Previous)
                        {
                            game.Canvas.SetFlagTooltip(false);
                            game.Invalidate();
                            return;
                        }
                    }
                    game.Canvas.UpdateScrubber();
                }
                if (startmusic && Settings.Local.EnableSong)
                {
                    if (_flag != null)
                        game.UpdateSongPosition(CurrentFrame / 40f);
                    else
                        game.UpdateSongPosition(0);
                }
                game.Invalidate();
            }
        }
        public TrackWriter CreateTrackWriter()
        {
            return TrackWriter.AcquireWrite(_tracksync, _track, renderer, UndoManager, BufferManager);
        }
        public TrackReader CreateTrackReader()
        {
            return TrackReader.AcquireRead(_tracksync, _track);
        }
        public void SetFrame(int frame, bool updateslider = true)
        {
            //todo evaluate thread safety
            if (frame > _track.RiderStates.Count)
            {
                throw new Exception("unsupported frameskip to " + (frame - _track.RiderStates.Count));
            }
            if (frame == _track.RiderStates.Count)
            {
                using (var trk = CreateTrackReader())
                {
                    using (EnterPlayback())
                    {
                        _track.AddFrame();
                    }
                }
            }
            using (EnterPlayback())
            {
                Offset = frame;
                IterationsOffset = 6;
                _renderrider.Iteration = 6;
                _renderrider.State = _track.RiderStates[Offset];
                _renderrider.Frame = Offset;
                game.Canvas.UpdateIterationUI();
            }
            if (updateslider)
            {
                game.Canvas.UpdateScrubber();
            }
            game.Invalidate();
        }

        public void Update(int times)
        {
            var slider = (HorizontalIntSlider)game.Canvas.FindChildByName("timeslider");
            if (slider.Held)
            {
                UpdateCamera();
                return;
            }
            for (var i = 0; i < times; i++)
            {
                NextFrame();
                for (var ix = ActiveTriggers.Count - 1; ix >= 0; --ix)
                    if (ActiveTriggers[ix].Activate())
                        ActiveTriggers.RemoveAt(ix);
                UpdateCamera();
            }
        }

        public void UpdateCamera()
        {
            Camera.SetFrame(RenderRider.CalculateCenter(), true);
            if (Settings.SmoothCamera)
            {
                Rider prediction;
                using (var trk = CreateTrackReader())
                {
                    prediction = trk.Tick(RenderRider);
                }
                Camera.SetPrediction(prediction.CalculateCenter());

            }
            SimulationNeedsDraw = true;
        }

        public void BackupTrack(bool Crash = true)
        {
            try
            {
                if (_track.Lines.Count == 0)
                    return;
                game.Loading = true;
                using (var trk = CreateTrackReader())
                {
                    if (Crash)
                    {
                        TrackLoader.CreateTrackFile(_track, "Crash Backup", Settings.Local.CurrentSong?.ToString());
                    }
                    else
                    {
                        TrackLoader.CreateAutosave(_track, Settings.Local.CurrentSong?.ToString());
                    }
                }
            }
            catch
            {
                //ignored
            }
            finally
            {
                game.Loading = false;
                game.Invalidate();
            }
        }

        public void ChangeTrack(Track trk)
        {
            _flag = null;
            if (_track != null && _track.ActiveTriggers == ActiveTriggers)
                _track.ActiveTriggers = null;
            BufferManager.Reset();
            _track = trk;
            _track.ActiveTriggers = ActiveTriggers;
            RefreshTrack();
            Reset();
            Camera.SetFrame(trk.StartOffset, false);
        }

        internal Tracklocation GetFlag()
        {
            return _flag;
        }
    }
}