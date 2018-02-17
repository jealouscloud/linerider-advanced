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
using linerider.Rendering;
using linerider.Game;
using OpenTK;
using System.IO;
using System.Threading;
using Gwen.Controls;
using linerider.Tools;
using linerider.Audio;
using linerider.Utils;
using linerider.Lines;
using linerider.IO;

namespace linerider
{
    /// <summary>The interface for communicating with the game track object</summary>
    public class Editor : GameService
    {
        internal class Tracklocation
        {
            public int Frame;
            public int Iteration;
            public Rider State;
            public List<int> diagnosis = null;
        }

        public readonly Stopwatch Fpswatch = new Stopwatch();
        internal readonly FPSCounter FpsCounter = new FPSCounter();
        /// <summary>
        /// indicates if we're in playbackmode
        /// </summary>
        public bool PlaybackMode { get; private set; }
        private bool _paused = false;
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
        public float Zoom = Constants.DefaultZoom;
        public Camera Camera { get; private set; }
        public List<LineTrigger> ActiveTriggers = new List<LineTrigger>();
        private float _oldZoom = 1.0f;
        private Tracklocation _flag;
        private Track _track;
        private int _startFrame;
        private ResourceSync _tracksync = new ResourceSync();
        private ResourceSync _playbacksync = new ResourceSync();
        private ResourceSync _renderridersync = new ResourceSync();
        private SimulationRenderer _renderer = new SimulationRenderer();
        /// <summary>
        /// The current number of frames since start (incl flag)
        /// </summary>
        public int Offset { get; private set; }
        public int CurrentFrame => PlaybackMode ? Offset + _startFrame : 0;
        public int LineCount => _track.Lines.Count;
        public bool SimulationNeedsDraw = false;
        public PlaybackBufferManager BufferManager;
        public HitTestManager HitTest = new HitTestManager();
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
        public bool ZeroStart
        {
            get { return _track.ZeroStart; }
            set { _track.ZeroStart = value; }
        }

