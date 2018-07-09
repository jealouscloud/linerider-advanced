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
        private RiderFrame _flag;
        private Track _track;
        private int _iteration = 6;
        private bool _renderriderinvalid = true;
        private RiderFrame _renderrider = null;
        private ResourceSync _tracksync = new ResourceSync();
        private ResourceSync _renderridersync = new ResourceSync();
        private SimulationRenderer _renderer = new SimulationRenderer();
        private bool _refreshtrack = false;
        private Vector2d _savedcamera;
        private float _savedzoom;
        private bool _hasstopped = true;
        private float _zoom = Constants.DefaultZoom;
        private EditorGrid _cells = new EditorGrid();

        public readonly GameScheduler Scheduler = new GameScheduler();
        /// <summary>
        /// A sync object for loading track files
        /// </summary>
        public readonly object LoadSync = new object();
        public readonly Stopwatch FramerateWatch = new Stopwatch();
        public readonly FPSCounter FramerateCounter = new FPSCounter();
        private bool _paused = false;
        public float Zoom
        {
            get
            {
                return _zoom;
            }
            set
            {
                _zoom = (float)MathHelper.Clamp(value, Constants.MinimumZoom, Settings.Local.MaxZoom);
                Invalidate();
            }
        }
        public Song Song
        {
            get
            {
                return _track.Song;
            }
            set
            {
                _track.Song = value;
            }
        }
        public ICamera Camera { get; private set; }
        public int LineCount => _track.Lines.Count;
        public int BlueLines => _track.BlueLines;
        public int RedLines => _track.RedLines;
        public int GreenLines => _track.SceneryLines;
        public int TrackChanges
        {
            get
            {
                return Math.Abs(UndoManager.ActionCount - _prevSaveUndoPos);
            }
        }
        public float StartZoom
        {
            get
            {
                return _track.StartZoom;
            }
            set
            {
                if (_track.StartZoom != value)
                {
                    _track.StartZoom = value;
                    Timeline.Restart(_track.GetStart(), _track.StartZoom);
                    Zoom = Timeline.GetFrameZoom(Offset);
                    UseUserZoom = false;
                }
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
        public bool Playing => !Paused;
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
        public bool UseUserZoom = false;
        public Editor()
        {
            _track = new Track();
            Timeline = new Timeline(_track);
            Timeline.FrameInvalidated += FrameInvalidated;
            InitCamera();
            Offset = 0;
            UndoManager = new UndoManager();
            Paused = true;
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
            drawOptions.DrawFlag = _flag != null && !TrackRecorder.Recording;
            if (drawOptions.DrawFlag)
            {
                drawOptions.FlagRider = _flag.State;
            }
            drawOptions.Blend = blend;
            drawOptions.NightMode = Settings.NightMode;
            drawOptions.GravityWells = Settings.Editor.RenderGravityWells;
            drawOptions.LineColors = !Settings.PreviewMode && (!Playing || Settings.ColorPlayback);
            drawOptions.KnobState = KnobState.Hidden;
            var selectedtool = CurrentTools.SelectedTool;
            if (!Playing &&
                (selectedtool == CurrentTools.MoveTool ||
                selectedtool == CurrentTools.SelectTool))
            {
                drawOptions.KnobState = KnobState.Shown;
            }
            drawOptions.Paused = Paused;
            drawOptions.Rider = RenderRiderInfo.State;
            drawOptions.ShowContactLines = Settings.Editor.DrawContactPoints;
            drawOptions.ShowMomentumVectors = Settings.Editor.MomentumVectors;
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
        public void RedrawLine(GameLine line)
        {
            _renderer.RedrawLine(line);
        }
        public void ZoomBy(float percent)
        {
            if (Math.Abs(percent) < 0.00001)
                return;
            UseUserZoom = true;
            Zoom = Zoom + (Zoom * percent);
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
            Timeline.Restart(_track.GetStart(), _track.StartZoom);
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
            Timeline.NotifyChanged();
            InvalidateRenderRider();
            Invalidate();
        }
        internal void RestoreFlag(RiderFrame flag)
        {
            _flag = flag;
        }
        public void ResetSpeedDefault(bool resetscheduler = true)
        {
            Scheduler.DefaultSpeed();
            if (resetscheduler)
            {
                Scheduler.Reset();
            }
        }
        public void Flag(int offset, bool canremove = true)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException("Cannot set a flag to a negative frame");
            if (canremove && _flag != null && _flag.FrameID == offset)
            {
                _flag = null;
            }
            else
            {
                _flag = new RiderFrame { State = Timeline.GetFrame(offset), FrameID = offset };
            }
            Invalidate();
        }

        public void NextFrame()
        {
            SetFrame(++Offset);
        }

        public void PreviousFrame()
        {
            SetFrame(Math.Max(0, Offset - 1));
        }
        public void TogglePause()
        {
            Paused = !Paused;
            if (Playing && _hasstopped)
            {
                _savedcamera = Camera.GetCenter();
                _savedzoom = Zoom;
                _hasstopped = false;
            }
            Scheduler.Reset();
            if (Paused)
            {
                Camera.BeginFrame(1, Zoom);
                Camera.SetFrameCenter(Camera.GetCenter(true));
            }
            else
            {
                UpdateCamera();
            }
            Invalidate();
        }
        public void StartFromFlag()
        {
            CancelTriggers();
            UseUserZoom = false;
            var frame = _flag != null ? _flag.FrameID : 0;
            Start(frame);
        }
        public void StartIgnoreFlag()
        {
            CancelTriggers();
            UseUserZoom = false;
            Start(0);
        }
        private void Start(int frameid)
        {
            if (_hasstopped)
            {
                _savedcamera = Camera.GetCenter();
                _savedzoom = Zoom;
                _hasstopped = false;
            }
            Paused = false;
            Offset = frameid;
            IterationsOffset = 6;
            Camera.SetFrameCenter(Timeline.GetFrame(frameid).CalculateCenter());

            game.UpdateCursor();
            switch (Settings.PlaybackZoomType)
            {
                case 0://default
                    UseUserZoom = false;
                    Zoom = Timeline.GetFrameZoom(Offset);
                    break;
                case 1://current
                    UseUserZoom = true;
                    break;
                case 2://specific
                    UseUserZoom = true;
                    Zoom = Settings.PlaybackZoomValue;
                    break;
            }
            Scheduler.Reset();
            FramerateCounter.Reset();
            InvalidateRenderRider();
            Invalidate();
        }
        public void Stop()
        {
            Paused = true;
            if (!_hasstopped)
            {
                Zoom = _savedzoom;
                Camera.SetFrameCenter(_savedcamera);
                _hasstopped = true;
            }
            CancelTriggers();
            int frame = _flag != null ? _flag.FrameID : 0;
            SetFrame(frame);
            Camera.BeginFrame(1, Zoom);
            Camera.SetFrameCenter(Camera.GetCenter(true));
            Invalidate();
        }
        public void PlaybackSpeedUp()
        {
            var index = Array.IndexOf(
                Constants.MotionArray,
                Scheduler.UpdatesPerSecond);
            Scheduler.UpdatesPerSecond = Constants.MotionArray[
                Math.Min(
                Constants.MotionArray.Length - 1,
                index + 1)];
        }

        public void PlaybackSpeedDown()
        {
            var index = Array.IndexOf(
                Constants.MotionArray,
                Scheduler.UpdatesPerSecond);
            Scheduler.UpdatesPerSecond =
                Constants.MotionArray[Math.Max(0, index - 1)];
        }
        public void SetFrame(int frame, bool updateslider = true)
        {
            Offset = frame;
            Timeline.GetFrame(frame);
            if (frame + 1 > FrameCount)
                FrameCount = frame + 1;
            IterationsOffset = 6;
            InvalidateRenderRider();
            game.Invalidate();
        }

        public void Update(int times)
        {
            if (game.Canvas.Scrubbing)
            {
                UpdateCamera();
                return;
            }
            for (var i = 0; i < times; i++)
            {
                NextFrame();
                UpdateCamera();
            }
        }

        public void UpdateCamera(bool reverse = false)
        {
            if (!UseUserZoom)
                Zoom = Timeline.GetFrameZoom(Offset);
            Camera.SetFrame(Offset);
            if (Paused)
            {
                Camera.BeginFrame(1, Zoom);
                Camera.SetFrameCenter(Camera.GetCenter(true));
            }
        }

        public void BackupTrack(bool Crash = true)
        {
            try
            {
                if (_track.Lines.Count == 0)
                    return;
                game.Canvas.Loading = true;
                using (var trk = CreateTrackReader())
                {
                    if (Crash)
                    {
                        TrackIO.SaveTrackToFile(_track, "Crash Backup");
                    }
                    else
                    {
                        if (TrackChanges > 50)
                        {
                            TrackIO.CreateAutosave(_track);
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
                game.Canvas.Loading = false;
                Invalidate();
            }
        }

        public void ChangeTrack(Track trk)
        {
            lock (LoadSync)
            {
                using (_tracksync.AcquireWrite())
                {
                    AudioService.Stop();
                    CurrentTools.SelectedTool.Stop();
                    _loadingTrack = true;
                    Stop();
                    _flag = null;
                    _track = trk;

                    Timeline = new Timeline(trk);
                    Timeline.FrameInvalidated += FrameInvalidated;
                    InitCamera();
                    _refreshtrack = true;
                    _cells.Clear();
                    foreach (var line in trk.LineLookup.Values)
                    {
                        _cells.AddLine(line);
                    }
                    Reset();
                    Camera.SetFrameCenter(Timeline.GetFrame(0).CalculateCenter());
                    _loadingTrack = false;
                    if (CurrentTools.SelectedTool.Active)
                    {
                        CurrentTools.SelectedTool.Stop();
                    }
                    UndoManager = new UndoManager();
                    ResetTrackChangeCounter();
                }
            }
            Invalidate();
            GC.Collect();//this is probably safest place to make the gc work
            MoveStartWarned = false;
        }
        public void QuickSave()
        {
            if (TrackIO.QuickSave(_track))
            {
                ResetTrackChangeCounter();
                Settings.LastSelectedTrack = _track.Filename;
                Settings.Save();
            }
            else
            {
                game.Canvas.ShowSaveDialog();
            }
        }
        public void ResetTrackChangeCounter()
        {
            this._prevSaveUndoPos = UndoManager.ActionCount;
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
                game.Canvas.Loading = true;
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
                else if (string.Equals(
                    Path.GetExtension(lasttrack),
                    ".json",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var trackname = TrackIO.GetTrackName(lasttrack);
                    lock (LoadSync)
                    {
                        var track = JSONLoader.LoadTrack(lasttrack);
                        if (track.Name == Constants.DefaultTrackName ||
                        string.IsNullOrEmpty(track.Name))
                            track.Name = trackname;
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
                game.Canvas.Loading = false;
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
            //todo
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
                if (Settings.PredictiveCamera)
                    Camera = new PredictiveCamera();
                else
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