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

namespace linerider
{
    public class GLTrack : GameService
    {
        internal class Tracklocation
        {
            public int Frame;
            public Rider State;
        }

        private class TrackUpdater
        {
            public static readonly TrackUpdater Instance;
            private readonly ManualResetEvent _waithandle = new ManualResetEvent(false);
            private bool _reset;

            static TrackUpdater()
            {
                Instance = new TrackUpdater();
            }

            public TrackUpdater()
            {
                new Thread(delegate ()
                {
                    var updatestart = -1;
                    var track = game.Track;
                    while (true)
                    {
                        if (_waithandle.WaitOne())
                        {
                            if (updatestart == -1)
                            {
                                updatestart = track._track.RiderStates.Count;
                            }
                            _waithandle.Reset();
                            _reset = false;
                            track.EnterPlayback();
                            try
                            {
                                if (Update(ref updatestart))
                                {
                                    updatestart = -1;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                Reset();
                            }
                            finally
                            {
                                track.ExitPlayback();
                            }
                        }
                    }
                })
                { IsBackground = true }.Start();
            }

            public void Reset()
            {
                _reset = true;
                _waithandle.Set();
            }

            private bool Update(ref int start)
            {
                var track = game.Track._track;
                var calc = track.CalculateUpdateStart();
                calc = Math.Max(0, calc - 1);
                if (calc < start)
                    start = calc;
                var count = track.RiderStates.Count - start;
                if (count == 0) return false; //nothing to be done
                var states = new List<Rider>(count);
                var state = track.RiderStates[start].Clone();
                var coll = new List<ConcurrentDictionary<int, StandardLine>>();
                for (var i = 0; i < count; i++)
                {
                    if (_reset) //need a complete reset as per another thread's request
                        return false;
                    states.Add(state.Clone());
                    ConcurrentDictionary<int, StandardLine> c;
                    game.Track.EnterTrackRead();
                    c = track.Tick(state);
                    game.Track.ExitTrackRead();
                    coll.Add(c);
                }
                if (_reset)
                    return false;
                for (var i = 0; i < count; i++)
                {
                    track.RiderStates[start + i] = states[i];
                }
                track.RiderState = track.RiderStates[track.Frame].Clone();
                if (_reset)
                    return false;

                lock (track.Collisions)
                {
                    for (var i = 0; i < coll.Count; i++)
                    {
                        track.Collisions[start + i] = coll[i];
                    }
                    if (_reset)
                        return false;
                }
                track.CalculateAllCollidedLines();
                return !_reset;
            }
        }

        public readonly Stopwatch Fpswatch = new Stopwatch();
        internal readonly FPSCounter FpsCounter = new FPSCounter();
        private readonly object _trackWriteLock = new object();
        private readonly object _trackPlaybackLock = new object();
        private TrackRenderer _renderer;
        private SceneryRenderer _sceneryrenderer;
        public bool Animating;
        public bool Paused;
        public float Zoom = 4.0f;
        public Camera Camera;
        public List<LineTrigger> ActiveTriggers = new List<LineTrigger>();
        private float _oldZoom = 1.0f;
        private Tracklocation _flag;
        private Track _track;
        private int _readCount;
        private int _startFrame;
        private FloatRect _trackrect;
        public int Frame => _track.Frame;
        public int CurrentFrame => Animating ? _track.Frame + _startFrame : 0;
        public int LineCount => _track.Lines.Count;
        public bool SimulationNeedsDraw = false;

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
        public bool ZeroStart
        {
            get { return _track.ZeroStart; }
            set { _track.ZeroStart = value; }
        }

        public FloatRect RiderRect => _track.RiderRect;

        public string Name
        {
            get { return _track.Name; }
            set { _track.Name = value; }
        }

        public Vector2d StartPosition
        {
            get { return _track.Start; }
            set { _track.Start = value; }
        }

        public Rider RiderState => _track.RiderState;
        public UndoManager UndoManager => _track.UndoManager;
        public bool Playing => Animating && !Paused;

        public GLTrack()
        {
            Camera = new Camera();
            _renderer = new TrackRenderer();
            _sceneryrenderer = new SceneryRenderer();
            _track = new Track();
            _track.RiderState.Reset(new Vector2d(0, 0), _track);
        }

        public bool FastGridCheck(double x, double y)
        {
            var chunk = _track.FastChunks.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }
        public bool GridCheck(double x, double y)
        {
            var chunk = _track.Chunks.PointToChunk(new Vector2d(x, y));
            return chunk != null && chunk.Count != 0;
        }

