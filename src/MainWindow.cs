//#define debuggrid
//#define debugcamera
//
//  GLWindow.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using Gwen;
using Gwen.Controls;
using linerider.Audio;
using linerider.Drawing;
using linerider.Rendering;
using linerider.IO;
using linerider.Tools;
using linerider.UI;
using linerider.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Key = OpenTK.Input.Key;
using Label = Gwen.Controls.Label;
using Menu = Gwen.Controls.Menu;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace linerider
{
    public class MainWindow : OpenTK.GameWindow
    {
        private enum Tools
        {
            None = 0,
            HandTool,
            LineTool,
            PencilTool,
            EraserTool,
            MoveTool,
            GwellTool
        }


        public readonly GameScheduler Scheduler = new GameScheduler();
        public Dictionary<string, MouseCursor> Cursors = new Dictionary<string, MouseCursor>();
        public Tool SelectedTool;
        public MsaaFbo MSAABuffer;
        public readonly PencilTool PencilTool;
        public readonly LineTool LineTool;
        public readonly EraserTool EraserTool;
        public readonly HandTool HandTool;
        public readonly MoveTool MoveTool;
        private readonly Stopwatch _autosavewatch = Stopwatch.StartNew();
        public GameCanvas Canvas;
        public bool Loading = false;

        public bool ReversePlayback = false;
        private bool _handToolOverride;
        private Gwen.Input.OpenTK _input;
        private bool _dragRider;
        private bool _invalidated;

        public bool EnableSnap
        {
            get { return !Settings.Local.DisableSnap && !InputUtils.Check(Hotkey.ToolDisableSnap); }
        }

        public Size RenderSize
        {
            get
            {
                if (TrackRecorder.Recording)
                {
                    return TrackRecorder.Recording1080p ? new Size(1920, 1080) : new Size(1280, 720);
                }
                return ClientSize;
            }
            set
            {
                ClientSize = value;
            }
        }
        public Vector2d ScreenPosition
            => Track.Camera.GetViewport(
                Track.Zoom,
                RenderSize.Width,
                RenderSize.Height).Vector;

        public Vector2d ScreenTranslation => -ScreenPosition;
        public Editor Track { get; }
        private bool _playbacktemp = false;
        public MainWindow()
            : base(
                1280,
                720,
                new GraphicsMode(new ColorFormat(24), 0, 0, 0, ColorFormat.Empty),
                   "Line Rider: Advanced",
                   GameWindowFlags.Default,
                   DisplayDevice.Default,
                   2,
                   0,
                   GraphicsContextFlags.Default)
        {
            SafeFrameBuffer.Initialize();
            PencilTool = new PencilTool();
            LineTool = new LineTool();
            EraserTool = new EraserTool();
            HandTool = new HandTool();
            MoveTool = new MoveTool();
            SelectedTool = PencilTool;
            Track = new Editor();
            VSync = VSyncMode.On;
            Context.ErrorChecking = false;
            WindowBorder = WindowBorder.Resizable;
            RenderFrame += (o, e) => { Render(); };
            UpdateFrame += (o, e) => { GameUpdate(); };
            GameService.Initialize(this);
            RegisterHotkeys();
        }

        public override void Dispose()
        {
            if (Canvas != null)
            {
                Canvas.Dispose();
                Canvas.Skin.Dispose();
                Canvas.Skin.DefaultFont.Dispose();
                Canvas.Renderer.Dispose();
                Canvas = null;
            }
            base.Dispose();
        }

        public bool ShouldXySnap()
        {
            return Settings.Local.ForceXySnap || InputUtils.Check(Hotkey.ToolXYSnap);
        }
        public void SetZoom(float val, bool changezoomslider = true)
        {
            float maxzoom = Settings.SuperZoom ? 200 : 24;
            Track.Zoom = MathHelper.Clamp(val, 0.1f, maxzoom);

            VerticalSlider vslider = (VerticalSlider)Canvas.FindChildByName("vslider", true);
            if (changezoomslider)
            {
                if (vslider != null)
                    vslider.Value = Track.Zoom;
            }
            vslider.SetToolTipText(Math.Round(Track.Zoom, 2) + "x");
            Invalidate();
        }

        public void Zoom(float percent)
        {
            if (Math.Abs(percent) < 0.00001)
                return;
            SetZoom(Track.Zoom + (Track.Zoom * percent), true);
        }
        public void Render(float blend = 1)
        {
            bool shouldrender = _invalidated ||
             Canvas.NeedsRedraw ||
            (Track.PlaybackMode) ||
            Loading ||
            Track.NeedsDraw ||
            SelectedTool.NeedsRender;
            _invalidated = false;
            if (shouldrender)
            {

                BeginOrtho();
                var slider = Canvas.Scrubber;
                if (blend == 1 && Settings.SmoothPlayback && Track.Playing && !slider.Held)
                {
                    blend = Math.Min(1, (float)Scheduler.ElapsedPercent);
                    if (ReversePlayback)
                        blend = 1 - blend;
                    Track.Camera.BeginFrame(blend, Track.Zoom);
                }
                else
                {
                    Track.Camera.BeginFrame(blend, Track.Zoom);
                }
                GL.ClearColor(Settings.NightMode
                   ? Constants.ColorNightMode
                   : (Settings.WhiteBG ? Constants.ColorWhite : Constants.ColorOffwhite));
                MSAABuffer.Use(RenderSize.Width, RenderSize.Height);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.Lighting);

#if debuggrid
                if (this.Keyboard.GetState().IsKeyDown(Key.C))
                    GameRenderer.DbgDrawGrid();
#endif
                Track.Render(blend);
#if debugcamera
                if (this.Keyboard.GetState().IsKeyDown(Key.C))
                    GameRenderer.DbgDrawCamera();
#endif
                SelectedTool.Render();
                Canvas.RenderCanvas();
                MSAABuffer.End();

                if (Settings.NightMode)
                {
                    StaticRenderer.RenderRect(new FloatRect(0, 0, RenderSize.Width, RenderSize.Height), Color.FromArgb(40, 0, 0, 0));
                }
                SwapBuffers();
                //there are machines and cases where a refresh may not hit the screen without calling glfinish...
                GL.Finish();
                var seconds = Track.FramerateWatch.Elapsed.TotalSeconds;
                Track.FramerateCounter.AddFrame(seconds);
                Track.FramerateWatch.Restart();
            }
            if (!Track.Playing &&
                !Canvas.NeedsRedraw &&
                !Track.NeedsDraw &&
                !SelectedTool.Active)//if nothing is waiting on us we can let the os breathe
                Thread.Sleep(10);
        }
        private void GameUpdateHandleInput()
        {
            if (InputUtils.HandleMouseMove(out int x, out int y) && !Canvas.IsModalOpen)
            {
                if (_handToolOverride)
                    HandTool.OnMouseMoved(new Vector2d(x, y));
                else
                    SelectedTool.OnMouseMoved(new Vector2d(x, y));
            }
            if (InputUtils.Check(Hotkey.PlaybackForward))
            {
                if (!_playbacktemp || (ReversePlayback && !InputUtils.Check(Hotkey.PlaybackBackward)))
                {
                    StopTools();
                    if (!Track.PlaybackMode)
                    {
                        Track.StartFromFlag();
                        Scheduler.DefaultSpeed();
                    }
                    if (Track.Paused)
                        Track.TogglePause();
                    Scheduler.Reset();
                    ReversePlayback = false;
                    _playbacktemp = true;
                }
            }
            else if (InputUtils.Check(Hotkey.PlaybackBackward))
            {
                if (!_playbacktemp || (!ReversePlayback && !InputUtils.Check(Hotkey.PlaybackForward)))
                {
                    StopTools();
                    if (!Track.PlaybackMode)
                    {
                        Track.StartFromFlag();
                        Scheduler.DefaultSpeed();
                    }
                    if (Track.Paused)
                        Track.TogglePause();
                    Scheduler.Reset();
                    ReversePlayback = true;
                    _playbacktemp = true;
                }
            }
            else if (_playbacktemp)
            {
                if (!Track.Paused)
                    Track.TogglePause();
                _playbacktemp = false;
                ReversePlayback = false;
                Track.UpdateCamera();
            }
        }
        public void GameUpdate()
        {
            GameUpdateHandleInput();
            var updates = Scheduler.UnqueueUpdates();
            if (updates > 0)
            {
                Invalidate();
                if (Track.Playing)
                {
                    if (InputUtils.Check(Hotkey.PlaybackZoom))
                        Zoom(0.08f);
                    else if (InputUtils.Check(Hotkey.PlaybackUnzoom))
                        Zoom(-0.08f);
                }
            }
            var qp = (!Track.PlaybackMode) ? InputUtils.Check(Hotkey.EditorQuickPan) : false;
            if (qp != _handToolOverride)
            {
                _handToolOverride = qp;
                if (_handToolOverride == false)
                {
                    HandTool.Stop();
                }
                Invalidate();
                UpdateCursor();
            }
            if (_autosavewatch.Elapsed.TotalMinutes >= 5)
            {
                _autosavewatch.Restart();
                new Thread(() => { Track.BackupTrack(false); }).Start();
            }
            if (Canvas.GetOpenWindows().Count != 0)
            {
                Invalidate();
            }


            if (Track.Playing)
            {
                if (ReversePlayback)
                {
                    Track.ActiveTriggers.Clear();//we don't want wonky unpredictable behavior
                    for (int i = 0; i < updates; i++)
                    {
                        Track.PreviousFrame();
                        Track.UpdateCamera(true);
                    }
                }
                else
                {
                    Track.Update(updates);
                }
            }
            AudioService.EnsureSync();
            if (Program.NewVersion != null)
            {
                Canvas.ShowOutOfDate();
            }
        }
        public void Invalidate()
        {
            _invalidated = true;
        }
        public void UpdateCursor()
        {
            if ((Track.PlaybackMode && !Track.Paused) || _dragRider)
                Cursor = Cursors["default"];
            else if (_handToolOverride)
                Cursor = HandTool.Cursor;
            else if (SelectedTool != null)
                Cursor = SelectedTool.Cursor;
        }
        protected override void OnLoad(EventArgs e)
        {
            Shaders.Load();
            MSAABuffer = new MsaaFbo();
            var renderer = new Gwen.Renderer.OpenTK();

            var skinpng = new Texture(renderer);
            Gwen.Renderer.OpenTK.LoadTextureInternal(
                skinpng,
                GameResources.DefaultSkin);

            var fontpng = new Texture(renderer);
            Gwen.Renderer.OpenTK.LoadTextureInternal(
                fontpng,
                GameResources.gamefont_15_png);

            var gamefont_15 = new Gwen.Renderer.BitmapFont(
                renderer,
                GameResources.gamefont_15_fnt,
                fontpng);

            var skin = new Gwen.Skin.TexturedBase(renderer,
            skinpng,
            GameResources.DefaultColors
            )
            { DefaultFont = gamefont_15 };

            Canvas = new GameCanvas(skin,
            this,
            renderer);

            _input = new Gwen.Input.OpenTK(this);
            _input.Initialize(Canvas);
            Canvas.ShouldDrawBackground = false;
            InitControls();
            Models.LoadModels();

            AddCursor("pencil", GameResources.cursor_pencil, 3, 28);
            AddCursor("line", GameResources.cursor_line, 11, 11);
            AddCursor("eraser", GameResources.cursor_eraser, 8, 8);
            AddCursor("hand", GameResources.cursor_move, 16, 16);
            AddCursor("closed_hand", GameResources.cursor_dragging, 16, 16);
            AddCursor("adjustline", GameResources.cursor_select, 4, 4);
            AddCursor("size_nesw", GameResources.cursor_size_nesw, 16, 16);
            AddCursor("size_nwse", GameResources.cursor_size_nwse, 16, 16);
            AddCursor("size_hor", GameResources.cursor_size_horz, 16, 16);
            AddCursor("size_ver", GameResources.cursor_size_vert, 16, 16);
            AddCursor("size_all", GameResources.cursor_size_all, 16, 16);
            AddCursor("default", GameResources.cursor_default, 7, 4);
            AddCursor("zoom", GameResources.cursor_zoom_in, 11, 10);
            Gwen.Platform.Neutral.CursorSetter = new UI.CursorImpl(this);
            Program.UpdateCheck();
            Track.AutoLoadPrevious();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            try
            {
                Canvas.SetSize(RenderSize.Width, RenderSize.Height);
                Canvas.FindChildByName("buttons").Position(Pos.CenterH);
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var r = _input.ProcessMouseMessage(e);
                if (Canvas.GetOpenWindows().Count != 0)
                    return;

                if (!r && !Track.Playing)
                {
                    if (!Track.Paused && OpenTK.Input.Keyboard.GetState()[Key.D])
                    {
                        var pos = new Vector2d(e.X, e.Y) / Track.Zoom;
                        var gamepos = (ScreenPosition + pos);
                        _dragRider = Game.Rider.GetBounds(
                            Track.GetStart()).Contains(
                                gamepos.X,
                                gamepos.Y);
                    }
                    if (!_dragRider)
                    {
                        if (e.Button == MouseButton.Left)
                        {
                            if (_handToolOverride)
                                HandTool.OnMouseDown(new Vector2d(e.X, e.Y));
                            else
                                SelectedTool.OnMouseDown(new Vector2d(e.X, e.Y));
                        }
                        else if (e.Button == MouseButton.Right)
                        {
                            if (_handToolOverride)
                                HandTool.OnMouseRightDown(new Vector2d(e.X, e.Y));
                            else
                                SelectedTool.OnMouseRightDown(new Vector2d(e.X, e.Y));
                        }
                        else if (e.Button == MouseButton.Middle)
                        {
                            _handToolOverride = true;
                            HandTool.OnMouseDown(new Vector2d(e.X, e.Y));
                        }
                    }
                    if (e.Button != MouseButton.Right)
                    {
                        UpdateCursor();
                    }
                }
                else
                {
                    Cursor = Cursors["default"];
                }
                Invalidate();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                _dragRider = false;
                var r = _input.ProcessMouseMessage(e);
                if (Canvas.GetOpenWindows().Count != 0)
                    return;
                if (e.Button == MouseButton.Left)
                {
                    if (_handToolOverride)
                        HandTool.OnMouseUp(new Vector2d(e.X, e.Y));
                    else
                        SelectedTool.OnMouseUp(new Vector2d(e.X, e.Y));
                }
                else if (e.Button == MouseButton.Right)
                {
                    if (_handToolOverride)
                        HandTool.OnMouseRightUp(new Vector2d(e.X, e.Y));
                    else
                        SelectedTool.OnMouseRightUp(new Vector2d(e.X, e.Y));
                }
                else if (e.Button == MouseButton.Middle)
                {
                    _handToolOverride = false;
                    HandTool.OnMouseUp(new Vector2d(e.X, e.Y));
                }
                if (r)
                    Cursor = Cursors["default"];
                else
                    UpdateCursor();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var r = _input.ProcessMouseMessage(e);
                if (Canvas.GetOpenWindows().Count != 0)
                    return;
                if (_dragRider)
                {
                    var pos = new Vector2d(e.X, e.Y);
                    var gamepos = ScreenPosition + (pos / Track.Zoom);
                    Track.Stop();
                    using (var trk = Track.CreateTrackWriter())
                    {
                        trk.Track.StartOffset = gamepos;
                        Track.Reset();
                        Track.NotifyTrackChanged();
                    }
                    Invalidate();
                }
                if (!_handToolOverride)
                {
                    if (SelectedTool.RequestsMousePrecision)
                    {
                        SelectedTool.OnMouseMoved(new Vector2d(e.X, e.Y));
                    }
                }

                if (r)
                {
                    Cursor = Cursors["default"];
                    Invalidate();
                }
                else
                    UpdateCursor();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            try
            {
                InputUtils.UpdateMouse(e.Mouse);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                if (_input.ProcessMouseMessage(e))
                    return;
                if (Canvas.GetOpenWindows().Count != 0)
                    return;
                var delta = (float.IsNaN(e.DeltaPrecise) ? e.Delta : e.DeltaPrecise);
                Zoom(delta / 5);
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            try
            {
                if (!e.IsRepeat)
                {
                    InputUtils.KeyDown(e.Key);
                }
                InputUtils.UpdateKeysDown(e.Keyboard);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                var openwindows = Canvas.GetOpenWindows();
                var mod = e.Modifiers;
                if (openwindows != null && openwindows.Count >= 1)
                {
                    if (e.Key == Key.Escape)
                    {
                        foreach (var v in openwindows)
                        {
                            ((WindowControl)v).Close();
                            Invalidate();
                        }
                        return;
                    }
                }
                if (_input.ProcessKeyDown(e) || Canvas.IsModalOpen)
                {
                    return;
                }
                if (_dragRider || OpenTK.Input.Mouse.GetState().IsButtonDown(MouseButton.Left))
                {
                    if (!_handToolOverride && (!Track.PlaybackMode || (Track.PlaybackMode && Track.Paused)) &&
                        SelectedTool != null)
                    {
                        if (SelectedTool.OnKeyDown(e.Key))
                            return;
                    }
                    else
                    {
                        return;
                    }
                }
                InputUtils.ProcessHotkeys();
                var input = e.Keyboard;
                if (!input.IsAnyKeyDown)
                    return;
                if (input.IsKeyDown(Key.AltLeft) || input.IsKeyDown(Key.AltRight))
                {
                    if (input.IsKeyDown(Key.Enter))
                    {
                        if (WindowBorder == WindowBorder.Resizable)
                        {
                            WindowBorder = WindowBorder.Hidden;
                            X = 0;
                            Y = 0;
                            var area = Screen.PrimaryScreen.Bounds;
                            RenderSize = area.Size;
                        }
                        else
                        {
                            WindowBorder = WindowBorder.Resizable;
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            try
            {
                InputUtils.UpdateKeysDown(e.Keyboard);
                if (linerider.IO.TrackRecorder.Recording)
                    return;
                if (_input.ProcessKeyUp(e) || Canvas.GetOpenWindows()?.Count > 1)
                    return;
                InputUtils.ProcessKeyup();
            }
            catch (Exception ex)
            {
                // SDL2 backend eats exceptions.
                // we have to manually crash.
                Program.Crash(ex, true);
                Close();
            }
        }

        internal void InitControls()
        {
            var ctrl = new ControlBase(Canvas);
            ctrl.Name = "buttons";
            var pos = 0;
            Func<Bitmap, Bitmap, string, string, ImageButton> createbutton =
                delegate (Bitmap bmp, Bitmap bmp2, string tooltip, string name)
                {
                    var ret = new ImageButton(ctrl) { Name = name };
                    if (tooltip != null)
                        ret.SetToolTipText(tooltip);
                    ret.SetImage(bmp, bmp2);
                    ret.SetSize(32, 32);
                    ret.X = pos;
                    pos += 32;
                    return ret;
                };
            //Warning:
            //the name parameter needs to stay consistent for these buttons
            //other parts of code reference it.
            var btn = createbutton(GameResources.pencil_icon, GameResources.pencil_icon_white, "Pencil Tool (Q)", "penciltool");
            btn.Clicked += (o, e) => { SetTool(Tools.PencilTool); };
            btn = createbutton(GameResources.line_icon, GameResources.line_icon_white, "Line Tool (W)", "linetool");
            btn.Clicked += (o, e) => { SetTool(Tools.LineTool); };
            btn = createbutton(GameResources.eraser_icon, GameResources.eraser_icon_white, "Eraser Tool (E)", "erasertool");
            btn.Clicked += (o, e) => { SetTool(Tools.EraserTool); };
            btn = createbutton(GameResources.movetool_icon, GameResources.movetool_icon_white, "Line Adjustment Tool (R)",
                "lineadjusttool");
            btn.Clicked += (o, e) => { SetTool(Tools.MoveTool); };
            //  btn = createbutton(Content.gwell_tool, Content.gwell_tool, "Gravity Well Tool (T)",
            //       "gwelltool");
            //   btn.Clicked += (o, e) => { SetTool(Tools.GwellTool); };
            btn = createbutton(GameResources.pantool_icon, GameResources.pantool_icon_white, "Hand Tool (Space) (T)", "handtool");
            btn.Clicked += (o, e) =>
            {
                SetTool(Tools.HandTool);
                _handToolOverride = false;
            };
            btn = createbutton(GameResources.play_icon, GameResources.play_icon_white, "Start (Y)", "start");
            btn.Clicked += (o, e) =>
            {
                StopTools();
                if (UI.InputUtils.Check(Hotkey.PlayButtonIgnoreFlag))
                {
                    Track.StartIgnoreFlag();
                }
                else
                {
                    Track.StartFromFlag();
                }
                Scheduler.DefaultSpeed();
            };
            pos -= 32; //occupy same space as the start button
            btn = createbutton(GameResources.pause, GameResources.pause_white, null, "pause");
            btn.IsHidden = true;
            btn.Clicked += (o, e) =>
            {
                StopTools();
                Track.TogglePause();
            };
            btn = createbutton(GameResources.stop_icon, GameResources.stop_icon_white, "Stop (U)", "stop");
            btn.Clicked += (o, e) =>
            {
                StopTools();
                Track.Stop();
            };
            btn = createbutton(GameResources.flag_icon, GameResources.flag_icon_white, "Flag (I)", "flag");
            btn.SetOverride(GameResources.flag_invalid_icon);
            btn.Clicked += (o, e) => { Track.Flag(); };
            btn.RightClicked += (o, e) => { Canvas.CalculateFlag(Track.GetFlag()); };
            Canvas.FlagTool = btn;
            btn = createbutton(GameResources.menu_icon, GameResources.menu_icon_white, "Menu", "menu");
            var _menuEdit = new Menu(Canvas);
            var item = _menuEdit.AddItem("Save");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, evt) => { Canvas.ShowSaveWindow(); };
            item = _menuEdit.AddItem("Load");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, evt) => { Canvas.ShowLoadWindow(); };
            item = _menuEdit.AddItem("New");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, evt) => { Canvas.ShowNewTrack(); };
            item = _menuEdit.AddItem("Preferences");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, msg) => { Canvas.ShowPreferences(); };
            item = _menuEdit.AddItem("Song");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, msg) => { Canvas.ShowSongWindow(); };
            item = _menuEdit.AddItem("Export SOL");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, msg) =>
            {
                Canvas.ExportAsSol();
            };
            item = _menuEdit.AddItem("Export Video");
            item.AutoSizeToContents = false;
            item.Clicked += (snd, msg) =>
            {
                if (SafeFrameBuffer.CanRecord)
                {
                    ExportVideoWindow.Create(this);
                }
                else
                {
                    PopupWindow.Error("This computer does not support recording.\nTry updating your graphics drivers.");
                }
            };
            btn.Clicked += (o, e) =>
            {
                var canvaspos = ctrl.LocalPosToCanvas(new Point(btn.X, btn.Y));
                _menuEdit.MoveTo(canvaspos.X, canvaspos.Y + 32);
                _menuEdit.Show();
            };
            _menuEdit.DeleteOnClose = false;
            _menuEdit.Close();
            var cc = new ColorControls(ctrl, new Vector2(0, 32 + 3));
            cc.Selected = LineType.Blue;
            Canvas.ColorControls = cc;
            ctrl.SizeToChildren();
            ctrl.ShouldCacheToTexture = true;
            ctrl.Position(Pos.CenterH);

            Canvas.ButtonsToggleNightmode();
        }

        public void PlaybackSpeedUp()
        {
            if (Track.PlaybackMode)
            {
                var index = Array.IndexOf(Constants.MotionArray, Scheduler.UpdatesPerSecond);
                Scheduler.UpdatesPerSecond = Constants.MotionArray[Math.Min(Constants.MotionArray.Length - 1, index + 1)];
            }
        }

        public void PlaybackSpeedDown()
        {
            if (Track.PlaybackMode)
            {
                var index = Array.IndexOf(Constants.MotionArray, Scheduler.UpdatesPerSecond);
                Scheduler.UpdatesPerSecond = Constants.MotionArray[Math.Max(0, index - 1)];
            }
        }

        public void StopTools()
        {
            if (_handToolOverride)
                HandTool.Stop();
            else
                SelectedTool?.Stop();
        }
        
        private void BeginOrtho()
        {
            if (RenderSize.Height > 0 && RenderSize.Width > 0)
            {
                GL.Viewport(new Rectangle(0, 0, RenderSize.Width, RenderSize.Height));
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, RenderSize.Width, RenderSize.Height, 0, 0, 1);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
            }
        }
        private void SetTool(Tools tool)
        {
            if (SelectedTool != null)
            {
                SelectedTool.Stop();
                SelectedTool.OnChangingTool();
            }
            if (SelectedTool == EraserTool && tool != Tools.EraserTool)
            {
                Canvas.ColorControls.SetEraser(false);
            }
            if (tool == Tools.HandTool)
            {
                SelectedTool = HandTool;
                HandTool.Stop();
                _handToolOverride = false;
                Canvas.ColorControls.SetVisible(false);
            }
            else if (tool == Tools.LineTool)
            {
                SelectedTool = LineTool;
                Canvas.ColorControls.SetVisible(true);
                if (Canvas.ColorControls.Selected == LineType.All)
                {
                    Canvas.ColorControls.Selected = LineType.Blue;
                }
            }
            else if (tool == Tools.PencilTool)
            {
                SelectedTool = PencilTool;
                Canvas.ColorControls.SetVisible(true);
                if (Canvas.ColorControls.Selected == LineType.All)
                {
                    Canvas.ColorControls.Selected = LineType.Blue;
                }
            }
            else if (tool == Tools.EraserTool)
            {
                Canvas.ColorControls.SetVisible(true);
                Canvas.ColorControls.SetEraser(true);
                if (SelectedTool == EraserTool)
                    Canvas.ColorControls.Selected = LineType.All;
                SelectedTool = EraserTool;
            }
            else if (tool == Tools.MoveTool)
            {
                SelectedTool = MoveTool;
                Canvas.ColorControls.SetVisible(false);
            }
            Invalidate();
            UpdateCursor();
        }

        private void AddCursor(string name, Bitmap image, int hotx, int hoty)
        {
            var data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppPArgb);
            Cursors[name] = new MouseCursor(hotx, hoty, image.Width, image.Height, data.Scan0);
        }
        private void RegisterHotkeys()
        {
            RegisterPlaybackHotkeys();
            RegisterEditorHotkeys();
            RegisterSettingHotkeys();
            RegisterPopupHotkeys();
        }
        private void RegisterSettingHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.PreferenceOnionSkinning, () => true, () =>
            {
                Settings.Local.OnionSkinning = !Settings.Local.OnionSkinning;
                Track.Invalidate();
            });
        }
        private void RegisterPlaybackHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.PlaybackStartSlowmo, () => true, () =>
            {
                StopTools();
                Track.StartFromFlag();
                Scheduler.UpdatesPerSecond = Settings.Local.SlowmoSpeed;
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStartIgnoreFlag, () => true, () =>
            {
                StopTools();
                Track.StartIgnoreFlag();
                Scheduler.DefaultSpeed();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStartGhostFlag, () => true, () =>
            {
                StopTools();
                Track.ResumeFromFlag();
                Scheduler.DefaultSpeed();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStart, () => true, () =>
            {
                StopTools();
                Track.StartFromFlag();
                Scheduler.DefaultSpeed();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackStop, () => true, () =>
            {
                StopTools();
                Track.Stop();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackFlag, () => true, () =>
            {
                Track.Flag();
            });

            InputUtils.RegisterHotkey(Hotkey.PlaybackFrameNext, () => true, () =>
            {
                StopTools();
                if (!Track.PlaybackMode)
                {
                    Track.StartFromFlag();
                    Scheduler.DefaultSpeed();
                }
                if (!Track.Paused)
                    Track.TogglePause();
                Track.NextFrame();
                Invalidate();
                Track.UpdateCamera();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackFramePrev, () => true, () =>
            {
                StopTools();
                if (!Track.PlaybackMode)
                {
                    Track.StartFromFlag();
                    Scheduler.DefaultSpeed();
                }
                if (!Track.Paused)
                    Track.TogglePause();
                Track.PreviousFrame();
                Invalidate();
                Track.UpdateCamera(true);
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackSpeedUp, () => Track.PlaybackMode, () =>
            {
                PlaybackSpeedUp();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackSpeedDown, () => Track.PlaybackMode, () =>
            {
                PlaybackSpeedDown();
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackSlowmo, () => Track.PlaybackMode, () =>
            {
                if (Scheduler.UpdatesPerSecond !=
                Settings.Local.SlowmoSpeed)
                {
                    Scheduler.UpdatesPerSecond = Settings.Local.SlowmoSpeed;
                }
                else
                {
                    Scheduler.DefaultSpeed();
                }
            });
            InputUtils.RegisterHotkey(Hotkey.PlaybackTogglePause, () => Track.PlaybackMode, () =>
            {
                StopTools();
                Track.TogglePause();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackIterationNext, () => !Track.Playing, () =>
            {
                StopTools();
                if (!Track.PlaybackMode)
                {
                    Track.StartFromFlag();
                    Scheduler.DefaultSpeed();
                }
                if (!Track.Paused)
                    Track.TogglePause();
                if (Track.IterationsOffset != 6)
                {
                    Track.IterationsOffset++;
                }
                else
                {
                    Track.NextFrame();
                    Track.IterationsOffset = 0;
                    Track.Camera.SetFrameCenter(Track.RenderRider.CalculateCenter());
                }
                Track.InvalidateRenderRider();
                Canvas.UpdateIterationUI();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.PlaybackIterationPrev, () => !Track.Playing, () =>
            {
                if (Track.Offset != 0)
                {
                    StopTools();
                    if (Track.IterationsOffset > 0)
                    {
                        Track.IterationsOffset--;
                    }
                    else
                    {
                        Track.PreviousFrame();
                        Track.IterationsOffset = 6;
                        Invalidate();
                        Track.Camera.SetFrameCenter(Track.RenderRider.CalculateCenter());
                    }
                    Track.InvalidateRenderRider();
                    Canvas.UpdateIterationUI();
                }
            },
            null,
            repeat: true);
        }
        private void RegisterPopupHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.LoadWindow, () => true, () =>
            {
                StopTools();
                Canvas.ShowLoadWindow();
            });

            InputUtils.RegisterHotkey(Hotkey.PreferencesWindow, () => true, () =>
            {
                StopTools();
                Canvas.ShowPreferences();
            });
            InputUtils.RegisterHotkey(Hotkey.Quicksave, () => true, () =>
               {
                   Track.QuickSave();
               });
        }
        private void RegisterEditorHotkeys()
        {
            InputUtils.RegisterHotkey(Hotkey.EditorPencilTool, () => !Track.Playing, () =>
            {
                SetTool(Tools.PencilTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorLineTool, () => !Track.Playing, () =>
            {
                SetTool(Tools.LineTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorEraserTool, () => !Track.Playing, () =>
            {
                SetTool(Tools.EraserTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorSelectTool, () => !Track.Playing, () =>
            {
                SetTool(Tools.MoveTool);
            });
            InputUtils.RegisterHotkey(Hotkey.EditorPanTool, () => !Track.Playing, () =>
            {
                //bugfix: pushing t wuold cancel panning and youd have to click again
                if (SelectedTool != HandTool)
                {
                    SetTool(Tools.HandTool);
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorUndo, () => !Track.Playing, () =>
            {
                StopTools();
                Track.UndoManager.Undo();
                Invalidate();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorRedo, () => !Track.Playing, () =>
            {
                StopTools();
                Track.UndoManager.Redo();
                Invalidate();
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorRemoveLatestLine, () => !Track.Playing, () =>
            {
                if (!Track.PlaybackMode || Track.Paused)
                {
                    StopTools();
                    using (var trk = Track.CreateTrackWriter())
                    {
                        SelectedTool?.Stop();
                        var l = trk.GetNewestLine();
                        if (l != null)
                        {
                            Track.UndoManager.BeginAction();
                            trk.RemoveLine(l);
                            Track.UndoManager.EndAction();
                        }

                        Track.NotifyTrackChanged();
                        Invalidate();
                    }
                }
            },
            null,
            repeat: true);
            InputUtils.RegisterHotkey(Hotkey.EditorFocusStart, () => !Track.Playing, () =>
            {
                using (var trk = Track.CreateTrackReader())
                {
                    var l = trk.GetOldestLine();
                    if (l != null)
                    {
                        Track.Camera.SetFrameCenter(l.Position);
                        Invalidate();
                    }
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusLastLine, () => !Track.Playing, () =>
            {
                using (var trk = Track.CreateTrackReader())
                {
                    var l = trk.GetNewestLine();
                    if (l != null)
                    {
                        Track.Camera.SetFrameCenter(l.Position);
                        Invalidate();
                    }
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorCycleToolSetting, () => !Track.Playing, () =>
            {
                Canvas.ColorControls.OnTabButtonPressed();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor1, () => !Track.Playing, () =>
            {
                Canvas.ColorControls.Selected = LineType.Blue;
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor2, () => !Track.Playing, () =>
            {
                Canvas.ColorControls.Selected = LineType.Red;
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorToolColor3, () => !Track.Playing, () =>
            {
                Canvas.ColorControls.Selected = LineType.Scenery;
                Invalidate();
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusFlag, () => !Track.Playing, () =>
            {
                var flag = Track.GetFlag();
                if (flag != null)
                {
                    Track.Camera.SetFrameCenter(flag.State.CalculateCenter());
                    Invalidate();
                }
            });
            InputUtils.RegisterHotkey(Hotkey.EditorFocusRider, () => !Track.Playing, () =>
            {
                Track.Camera.SetFrameCenter(Track.RenderRider.CalculateCenter());
                Invalidate();
            });
        }
    }
}