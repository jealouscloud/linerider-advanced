﻿//
//  Editor.cs
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
using linerider.Rendering;
using linerider.Game;
using OpenTK;
using System.IO;
using System.Threading;
using Gwen.Controls;
using linerider.Tools;
using linerider.Audio;
using linerider.Utils;
using linerider.IO;

namespace linerider
{
    /// <summary>The interface for communicating with the game track object</summary>
    public class Editor : GameService
    {
        private bool _loadingTrack = false;
        private int _prevSaveUndoPos = 0;
        private float _oldZoom = 1.0f;
        private RiderFrame _flag;
        private Track _track;
        private int _startFrame;
        private int _iteration = 6;
        private bool _renderriderinvalid = true;
        private RiderFrame _renderrider = null;
        private ResourceSync _tracksync = new ResourceSync();
        private ResourceSync _renderridersync = new ResourceSync();
        private SimulationRenderer _renderer = new SimulationRenderer();
        private bool _refreshtrack = false;
        private EditorGrid _cells = new EditorGrid();

        /// <summary>
        /// A sync object for loading track files
        /// </summary>
        public readonly object LoadSync = new object();
        public readonly Stopwatch FramerateWatch = new Stopwatch();
        public readonly FPSCounter FramerateCounter = new FPSCounter();
        /// <summary>
        /// indicates if we're in playbackmode
        /// </summary>
        public bool PlaybackMode { get; private set; }
        private bool _paused = false;
        public float Zoom = Constants.DefaultZoom;
        public ICamera Camera { get; private set; }
        public List<LineTrigger> ActiveTriggers = new List<LineTrigger>();
        public int CurrentFrame => PlaybackMode ? Offset + _startFrame : 0;
        public int LineCount => _track.Lines.Count;
        public int TrackChanges
        {
            get
            {
                return Math.Abs(UndoManager.ActionPosition - _prevSaveUndoPos);
            }
        }
        private bool _invalidated = false;
        public bool NeedsDraw
        {
            get
            {
                return _renderer.RequiresUpdate || _refreshtrack || _renderriderinvalid || _invalidated;
            }
        }
        public bool ZeroStart
        {
            get { return _track.ZeroStart; }
            set

            {
                if (_track.ZeroStart != value)
                {
                    _track.ZeroStart = value;
                    Stop();
                    Reset();
                }
            }
        }
        public RiderFrame RenderRiderInfo
        {
            get
            {
                if (_renderriderinvalid)
                {
                    _renderriderinvalid = false;
                    _renderrider = Timeline.ExtractFrame(Offset, IterationsOffset);

                    // we dont want it and we dont wanto to continuously
                    // update the diagnosis.
                    if (!PlaybackMode)
                        _renderrider.Diagnosis.Clear();
                }
                return _renderrider;
            }
        }
        public Rider RenderRider
        {
            get
            {
                return RenderRiderInfo.State;
            }
        }
        public string Name
        {
            get
            {
                return _track.Name;
            }
        }

        public bool Paused
        {
            get
            {
                return _paused;
            }
            private set
            {
                if (value == _paused)
                    return;
                _paused = value;
            }
        }
        public int FrameCount { get; private set; } = 1;

        public UndoManager UndoManager { get; private set; }
        public bool Playing => PlaybackMode && !Paused;
        /// <summary>
        /// The offset between 0-6 of the currently rendered iteration
        /// </summary>
        public int IterationsOffset
        {
            get
            {
                return _iteration;
            }
            set
            {
                if (IterationsOffset > 6 || IterationsOffset < 0)
                    throw new Exception("iteration num out of range");
                _iteration = value;
            }
        }
        /// <summary>
        /// The current number of frames since start (incl flag)
        /// </summary>
        public int Offset { get; private set; }