        public void SetRiderState(Rider r)
        {
            _track.RiderState = r;
        }

        /// <summary>
        ///     Enters a state where no thread can modify track data so you can safely read the track
        /// </summary>
        public void EnterTrackRead()
        {
            //enter write lock to ensure the track isnt being written to
            lock (_trackWriteLock)
            {

                Interlocked.Increment(ref _readCount);
            }
        }

        public bool TryEnterTrackRead()
        {
            var ret = false;
            Monitor.Enter(_trackWriteLock, ref ret);
            if (ret)
            {
                Interlocked.Increment(ref _readCount);
                Monitor.Exit(_trackWriteLock);
            }
            return ret;
        }

        public void ExitTrackRead()
        {
            if (_readCount > 0)
                Interlocked.Decrement(ref _readCount);
        }
        int playbackcount = 0;
        /// <summary>
        ///     Enters a state where no other thread can modify the track state
        /// </summary>
        public void EnterPlayback()
        {
            System.Threading.Interlocked.Increment(ref playbackcount);
            Monitor.Enter(_trackPlaybackLock);
        }

        public void ExitPlayback()
        {
            Monitor.Exit(_trackPlaybackLock);
            System.Threading.Interlocked.Decrement(ref playbackcount);
        }

        /// <summary>
        ///     Enters a state for modifying the track safely.
        /// </summary>
        public void EnterTrackReadWrite()
        {
            Monitor.Enter(_trackWriteLock);
            while (_readCount != 0)
                Thread.Sleep(0);
        }

        public void ExitTrackReadWrite()
        {
            Monitor.Exit(_trackWriteLock);
        }

        public bool IsCurrentFrameCrashed(out bool failed)
        {
            if (Animating)
            {
                if (TryEnterTrackRead())
                {
                    failed = false;
                    var ret = false;
                    if (game.IterationsOffset != 6 && _track.Frame != 0 && Animating && Paused)
                    {
                        var renderstate = _track.RiderStates[_track.Frame - 1].Clone();
                        _track.TickWithIterations(renderstate);
                        ret = renderstate.iterations[game.IterationsOffset].Crashed;
                    }
                    else
                    {
                        ret = _track.RiderStates[_track.Frame].Crashed;
                    }
                    ExitTrackRead();
                    return ret;
                }
            }
            failed = true;
            return false;
        }

        public void AddLine(Line l)
        {
            EnterTrackReadWrite();
            _track.AddLines(l);
            ExitTrackReadWrite();
            if (l is SceneryLine)
            {
                _sceneryrenderer.AddLine(l);
            }
            else
            {
                _renderer.AddLine((StandardLine)l);
            }
        }
        public void LineChanged(Line l)
        {
            if (l is SceneryLine)
            {
                _sceneryrenderer.LineChanged(l);
            }
            else
            {
                _renderer.LineChanged((StandardLine)l);
            }
        }

        public void RemoveLine(Line l)
        {
            EnterTrackReadWrite();
            _track.RemoveLine(l);
            ExitTrackReadWrite();
            if (l is SceneryLine)
            {
                _sceneryrenderer.RemoveLine(l);
            }
            else
            {
                _renderer.RemoveLine((StandardLine)l);
            }
        }

        public void Erase(Vector2d pos, LineType t)
        {
            EnterTrackReadWrite();
            var e = _track.Erase(pos, t, Zoom);
            ExitTrackReadWrite();
            foreach (var v in e)
            {
                if (v is StandardLine)
                {
                    TrackUpdated();
                    break;
                }
            }
            foreach (var v in e)
            {
                if (v is SceneryLine)
                {
                    _sceneryrenderer.RemoveLine(v);
                }
                else
                {
                    _renderer.RemoveLine((StandardLine)v);
                }
            }
            if (e.Count != 0)
                game.Invalidate();
        }

        public Line GetLastLine()
        {
            if (_track.Lines.Count == 0)
                return null;
            return _track.Lines[_track.Lines.Count - 1];
        }

        public Line GetFirstLine()
        {
            if (_track.Lines.Count == 0)
                return null;
            return _track.Lines[0];
        }

        public void TrackUpdated()
        {
            if (Animating)
                TrackUpdater.Instance.Reset();
        }