        private Tracklocation _renderrider = null;
        public Rider RenderRider
        {
            get
            {
                using (_renderridersync.AcquireRead())
                {
                    return _renderrider.State;
                }
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
        private bool _refreshtrack = false;

        public Editor()
        {
            Camera = new Camera();
            _track = new Track();
            _track.ActiveTriggers = ActiveTriggers;
            Offset = 0;
            BufferManager = new PlaybackBufferManager(_track.Grid);
            _renderrider = new Tracklocation() { Frame = 0, State = _track.RiderStates[0], Iteration = 6 };
            UndoManager = new UndoManager();
        }
        public void Render(float blend)
        {
            if (_refreshtrack)
            {
                RefreshTrack();
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
            drawOptions.Playback = PlaybackMode;
            drawOptions.Rider = RenderRider;
            drawOptions.ShowContactLines = Settings.Local.DrawContactPoints;
            drawOptions.ShowMomentumVectors = Settings.Local.MomentumVectors;
            drawOptions.Zoom = Zoom;
            drawOptions.RiderDiagnosis = _renderrider.diagnosis;
            if (Settings.SmoothPlayback && Playing && Offset > 0 && blend < 1)
            {
                //interpolate between last frame and current one
                drawOptions.Rider = Rider.Lerp(_track.RiderStates[Offset - 1], _track.RiderStates[Offset], blend);
            }
            var changes = HitTest.SetFrame(Offset);
            foreach (var change in changes)
            {
                GameLine line;
                if (_track.LineLookup.TryGetValue(change, out line))
                {
                    _renderer.RedrawLine(line);
                }
            }

            _renderer.Render(_track, Camera, drawOptions);
        }

        /// <summary>
        /// Function to be called after updating the playback buffer
        /// </summary>
        public void UpdateRenderRider()
        {
            SimulationNeedsDraw = true;
            Tracklocation newrider;
            using (_renderridersync.AcquireRead())
                newrider = _renderrider;

            newrider.Frame = Offset;
            Rider rider;
            bool isiteration = newrider.Iteration < 6 && newrider.Frame > 0;
            using (_playbacksync.AcquireRead())
            {
                if (isiteration)
                {
                    rider = _track.RiderStates[newrider.Frame - 1];
                }
                else
                {
                    rider = _track.RiderStates[newrider.Frame];
                }
            }
            using (var trk = CreateTrackReader())
            {
                newrider.diagnosis = trk.Diagnose(
                    rider,
                    Math.Min(6, newrider.Iteration + 1));
                if (isiteration)
                {
                    newrider.State = trk.TickBasic(rider, newrider.Iteration);
                }
                else
                {
                    newrider.State = rider;
                }
            }
            using (_renderridersync.AcquireWrite())
            {
                _renderrider = newrider;
            }
            SimulationNeedsDraw = true;
        }
        public Rider GetStart()
        {
            return _track.GetStart();
        }
        public void Reset()
        {
            using (_playbacksync.AcquireWrite())
            {
                _track.Reset();
                SetFrame(0);
            }
        }
        public bool FastGridCheck(double x, double y)
        {
            var chunk = _track.QuickGrid.GetCellFromPoint(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        public bool GridCheck(double x, double y)
        {
            var chunk = _track.Grid.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        /// <summary>
        /// Enters a state where no other thread can modify the playback state
        /// </summary>
        public PlaybackReader CreatePlaybackReader()
        {
            return PlaybackReader.AcquireRead(_playbacksync, _track);
        }
        /// <summary>
        /// Acquires read access to the playback state
        /// Upgradable to a writer
        /// </summary>
        public ResourceSync.ResourceLock CreatePlaybackUpgradableReader()
        {
            return _playbacksync.AcquireUpgradableRead();
        }

        /// <summary>
        /// Function for indicating the physics of the track have changed, so inform buffermanager
        /// </summary>
        public void NotifyTrackChanged()
        {
            if (PlaybackMode)
            {
                BufferManager.Update();
            }
            game.Canvas.DisableFlagTooltip();
        }
        internal void RestoreFlag(Tracklocation flag)
        {
            _flag = flag;
        }

        public void Flag()
        {
            using (CreatePlaybackReader())
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
            _renderer.RefreshTrack(_track);
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
                HitTest.Reset();
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
            BufferManager.Cancel();
            HitTest.Reset();
            using (_playbacksync.AcquireWrite())
            {
                FpsCounter.Reset(40);
                Fpswatch.Restart();
                PlaybackMode = true;
                Paused = false;
                _startFrame = 0;
                Offset = 0;
                if (_flag == null || ignoreflag)
                {
                    _track.Reset();
                }
                else
                {
                    _track.Reset(_flag.State);
                    _startFrame = _flag.Frame;
                }
                Camera.SetFrameCenter(_track.RiderStates[0].CalculateCenter());
                game.UpdateCursor();
                game.Scheduler.UpdatesPerSecond =
                    (int)(Math.Round(Settings.Local.DefaultPlayback * 40));
                game.Canvas.ShowPlaybackUI();
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
                Fpswatch.Restart();
                if (ghoststart && _flag != null)
                {
                    foreach (var v in ActiveTriggers)
                    {
                        v.Reset();
                    }
                    ActiveTriggers.Clear();
                    _track.Reset();
                    Offset = 0;
                    using (CreateTrackReader())
                    {
                        for (int i = 0; i < _flag.Frame; i++)
                        {
                            AddFrame();
                        }
                        SetFrame(EndFrameID);

                    }
                    if (!_track.RiderStates[Offset].Body.CompareTo(_flag.State.Body))
                    {
                        game.Canvas.SetFlagState(false);
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
                UpdateRenderRider();
                game.Invalidate();
            }
        }
        public void SetFrame(int frame, bool updateslider = true)
        {
            if (frame > _track.RiderStates.Count)
            {
                throw new Exception("unsupported frameskip to " + (frame - _track.RiderStates.Count));
            }
            using (CreatePlaybackReader())
            {
                if (frame == _track.RiderStates.Count)
                {
                    AddFrame();
                }
                Offset = frame;
                IterationsOffset = 6;
                UpdateRenderRider();
            }
            game.Canvas.UpdateIterationUI();
            if (updateslider)
            {
                game.Canvas.UpdateScrubber();
            }
            game.Invalidate();
        }
        private void AddFrame()
        {
            using (CreatePlaybackReader())
            {
                using (_tracksync.AcquireRead())
                {
                    var collisions = _track.AddFrame(true);
                    HitTest.AddFrame(collisions);
                }
            }
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
            Camera.SetFrame(RenderRider);
            if (Settings.SmoothCamera)
            {
                Rider prediction;
                using (var trk = CreateTrackReader())
                {
                    prediction = trk.TickBasic(RenderRider);
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
                        TrackIO.SaveTrackToFile(_track, "Crash Backup", Settings.Local.CurrentSong?.ToString());
                    }
                    else
                    {
                        TrackIO.CreateAutosave(_track, Settings.Local.CurrentSong?.ToString());
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
            using (_tracksync.AcquireWrite())
            {
                _flag = null;
                if (_track != null && _track.ActiveTriggers == ActiveTriggers)
                    _track.ActiveTriggers = null;
                _track = trk;
                _track.ActiveTriggers = ActiveTriggers;
            }
            BufferManager.Reset(trk.Grid);
            UndoManager = new UndoManager();
            HitTest.Reset();
            _refreshtrack = true;
            Reset();
            Camera.SetFrameCenter(trk.StartOffset);
            GC.Collect();//this is the safest place to collect
        }
        public void OnLoad()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    using (_tracksync.AcquireWrite())
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
                            string trackname = Path.GetFileNameWithoutExtension(lasttrack);
                            var dirname = Path.GetDirectoryName(lasttrack);
                            var dirs = Directory.GetDirectories(Constants.TracksDirectory);
                            foreach (var dir in dirs)
                            {
                                if (string.Equals(
                                    dirname,
                                    dir,
                                    StringComparison.InvariantCulture))
                                {
                                    trackname = Path.GetFileName(dirname);
                                    break;
                                }
                            }

                            ChangeTrack(TRKLoader.LoadTrack(lasttrack, trackname));
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
            });
        }
        public TrackWriter CreateTrackWriter()
        {
            return TrackWriter.AcquireWrite(_tracksync, _track, _renderer, UndoManager, BufferManager);
        }
        public TrackReader CreateTrackReader()
        {
            return TrackReader.AcquireRead(_tracksync, _track);
        }

        internal Tracklocation GetFlag()
        {
            return _flag;
        }
    }
}