        public Timeline Timeline { get; private set; }
        public bool MoveStartWarned = false;
        public Editor()
        {
            _track = new Track();
            Timeline = new Timeline(_track, ActiveTriggers);
            Timeline.FrameInvalidated += FrameInvalidated;
            InitCamera();
            Offset = 0;
            UndoManager = new UndoManager();
        }
        public void Render(float blend)
        {
            _invalidated = false;
            if (_refreshtrack)
            {
                using (_tracksync.AcquireRead())
                {
                    _renderer.RefreshTrack(_track);
                }
                _refreshtrack = false;
            }
            DrawOptions drawOptions = new DrawOptions();
            drawOptions.DrawFlag = _flag != null;
            if (drawOptions.DrawFlag)
            {
                drawOptions.FlagRider = _flag.State;
            }
            drawOptions.Blend = blend;
            drawOptions.NightMode = Settings.NightMode;
            drawOptions.GravityWells = Settings.Local.RenderGravityWells;
            drawOptions.LineColors = !Settings.Local.PreviewMode && (!Playing || Settings.Local.ColorPlayback);
            drawOptions.KnobState = KnobState.Hidden;
            if (!Playing && game.SelectedTool is MoveTool movetool)
            {
                drawOptions.KnobState = movetool.CanLifelock
                ? KnobState.LifeLock
                : KnobState.Shown;
            }
            drawOptions.Paused = Paused;
            drawOptions.PlaybackMode = PlaybackMode;
            drawOptions.Rider = RenderRiderInfo.State;
            drawOptions.ShowContactLines = Settings.Local.DrawContactPoints;
            drawOptions.ShowMomentumVectors = Settings.Local.MomentumVectors;
            drawOptions.Zoom = Zoom;
            drawOptions.RiderDiagnosis = RenderRiderInfo.Diagnosis;
            int renderframe = Offset;
            if (Playing && Offset > 0 && blend < 1)
            {
                //interpolate between last frame and current one
                var current = Timeline.GetFrame(Offset);
                var prev = Timeline.GetFrame(Offset - 1);
                drawOptions.Rider = Rider.Lerp(prev, current, blend);
                renderframe = Offset - 1;
            }
            drawOptions.Iteration = IterationsOffset;
            if (!_loadingTrack)
            {
                var changes = Timeline.RequestFrameForRender(renderframe);
                foreach (var change in changes)
                {
                    GameLine line;
                    if (_track.LineLookup.TryGetValue(change, out line))
                    {
                        _renderer.RedrawLine(line);
                    }
                }
            }

            _renderer.Render(_track, Timeline, Camera, drawOptions);
        }