        public List<Line> GetLinesInRect(FloatRect rect, bool precise)
        {
            EnterTrackRead();
            var ret = _track.GetLinesInRect(rect, precise);
            ExitTrackRead();
            return ret;
        }

        private struct RiderDrawCommand
        {
            public Rider state;
            public int iteration;
            public float opacity;
            public bool scarf;
            public bool contactpoints;
            public bool momentum;

            public RiderDrawCommand(int iteration, Rider state, float opacity, bool scarf, bool contactpoints,
                bool momentum)
            {
                this.state = state;
                this.iteration = iteration;
                this.opacity = opacity;
                this.scarf = scarf;
                this.contactpoints = contactpoints;
                this.momentum = momentum;
            }
            public RiderDrawCommand(Rider state, float opacity, bool scarf, bool contactpoints, bool momentum)
            {
                this.state = state;
                this.iteration = -1;
                this.opacity = opacity;
                this.scarf = scarf;
                this.contactpoints = contactpoints;
                this.momentum = momentum;
            }
        }
        public void Render(float blend)
        {
            Rider drawrider = RiderState.Clone();
            FloatRect rect = Camera.GetViewport().ToFloatRect();
            if (Settings.SmoothPlayback && Playing && _track.Frame != 0 && blend < 1)
            {
                drawrider = _track.RiderStates[_track.Frame - 1].Lerp(RiderState, blend);
            }
            var st = OpenTK.Input.Keyboard.GetState();
            var needsredraw = (!_trackrect.Contains(rect.Left, rect.Top) ||
                               !_trackrect.Contains(rect.Left + rect.Width, rect.Top + rect.Height));
            if (!needsredraw)
            {
                var viewport = rect.Inflate(rect.Width, rect.Height);
                if (viewport.Width < _trackrect.Width / 3 && viewport.Height < _trackrect.Height / 3)
                    needsredraw = true;
            }
            var drawcolor = !Animating || Paused || game.SettingColorPlayback;
            if (game.SettingPreviewMode)
            {
                drawcolor = false;
            }
            var knob = 0;
            var adjustTool = game.SelectedTool as LineAdjustTool;
            if (adjustTool != null && (!Animating || Paused))
            {
                knob = 1;
                if ((!adjustTool.Started && (st[OpenTK.Input.Key.AltLeft] || st[OpenTK.Input.Key.AltRight])) ||
                    adjustTool.LifeLock)
                    knob = 2;
            }
            if (needsredraw || RequiresUpdate)
            {
                EnterTrackRead();
                var viewport = rect.Inflate(rect.Width, rect.Height);
                var lines = _track.GetLinesInRect(viewport, false, true);
                _trackrect = viewport;
                _renderer.UpdateViewport(lines);
                ExitTrackRead();
            }
            _renderer.Render(_track, drawcolor, knob, game.SettingRenderGravityWells);
            _sceneryrenderer.Render(drawcolor);
            List<RiderDrawCommand> commands = new List<RiderDrawCommand>();

            EnterPlayback();
            if (game.IterationsOffset != 6 && Frame != 0 && Animating && Paused)
            {
                var renderstate = _track.RiderStates[Frame - 1].Clone();
                _track.TickWithIterations(renderstate);
                commands.Add(new RiderDrawCommand(game.IterationsOffset, renderstate, game.SettingDrawContactPoints ? 0.4f : 1, true,
                    game.SettingDrawContactPoints, game.SettingMomentumVectors));
            }
            else
            {
                commands.Add(new RiderDrawCommand(drawrider, game.SettingDrawContactPoints ? 0.4f : 1, true,
                    game.SettingDrawContactPoints, game.SettingMomentumVectors));
            }
            if (game.SettingOnionSkinning && _track.RiderStates.Count != 0 && Animating)
            {
                const int onions = 10;
                Rider[] onionstates = new Rider[onions * 2];
                for (int i = 0; i < onions; i++)
                {
                    var frame = _track.Frame - (onions - i);
                    if (frame > 0)
                    {
                        onionstates[i] = _track.RiderStates[frame].Clone();
                    }
                }
                Rider positivestate = _track.RiderStates[_track.Frame].Clone();

                for (int i = onions + 1; i < onions * 2; i++)
                {
                    _track.Tick(positivestate);
                    onionstates[i] = positivestate.Clone();
                }

                foreach (var state in onionstates)
                {
                    if (state == null)
                        continue;
                    commands.Add(new RiderDrawCommand(state, 0.2f, false,
                        game.SettingDrawContactPoints, game.SettingMomentumVectors));
                }
            }
            if (_flag != null)
            {
                commands.Add(new RiderDrawCommand(_flag.State.Clone(), 0.6f, false,
                    game.SettingDrawContactPoints, game.SettingMomentumVectors));
            }
            ExitPlayback();
            foreach (var v in commands)//todo rider vbo for every rider, massive improvements.
            {
                if (v.iteration != -1)
                {
                    GameRenderer.DrawIteration(v.opacity, v.state, v.iteration, v.momentum, v.contactpoints);
                }
                else
                {
                    GameRenderer.DrawRider(v.opacity, v.state, v.scarf, v.contactpoints, v.momentum);
                }
            }
        }

        public void Reset(Rider r)
        {
            r.Reset(_track.Start, _track);
        }

        internal void RestoreFlag(Tracklocation flag)
        {
            _flag = flag;
        }

        public void Flag()
        {
            EnterPlayback();
            if (Animating)
            {
                _flag = new Tracklocation { State = _track.RiderState.Clone(), Frame = CurrentFrame };
            }
            else
            {
                _flag = null;
            }
            ExitPlayback();
            game.Canvas.DisableFlagTooltip();
            game.Invalidate();
        }

        public void RefreshTrack()
        {
            _sceneryrenderer.InitializeTrack(_track.Lines);
            _sceneryrenderer.RequiresUpdate = true;
            _renderer.UpdateViewport(null);
            _renderer.RequiresUpdate = true;
        }

        public void NextFrame()
        {
            EnterPlayback();
            game.SetIteration(6, true);
            _track.NextFrame();
            if (_track.RiderStates.Count > 40 * (20 * 60))
            {
                Stop();
            }
            ExitPlayback();
            var slider = (HorizontalIntSlider)game.Canvas.FindChildByName("timeslider");
            slider.Min = 0;
            slider.Max = _track.RiderStates.Count - 1;
            slider.Value = _track.Frame;
        }

        public void PreviousFrame()
        {
            EnterPlayback();
            game.SetIteration(6, true);
            _track.SetFrame(Math.Max(0, _track.Frame - 1));
            ExitPlayback();
            var slider = (HorizontalIntSlider)game.Canvas.FindChildByName("timeslider");
            slider.Min = 0;
            slider.Max = _track.RiderStates.Count - 1;
            slider.Value = _track.Frame;
        }

        public void TryDisconnectLines(StandardLine l1, StandardLine l2, bool undo = true)
        {
            if (l1 == null || l2 == null) return;
            Vector2d joint;
            if (l1.Position == l2.Position || l1.Position == l2.Position2)
                joint = l1.Position;
            else if (l1.Position2 == l2.Position || l1.Position2 == l2.Position2)
                joint = l1.Position2;
            else
                return;
            //var leftlink = (l1.CompliantPosition == joint && l2.CompliantPosition2 == joint);
            var rightlink = (l1.CompliantPosition2 == joint && l2.CompliantPosition == joint);
            if (rightlink)
            {
                l1.Next = null;
                l1.RemoveExtension(StandardLine.ExtensionDirection.Right);
                l2.Prev = null;
                l2.RemoveExtension(StandardLine.ExtensionDirection.Left);
                if (undo)
                    UndoManager.AddExtensionChange(l1, l2, false);
            }
            else
            {
                l1.Prev = null;
                l1.RemoveExtension(StandardLine.ExtensionDirection.Left);
                l2.Next = null;
                l2.RemoveExtension(StandardLine.ExtensionDirection.Right);
                if (undo)
                    UndoManager.AddExtensionChange(l2, l1, false);
            }
        }