        /// <summary>
        /// Function to be called after updating the playback buffer
        /// </summary>
        public void InvalidateRenderRider()
        {
            _renderriderinvalid = true;
        }
        public void Invalidate()
        {
            _invalidated = true;
        }
        public Rider GetStart()
        {
            return _track.GetStart();
        }
        public void Reset()
        {
            Timeline.Restart(_track.GetStart());
            SetFrame(0);
        }
        public bool FastGridCheck(double x, double y)
        {
            var chunk = _cells.GetCellFromPoint(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        public bool GridCheck(double x, double y)
        {
            var chunk = _track.Grid.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        /// <summary>
        /// Function for indicating the physics of the track have changed, so inform buffermanager
        /// </summary>
        public void NotifyTrackChanged()
        {
            if (PlaybackMode)
            {
                Timeline.NotifyChanged();
                InvalidateRenderRider();
            }
            game.Canvas.DisableFlagTooltip();
            Invalidate();
        }
        internal void RestoreFlag(RiderFrame flag)
        {
            _flag = flag;
        }

        public void Flag()
        {
            if (PlaybackMode)
            {
                _flag = new RiderFrame { State = Timeline.GetFrame(Offset), FrameID = CurrentFrame };
            }
            else
            {
                _flag = null;
            }

            game.Canvas.DisableFlagTooltip();
            game.Invalidate();
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
                CancelTriggers();
                game.Canvas.HidePlaybackUI();
                Invalidate();
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
                Camera.BeginFrame(1, Zoom);
                Camera.SetFrameCenter(Camera.GetCenter(true));
                Invalidate();
            }
        }
        public void StartFromFlag()
        {
            CancelTriggers();
            if (_flag == null)
            {
                _startFrame = 0;
                Timeline.Restart(_track.GetStart());
            }
            else
            {
                _startFrame = _flag.FrameID;
                Timeline.Restart(_flag.State);
            }
            FrameCount = 1;
            Start(0);
        }
        public void StartIgnoreFlag()
        {
            CancelTriggers();
            _startFrame = 0;
            Timeline.Restart(_track.GetStart());
            FrameCount = 1;
            Start(0);
        }
        public void ResumeFromFlag()
        {
            CancelTriggers();
            _startFrame = 0;
            Timeline.Restart(_track.GetStart());
            FrameCount = 1;
            if (_flag != null)
            {
                var atflag = Timeline.GetFrame(_flag.FrameID);
                FrameCount = _flag.FrameID + 1;
                game.Canvas.SetFlagState(atflag.Body.CompareTo(_flag.State.Body));
            }
            Start(FrameCount - 1);
        }
        private void Start(int frameid)
        {
            if (frameid >= FrameCount)
                throw new Exception("Start frame out of range");
            if (!PlaybackMode)
            {
                _oldZoom = Zoom;
                Camera.Push();
            }
            PlaybackMode = true;
            Paused = false;
            Offset = frameid;
            IterationsOffset = 6;
            Camera.SetFrameCenter(Timeline.GetFrame(frameid).CalculateCenter());

            game.Canvas.ShowPlaybackUI();
            game.UpdateCursor();
            switch (Settings.PlaybackZoomType)
            {
                case 0: //current
                    break;

                case 1: //default
                    game.Track.Zoom = Constants.DefaultZoom;
                    break;

                case 2: //specific
                    game.Track.Zoom = Settings.PlaybackZoomValue;
                    break;
            }
            game.Scheduler.Reset();
            FramerateCounter.Reset();
            game.Canvas.UpdateScrubber();
            InvalidateRenderRider();
            Invalidate();
        }
        public void SetFrame(int frame, bool updateslider = true)
        {
            Offset = frame;
            Timeline.GetFrame(frame);
            if (frame + 1 > FrameCount)
                FrameCount = frame + 1;
            IterationsOffset = 6;
            InvalidateRenderRider();

            game.Canvas.UpdateIterationUI();
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

        public void UpdateCamera(bool reverse = false)
        {
            Camera.SetFrame(Offset);
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
                        TrackIO.SaveTrackToFile(_track, "Crash Backup", Settings.Local.CurrentSong?.ToString());
                    }
                    else
                    {
                        if (TrackChanges > 50)
                        {
                            TrackIO.CreateAutosave(_track, Settings.Local.CurrentSong?.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Autosave exception: " + e);
            }
            finally
            {
                game.Loading = false;
                game.Invalidate();
            }
        }

        public void ChangeTrack(Track trk)
        {
            lock (LoadSync)
            {
                using (_tracksync.AcquireWrite())
                {
                    _loadingTrack = true;
                    Stop();
                    ActiveTriggers.Clear();
                    _flag = null;
                    _track = trk;

                    Timeline = new Timeline(trk, ActiveTriggers);
                    Timeline.FrameInvalidated += FrameInvalidated;
                    InitCamera();
                    UndoManager = new UndoManager();
                    ResetTrackChangeCounter();
                    _refreshtrack = true;
                    _cells.Clear();
                    foreach (var line in trk.LineLookup.Values)
                    {
                        _cells.AddLine(line);
                    }
                    Reset();
                    Camera.SetFrameCenter(Timeline.GetFrame(0).CalculateCenter());
                    _loadingTrack = false;
                }
            }
            Invalidate();
            GC.Collect();//this is probably safest place to make the gc work
            MoveStartWarned = false;
        }
        public void QuickSave()
        {
            if (TrackIO.QuickSave(_track, Settings.Local.CurrentSong?.ToString()))
            {
                ResetTrackChangeCounter();
                Settings.LastSelectedTrack = _track.Filename;
                Settings.Save();
            }
            else
            {
                game.Canvas.ShowSaveWindow();
            }
        }
        public void ResetTrackChangeCounter()
        {
            this._prevSaveUndoPos = UndoManager.ActionPosition;
        }
        public void AutoLoadPrevious()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                AutoLoad();
            });
        }
        private void AutoLoad()
        {
            try
            {
                game.Loading = true;
                var lasttrack = Settings.LastSelectedTrack;
                var trdr = Constants.TracksDirectory;
                if (!lasttrack.StartsWith(trdr))
                    return;
                if (string.Equals(
                    Path.GetExtension(lasttrack),
                    ".trk",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var trackname = TrackIO.GetTrackName(lasttrack);
                    lock (LoadSync)
                    {
                        var track = TRKLoader.LoadTrack(lasttrack, trackname);
                        ChangeTrack(track);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Autoload failure: " + e.ToString());
            }
            finally
            {
                game.Loading = false;
            }
        }
        public TrackWriter CreateTrackWriter()
        {
            return TrackWriter.AcquireWrite(_tracksync, _track, _renderer, UndoManager, Timeline, _cells);
        }
        public TrackReader CreateTrackReader()
        {
            return TrackReader.AcquireRead(_tracksync, _track, _cells);
        }

        internal RiderFrame GetFlag()
        {
            return _flag;
        }
        private void CancelTriggers()
        {
            foreach (var v in ActiveTriggers)
            {
                v.Reset();
            }
            ActiveTriggers.Clear();
        }
        public void InitCamera()
        {
            Vector2d start = Rider.Create(
                _track.StartOffset,
                Vector2d.Zero).CalculateCenter();//avoid a timeline query
            if (Camera != null)
            {
                Camera.BeginFrame(1, Zoom);
                start = Camera.GetCenter(true);
            }
            if (Settings.SmoothCamera)
            {
                Camera = new SmoothCamera();
            }
            else
            {
                Camera = new ClampCamera();
            }
            Camera.SetTimeline(Timeline);
            Camera.SetFrameCenter(start);
        }
        private void FrameInvalidated(object sender, int frame)
        {
            Camera.InvalidateFrame(frame);
        }
    }
}