        public void TryConnectLines(StandardLine l1, StandardLine l2, bool undo = true)
        {
            if (l1 == null || l2 == null) return;
            Vector2d joint;
            if (l1.Position == l2.Position || l1.Position == l2.Position2)
                joint = l1.Position;
            else if (l1.Position2 == l2.Position || l1.Position2 == l2.Position2)
                joint = l1.Position2;
            else
                return;
            var leftlink = (l1.CompliantPosition == joint && l2.CompliantPosition2 == joint);
            var rightlink = (l1.CompliantPosition2 == joint && l2.CompliantPosition == joint);

            if (!leftlink && !rightlink) return;

            var diff1 = l2.CompliantPosition2 - l2.CompliantPosition;
            var diff2 = l1.CompliantPosition2 - l1.CompliantPosition;

            var angle1 = Angle.FromVector(diff1).Degrees;
            var angle2 = Angle.FromVector(diff2).Degrees;

            var anglediff1 = new Angle(angle1 - angle2).Degrees;
            var anglediff2 = new Angle(angle2 - angle1).Degrees;

            bool cmp1 = anglediff1 > 0 && anglediff1 <= 180;
            bool cmp2 = anglediff2 > 0 && anglediff2 <= 180;
            if ((rightlink) ? cmp2 : cmp1)
            {
                if (rightlink)
                {
                    l1.Next = l2;
                    l1.AddExtension(StandardLine.ExtensionDirection.Right);
                    l2.Prev = l1;
                    l2.AddExtension(StandardLine.ExtensionDirection.Left);
                    if (undo)
                        UndoManager.AddExtensionChange(l1, l2, true);
                }
                else
                {
                    l1.Prev = l2;
                    l1.AddExtension(StandardLine.ExtensionDirection.Left);
                    l2.Next = l1;
                    l2.AddExtension(StandardLine.ExtensionDirection.Right);
                    if (undo)
                        UndoManager.AddExtensionChange(l2, l1, true);
                }
            }
        }

        public void ChangeMade(Vector2d p1, Vector2d p2)
        {
            _track.ChangeMade(p1, p2);
        }

        public void Stop()
        {
            if (Animating)
            {
                Animating = false;
                Paused = false;

                Zoom = _oldZoom;
                Camera.Pop();
                _track.RiderState.Reset(_track.Start, _track);
                game.Scheduler.UpdatesPerSecond = 40;
                foreach (var v in ActiveTriggers)
                {
                    v.Reset();
                }
                ActiveTriggers.Clear();
                if (game.EnableSong)
                {
                    AudioService.Stop();
                }
                var canvas = game.Canvas;
                var buttons = canvas.FindChildByName("buttons");
                buttons.FindChildByName("pause").IsHidden = true;
                buttons.FindChildByName("start").IsHidden = false;
                var slider = canvas.FindChildByName("timeslider");
                slider.IsHidden = true;
                game.Canvas.FindChildByName("labeliterations").IsHidden = true;
                game.Canvas.FindChildByName("vslider", true).IsHidden = false;
                canvas.FindChildByName("btnfastforward").IsHidden = true;
                canvas.FindChildByName("btnslowmo").IsHidden = true;

                //incase recording mode was enabled at the start. There's a risk it was disabled during playback
                //if that's the case checking game.RecordingMode would fail but controls would remain invisible.
                //instead, we just by default ensure visibility
                {
                    buttons.IsHidden = false;
                    game.Canvas.FindChildByName("fps", true).IsHidden = false;
                    game.Canvas.FindChildByName("ppf", true).IsHidden = false;
                    game.Canvas.FindChildByName("labelplayback", true).IsHidden = false;
                    game.Canvas.FindChildByName("trackname", true).IsHidden = false;
                }
                game.Invalidate();
            }
        }

        public void TogglePause()
        {
            if (Animating)
            {
                EnterTrackRead();
                Paused = !Paused;
                var container = game.Canvas.FindChildByName("buttons");
                var start = container.FindChildByName("start");
                var pause = container.FindChildByName("pause");
                pause.IsHidden = Paused;
                start.IsHidden = !Paused;
                game.Scheduler.Reset();
                game.SetIteration(6, Paused);
                ExitTrackRead();
                if (game.EnableSong)
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
            }
        }


        public void Start(bool ignoreflag = false, bool clearcollidedlines = true, bool startmusic = true, bool ghoststart = false)
        {
            if (!Animating)
            {
                _oldZoom = Zoom;
                Camera.Push();
            }
            EnterPlayback();
            {
                FpsCounter.Reset(40);
                Fpswatch.Restart();
                Animating = true;
                Paused = false;
                _startFrame = 0;
                if (_flag == null || ignoreflag)
                {
                    _track.RiderState.Reset(_track.Start, _track);
                    _track.Reset();
                }
                else
                {
                    _track.RiderState = _flag.State.Clone();
                    _startFrame = _flag.Frame;
                    _track.Reset();
                }

                if (clearcollidedlines)
                {
                    lock (_track.Collisions)
                    {
                        _track.Collisions.Clear();
                        _track.Collisions.Add(new ConcurrentDictionary<int, StandardLine>());
                    }
                    _track.AllCollidedLines.Clear();
                }
                var cameracenter = _track.RiderState.CalculateCenter();

                Camera.SetFrame(RiderState.CalculateCenter(), false);
                UpdateCamera();
                game.UpdateCursor();
                game.Scheduler.UpdatesPerSecond =
                    (int)(Math.Round(game.SettingDefaultPlayback * 40));
                var canvas = game.Canvas;
                var buttons = canvas.FindChildByName("buttons");
                var slider = (HorizontalIntSlider)canvas.FindChildByName("timeslider");
                canvas.FindChildByName("btnfastforward").IsHidden = game.SettingRecordingMode;
                canvas.FindChildByName("btnslowmo").IsHidden = game.SettingRecordingMode;
                if (game.SettingRecordingMode)
                {
                    buttons.FindChildByName("pause").IsHidden = true;
                    buttons.FindChildByName("start").IsHidden = false;
                    game.Canvas.FindChildByName("trackname", true).IsHidden = true;
                    slider.IsHidden = true;
                    buttons.IsHidden = !game.SettingRecordingShowTools;
                    game.Canvas.FindChildByName("fps", true).IsHidden = !game.SettingShowFps;
                    game.Canvas.FindChildByName("ppf", true).IsHidden = !game.SettingShowPpf;
                    game.Canvas.FindChildByName("labelplayback", true).IsHidden = !game.SettingShowTimer;
                }
                else
                {
                    buttons.FindChildByName("pause").IsHidden = false;
                    buttons.FindChildByName("start").IsHidden = true;
                    game.Canvas.FindChildByName("trackname", true).IsHidden = false;
                    slider.IsHidden = false;
                    buttons.IsHidden = false;
                }
                game.Canvas.FindChildByName("labeliterations").IsHidden = true;
                game.Canvas.FindChildByName("vslider", true).IsHidden = true;
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

                    _track.RiderState.Reset(_track.Start, _track);
                    _track.Reset();
                    game.SetIteration(6, true);
                    for (int i = 0; i < _flag.Frame; i++)
                    {
                        _track.NextFrame();
                    }

                    for (var i = 0; i < _track.RiderState.ModelAnchors.Length; i++)
                    {
                        if (_track.RiderState.ModelAnchors[i].Position != _flag.State.ModelAnchors[i].Position ||
                            _track.RiderState.ModelAnchors[i].Prev != _flag.State.ModelAnchors[i].Prev)
                        {
                            game.Canvas.SetFlagTooltip(false);
                            game.Invalidate();
                            return;
                        }
                    }
                    slider.Min = 0;
                    slider.Max = _track.RiderStates.Count - 1;
                    slider.Value = _track.Frame;
                }
                if (startmusic && game.EnableSong)
                {
                    if (_flag != null)
                        game.UpdateSongPosition(CurrentFrame / 40f);
                    else
                        game.UpdateSongPosition(0);
                }
                game.Invalidate();
            }
            ExitPlayback();
        }

        public void SetFrame(int frame, bool updateslider = true)
        {
            _track.SetFrame(frame);
            if (updateslider)
            {
                var slider = (HorizontalIntSlider)game.Canvas.FindChildByName("timeslider");
                slider.Min = 0;
                slider.Max = _track.RiderStates.Count - 1;
                slider.Value = _track.Frame;
            }
            game.Invalidate();
        }

        public bool DoLifelock(bool ignoresetting, Line line)
        {
            var fail = false;
            if (!IsCurrentFrameCrashed(out fail) && !fail)
            {
                var pinklock = false;
                if (Settings.PinkLifelock)
                {
                    if (game.IterationsOffset == 6)
                    {
                        var diagnosis = _track.Diagnose(_track.RiderStates[_track.Frame]);
                        foreach (var d in diagnosis)
                        {
                            if (d >= 0)
                            {
                                pinklock = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        var renderstate = _track.RiderStates[_track.Frame - 1].Clone();
                        _track.TickWithIterations(renderstate);
                        var diagnosis = _track.DiagnoseIteration(renderstate, game.IterationsOffset);
                        foreach (var d in diagnosis)
                        {
                            if (d >= 0)
                            {
                                pinklock = true;
                                break;
                            }
                        }
                    }
                }
                if ((!game.HitTest || _track.IsLineCollided(line.ID)) && !pinklock)
                {
                    return true;
                }
            }
            return false;
        }

        public ConcurrentDictionary<int, StandardLine> Tick()
        {
            return Tick(RiderState);
        }

        public ConcurrentDictionary<int, StandardLine> Tick(Rider state)
        {
            var ret = _track.Tick(state);
            return ret;
        }

        public HashSet<int> DiagnoseIteration(Rider state, int it)
        {
            return _track.DiagnoseIteration(state, it);
        }

        public HashSet<int> Diagnose(Rider state)
        {
            return _track.Diagnose(state);
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
            Camera.SetFrame(RiderState.CalculateCenter(), true);
            if (Settings.SmoothCamera)
            {
                var clone = RiderState.Clone();
                Tick(clone);
                Camera.SetPrediction(clone.CalculateCenter());
            }
            SimulationNeedsDraw = true;
        }
        public void ExportAsSol()
        {
            if (_track.Lines.Count != 0)
            {
                var features = TrackLoader.TrackFeatures(_track);
                bool six_one;
                bool redmultiplier;
                bool scenerywidth;
                features.TryGetValue("SIX_ONE", out six_one);
                features.TryGetValue("REDMULTIPLIER", out redmultiplier);
                features.TryGetValue("SCENERY_WIDTH", out scenerywidth);
                if (six_one || redmultiplier || scenerywidth)
                {
                    var window = PopupWindow.Create(game.Canvas, game, "Unable to export SOL file due to it containing special LRA specific features,\nspecifically, " + (six_one ? "\nthe track is based on 6.1, " : "") + (redmultiplier ? "\nthe track uses red multiplier lines, " : "") + (scenerywidth ? "\nthe track uses varying scenery line width " : "") + "\nand therefore cannot be loaded", "Error!", true, false);

                    window.FindChildByName("Okay", true).Clicked += (o, e) =>
                    {
                        window.Close();
                    };
                }
                else
                {
                    var window = PopupWindow.Create(game.Canvas, game, "Are you sure you wish to save this track as an SOL file? It will overwrite any file named savedLines.sol", "Are you sure?", true, true);

                    window.FindChildByName("Okay", true).Clicked += (o, e) =>
                    {
                        window.Close();
                        TrackLoader.SaveTrackSol(_track);
                        game.Canvas.UpdateSOLFiles();
                    };
                    window.FindChildByName("Cancel", true).Clicked += (o, e) =>
                    {
                        window.Close();
                    };
                }
            }
        }

        public void BackupTrack(bool Crash = true)
        {
            try
            {
                if (_track.Lines.Count == 0)
                    return;
                var saveindex = 0;
                var trackfiles =
                    TrackLoader.EnumerateTRKFiles(Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar +
                                                  _track.Name);
                for (var i = 0; i < trackfiles.Length; i++)
                {
                    var s = Path.GetFileNameWithoutExtension(trackfiles[i]);
                    s = s.Remove(s.IndexOf(' '));
                    if (int.TryParse(s, out saveindex))
                    {
                        break;
                    }
                }
                saveindex++;
                if (saveindex < 2 && !Crash)
                    return;
                var save = Crash ? (saveindex + " " + "Crash Backup") : " Autosave";
                game.Loading = true;
                EnterTrackRead();
                {
                    TrackLoader.SaveTrackTrk(_track, save, game.CurrentSong?.ToString());
                }
                ExitTrackRead();
                game.Loading = false;
                game.Invalidate();
            }
            catch
            {
                //ignored
            }
        }

        public void ChangeTrack(Track trk)
        {
            _flag = null;
            _track = trk;
            RefreshTrack();
            trk.RiderState.Reset(trk.Start, trk);
            Camera.SetFrame(trk.Start, false);
        }

        /// <summary>
        ///     For moving lines
        /// DO NOT CALL WITHOUT CALLING ENTERTRACKREADWRITE
        /// </summary>
        public void RemoveLineFromGrid(Line sl)
        {
            _track.RemoveLineFromGrid(sl);
        }

        /// <summary>
        ///     For moving lines
        /// DO NOT CALL WITHOUT CALLING ENTERTRACKREADWRITE
        /// </summary>
        public void AddLineToGrid(Line sl)
        {
            _track.AddLineToGrid(sl);
        }

        internal Tracklocation GetFlag()
        {
            return _flag;
        }

        internal void Save(string savename, Audio.Song song)
        {
            TrackLoader.SaveTrackTrk(_track, savename, song.ToString());
        }
    }
}