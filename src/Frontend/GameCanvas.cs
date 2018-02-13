//
//  GameCanvas.cs
//
//  Author:
//       Noah Ablaseau <nablaseauhotmail.com>
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

using System.Threading;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using OpenTK;
using linerider.UI;
using linerider.Tools;
using linerider.Audio;
using Gwen.Skin;
using Gwen.Controls;
using Gwen;
using Color = System.Drawing.Color;

namespace linerider
{
    public class GameCanvas : Canvas
    {
        public Sprite SpriteLoading;
        public Gwen.Renderer.OpenTK Renderer;
        public ColorControls ColorControls;
        public ImageButton FlagTool;
        private MainWindow game;
        private bool _draggingSlider = false;
        int _lastfpsupdate = 0;

        public bool IsModalOpen
        {
            get { return Children.FirstOrDefault(x => x is Gwen.ControlInternal.Modal) != null; }
        }

        public GameCanvas(SkinBase skin, MainWindow Game, Gwen.Renderer.OpenTK renderer) : base(skin)
        {
            game = Game;
            this.Renderer = renderer;
            BoundsChanged += GameCanvas_BoundsChanged;
            SpriteLoading = new Sprite(this);
            SpriteLoading.SetImage(GameResources.loading);
            SpriteLoading.IsHidden = true;
            SpriteLoading.IsTabable = false;
            SpriteLoading.KeyboardInputEnabled = false;
            SpriteLoading.MouseInputEnabled = false;
            SpriteLoading.SetSize(32, 32);
            SpriteLoading.RotationPoint.X = 16;
            SpriteLoading.RotationPoint.Y = 16;
            SpriteLoading.SetPosition(Width - 32, 0);

            var timeslider = new HorizontalIntSlider(this)
            {
                X = 120,
                Y = Height - 25,
                Width = Width - 120 * 2,
                Height = 25,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "timeslider",
            };
            var btnfastfoward = new ImageButton(this)
            {
                X = timeslider.Right,
                Y = Height - 36,
                Width = 32,
                Height = 32,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "btnfastforward"
            };
            btnfastfoward.SetImage(GameResources.fast_forward, GameResources.fast_forward_white);
            var btnslowmo = new ImageButton(this)
            {
                X = timeslider.X - 24,
                Y = Height - 36,
                Width = 32,
                Height = 32,
                IsTabable = false,
                IsHidden = true,
                KeyboardInputEnabled = false,
                Name = "btnslowmo"
            };
            btnslowmo.SetImage(GameResources.rewind, GameResources.rewind_white);
            btnslowmo.Clicked += (o, e) => { game.PlaybackDown(); };
            btnfastfoward.Clicked += (o, e) => { game.PlaybackUp(); };

            timeslider.ValueChanged += timeslider_ValueChanged;
            var labelTrackName = new Label(this)
            {
                TextColor = System.Drawing.Color.Black,
                Dock = Pos.Left,
                Margin = new Margin(5, 0, 0, 0),
                Name = "trackname"
            };
            Label labeliteration = new Label(this);
            labeliteration.Name = "labeliterations";
            labeliteration.SetText("");
            labeliteration.TextColor = Color.Black;
            Align.PlaceDownLeft(labeliteration, labelTrackName);

            var toprightcontainer = new ControlBase(this)
            {
                Dock = Pos.Right,
                IsTabable = false,
                Width = 150,
                Height = 300,
                MouseInputEnabled = false
            };
            new Label(toprightcontainer) { TextColor = Color.Black, Name = "fps" };
            var labelppf = new Label(toprightcontainer) { TextColor = Color.Black, Name = "ppf" };
            var labelPlayback = new Label(toprightcontainer) { TextColor = Color.Black, Name = "labelplayback" };
            new Label(this) { TextColor = Color.Black, Name = "flagtime" };
            var textheight = renderer.MeasureText(skin.DefaultFont, "").Y + 3;
            labelppf.SetPosition(0, textheight);
            labelPlayback.SetPosition(0, textheight * 2);

            var vslider = new VerticalSlider(this)
            {
                Min = 0.1f,
                Max = 24f,
                Value = game.Track.Zoom,
                IsTabable = false,
                KeyboardInputEnabled = false
            };
            vslider.ValueChanged += (o, e) =>
            {
                var slider = (VerticalSlider)o;
                var diff = slider.Value - game.Track.Zoom;
                game.Zoom(diff, false);
            };
            vslider.SetPosition(0, (int)Height - 150);
            vslider.Name = "vslider";
            Align.AlignRight(vslider);
            vslider.Height = 125;
        }

        protected override void Render(SkinBase skin)
        {
            SpriteLoading.IsHidden = !game.Loading;
            if (game.Loading)
                SpriteLoading.Rotation = (Environment.TickCount % 1000) / 1000f;
            var trackname = (Label)FindChildByName("trackname");
            trackname.Text = game.Track.Name;
            var sppf = "";
            var playback = "";
            var fpsorlinecount = "";
            var fpslabel = ((Label)FindChildByName("fps", true));
            var ppflabel = ((Label)FindChildByName("ppf", true));
            var labelplayback = ((Label)FindChildByName("labelplayback", true));
            if (game.Track.PlaybackMode)
            {
                var currts = TimeSpan.FromSeconds(game.Track.CurrentFrame / 40f);
                var ppf = game.Track.RenderRider.CalculateMomentum().Length;
                if (Math.Abs(Environment.TickCount - _lastfpsupdate) > 500)
                {
                    _lastfpsupdate = Environment.TickCount;
                    fpsorlinecount = Settings.Local.RecordingMode ? "40 FPS" : Math.Round(game.Track.FpsCounter.FPS) + " FPS";
                }
                else
                {
                    fpsorlinecount = fpslabel.Text;
                }
                sppf = string.Format("{0:N2}", Math.Round(ppf, 2)) + " ppf";
                playback = currts.ToString("mm\\:ss") + ":"
                + (game.Track.CurrentFrame % 40f) + " " + Math.Round(game.Scheduler.UpdatesPerSecond / 40f, 3) + "x";
                if (Settings.Local.RecordingMode)
                {
                    if (Settings.Local.ShowFps)
                        fpsorlinecount = Settings.Local.SmoothRecording ? "60 FPS" : "40 FPS";
                    else
                        fpsorlinecount = "";
                    if (!Settings.Local.ShowPpf)
                        sppf = "";
                    if (!Settings.Local.ShowTimer)
                        playback = "";
                }
            }
            else
            {
                _lastfpsupdate = 0;
                fpsorlinecount = "Lines: " + game.Track.LineCount;
            }
            fpslabel.Text = fpsorlinecount;
            ppflabel.Text = sppf;
            labelplayback.Text = playback;
            var par = fpslabel.Parent;
            fpslabel.X = par.Width - fpslabel.Width;
            ppflabel.X = par.Width - ppflabel.Width;
            labelplayback.X = par.Width - labelplayback.Width;
            var flag = game.Track.GetFlag();
            var labelflagtime = (Label)FindChildByName("flagtime");
            if (flag != null)
            {
                var cam = flag.State.CalculateCenter();
                cam.X -= 15;
                cam.Y -= 15;
                var ts = TimeSpan.FromSeconds((flag.Frame) / 40f);
                labelflagtime.IsHidden = false;
                labelflagtime.Text = ts.ToString("mm\\:ss") + ":" + (flag.Frame % 40f);
                labelflagtime.SetPosition((float)(cam.X + game.ScreenTranslation.X) * game.Track.Zoom,
                    (float)(cam.Y + game.ScreenTranslation.Y) * game.Track.Zoom);
            }
            else
            {
                labelflagtime.IsHidden = true;
            }
            base.Render(skin);
        }

        public void UpdateIterationUI()
        {
            var l = (Label)FindChildByName("labeliterations");
            l.IsHidden = !(game.Track.PlaybackMode && game.Track.Paused);
            if (!l.IsHidden)
            {
                if (game.Track.IterationsOffset == 6)
                    l.SetText("");
                else if (game.Track.IterationsOffset == 0)
                    l.SetText("Physics Iteration: " + game.Track.IterationsOffset + " (momentum tick)");
                else
                    l.SetText("Physics Iteration: " + game.Track.IterationsOffset);
            }
        }
        public void ShowPlaybackUI()
        {
            var buttons = FindChildByName("buttons");
            var slider = (HorizontalIntSlider)FindChildByName("timeslider");
            FindChildByName("btnfastforward").IsHidden = Settings.Local.RecordingMode;
            FindChildByName("btnslowmo").IsHidden = Settings.Local.RecordingMode;
            if (Settings.Local.RecordingMode)
            {
                buttons.FindChildByName("pause").IsHidden = true;
                buttons.FindChildByName("start").IsHidden = false;
                FindChildByName("trackname", true).IsHidden = true;
                slider.IsHidden = true;
                buttons.IsHidden = !Settings.Local.RecordingShowTools;
                FindChildByName("fps", true).IsHidden = !Settings.Local.ShowFps;
                FindChildByName("ppf", true).IsHidden = !Settings.Local.ShowPpf;
                FindChildByName("labelplayback", true).IsHidden = !Settings.Local.ShowTimer;
            }
            else
            {
                buttons.FindChildByName("pause").IsHidden = false;
                buttons.FindChildByName("start").IsHidden = true;
                FindChildByName("trackname", true).IsHidden = false;
                slider.IsHidden = false;
                buttons.IsHidden = false;
            }
            FindChildByName("labeliterations").IsHidden = true;
            FindChildByName("vslider", true).IsHidden = true;
        }
        public void HidePlaybackUI()
        {
            var buttons = FindChildByName("buttons");
            buttons.FindChildByName("pause").IsHidden = true;
            buttons.FindChildByName("start").IsHidden = false;
            var slider = FindChildByName("timeslider");
            slider.IsHidden = true;
            FindChildByName("labeliterations").IsHidden = true;
            FindChildByName("vslider", true).IsHidden = false;
            FindChildByName("btnfastforward").IsHidden = true;
            FindChildByName("btnslowmo").IsHidden = true;

            //incase recording mode was enabled at the start. There's a risk it was disabled during playback
            //if that's the case checking game.RecordingMode would fail but controls would remain invisible.
            //instead, we just by default ensure visibility
            {
                buttons.IsHidden = false;
                FindChildByName("fps", true).IsHidden = false;
                FindChildByName("ppf", true).IsHidden = false;
                FindChildByName("labelplayback", true).IsHidden = false;
                FindChildByName("trackname", true).IsHidden = false;
            }
        }
        public void UpdatePauseUI()
        {
            var container = game.Canvas.FindChildByName("buttons");
            var start = container.FindChildByName("start");
            var pause = container.FindChildByName("pause");
            pause.IsHidden = game.Track.Paused;
            start.IsHidden = !game.Track.Paused;
        }
        public void UpdateScrubber()
        {

            var slider = (HorizontalIntSlider)FindChildByName("timeslider");
            slider.Min = 0;
            slider.Max = game.Track.EndFrameID;
            slider.Value = game.Track.Offset;
        }
        public void ExportAsSol()
        {
            using (var trk = game.Track.CreateTrackReader())
            {
                if (trk.GetOldestLine() != null)
                {
                    var features = trk.GetFeatures();
                    bool six_one;
                    bool redmultiplier;
                    bool scenerywidth;
                    features.TryGetValue("SIX_ONE", out six_one);
                    features.TryGetValue("REDMULTIPLIER", out redmultiplier);
                    features.TryGetValue("SCENERYWIDTH", out scenerywidth);
                    if (six_one || redmultiplier || scenerywidth)
                    {
                        var window = PopupWindow.Error("Unable to export SOL file due to it containing special LRA specific features.\n" + (six_one ? "\nthe track is based on 6.1, " : "") + (redmultiplier ? "\nthe track uses red multiplier lines, " : "") + (scenerywidth ? "\nthe track uses varying scenery line width " : "") + "\n\nand therefore cannot be loaded");
                    }
                    else
                    {
                        var window = PopupWindow.Create("Are you sure you wish to save this track as an SOL file? It will overwrite any file with its name. (trackname+savedLines.sol)", "Are you sure?", true, true);
                        window.Dismissed += (o, e) =>
                        {
                            if (window.Result == System.Windows.Forms.DialogResult.OK)
                            {
                                trk.SaveTrackAsSol();
                            }
                        };
                    }
                }
            }
        }
        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (Configuration.RunningOnWindows)
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (Configuration.RunningOnMacOS)
                {
                    Process.Start("open", url);
                }
                else if (Configuration.RunningOnLinux)
                {
                    Process.Start("xdg-open", url);
                }
                else
                {
                    throw;
                }
            }
        }
        public void ShowOutOfDate()
        {
            if (Program.NewVersion == null)
                return;
            var window = PopupWindow.Create("Would you like to download the latest version?", "Update Available! v" + Program.NewVersion, true, true);
            window.Dismissed += (o, e) =>
            {
                if (window.Result == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        OpenUrl(@"https://github.com/jealouscloud/linerider-advanced/releases/latest");
                    }
                    catch
                    {
                        PopupWindow.Error("Unable to open the browser.");
                    }
                }
            };
            Program.NewVersion = null;
        }
        public void ButtonsToggleNightmode()
        {
            var nightmode = Settings.NightMode;
            var buttons = FindChildByName("buttons");
            foreach (var v in buttons.Children)
            {
                if (v is ImageButton)
                {
                    var tool = (ImageButton)v;
                    tool.Nightmode(nightmode);
                }
            }
            foreach (var v in Children)
            {
                if (v is ImageButton)
                {
                    var tool = (ImageButton)v;
                    tool.Nightmode(nightmode);
                }
            }
        }
        public void ShowLoadWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            var loadwindow = new UI.LoadWindow(this, game);
            ShowCenteredWindow(loadwindow);
        }
        public void ShowSaveWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            UI.SaveWindow sw = new UI.SaveWindow(this, game);
            ShowCenteredWindow(sw);
        }
        public void ShowSongWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            game.Track.Stop();
            var songs = new UI.SongWindow(this, game);
            ShowCenteredWindow(songs);
        }
        public void ShowDelete()
        {
            var window = PopupWindow.Create("Do you want to delete the current track?", "Delete Track", true, true);
            window.Dismissed += (o, e) =>
            {
                if (window.Result == System.Windows.Forms.DialogResult.OK)
                {
                    game.Track.Stop();
                    game.Track.ChangeTrack(new Track() { Name = "untitled" });
                    game.Invalidate();
                }
            };
        }
        public void ShowPreferences()
        {
            var trk = game.Track;
            if (trk.PlaybackMode && !trk.Paused)
            {
                trk.TogglePause();
            }
            if (GetOpenWindows().Count != 0)
                return;
            ShowCenteredWindow(new UI.PreferencesWindow(this, game));
        }
        private void ShowCenteredWindow(WindowControl win)
        {
            win.Show();
            win.SetPosition((Width / 2) - (win.Width / 2), (Height / 2) - (win.Height / 2));

            game.Cursor = game.Cursors["default"];
        }
        public List<ControlBase> GetOpenWindows()
        {
            List<ControlBase> ret = new List<ControlBase>();
            foreach (var child in Children)
            {
                if (child is WindowControl)
                {
                    ret.Add(child);
                }
                else if (child is Gwen.ControlInternal.Modal)
                {
                    ret.Add(child.Children.Single(x => x is WindowControl));
                }
            }
            return ret;
        }
        public void SetTooltip(ControlBase control, string str)
        {
            if (control == null)
                control = this;
            control.SetToolTipText(str);
            ToolTip.Enable(control);
        }
        public void RemoveTooltip(ControlBase control)
        {
            if (control == null)
                control = this;
            ToolTip.Disable(control);
            control.Tooltip = null;
        }

        internal void DisableFlagTooltip()
        {
            RemoveTooltip(FlagTool);
        }

        internal void CalculateFlag(TrackService.Tracklocation loc)
        {
            //todo flagtooltip != null?
            if (loc?.State == null || FlagTool.Tooltip != null) return;
            var invalid = false;

            var state = game.Track.GetStart();
            var frame = loc.Frame;
            using (var trk = game.Track.CreateTrackReader())
            {
                if (frame > 400) //many frames, will likely lag the game. Update the window as a fallback.
                {
                    if (frame > 24000) //too many frames, could lag the game very bad.
                    {
                        SetTooltip(FlagTool, "Flag is incalculable.");
                        game.Invalidate();
                        return;
                    }
                    game.Title = Program.WindowTitle + " [Validating flag]";
                }

                for (var i = 0; i < frame; i++) //tick the exact number of frames that the flag should be on.
                {
                    state = trk.TickBasic(state);
                }
            }
            for (var i = 0; i < state.Body.Length; i++)
            {
                if (state.Body[i] != loc.State.Body[i])
                {
                    invalid = true;
                    break;
                }
            }
            SetFlagTooltip(!invalid);
            SetTooltip(FlagTool, invalid ? "Flag is invalid" : "Flag is valid");
            if (frame > 400)
            {
                game.Title = Program.WindowTitle;
            }
            game.Invalidate();
        }

        internal void SetFlagTooltip(bool valid)
        {
            SetTooltip(FlagTool, valid ? "Flag is valid" : "Flag is invalid");
        }

        private void GameCanvas_BoundsChanged(ControlBase sender, EventArgs arguments)
        {
            SpriteLoading.SetPosition(Width - 32, 0);
            var slider = (HorizontalIntSlider)FindChildByName("timeslider");
            slider.X = 120;
            slider.Width = Width - 120 * 2;
            slider.Y = Height - 32;
            var vslider = FindChildByName("vslider", true);
            vslider.X = Width - vslider.Width;
            vslider.Y = Height - vslider.Height - 25;
            FindChildByName("btnslowmo").SetPosition(slider.X - 32, slider.Y - 4);
            FindChildByName("btnfastforward").SetPosition(slider.Right, slider.Y - 4);
        }

        private void timeslider_ValueChanged(ControlBase sender, EventArgs arguments)
        {
            var slider = (HorizontalIntSlider)sender;
            if (slider.Held || _draggingSlider)
            {
                game.Track.SetFrame(slider.Value, false);
                if (!game.Track.Playing)
                {
                    game.Track.Camera.SetFrame(game.Track.RenderRider.CalculateCenter(), false);
                }
            }
            if (slider.Held)
            {
                _draggingSlider = true;
                if (Settings.Local.EnableSong)
                {
                    AudioService.Pause();
                }
            }
            else if (_draggingSlider)
            {
                _draggingSlider = false;
                game.Scheduler.Reset();
                if (Settings.Local.EnableSong)
                {
                    game.UpdateSongPosition(game.Track.CurrentFrame / 40f);
                }
            }
        }

        public override void Dispose()
        {
            var iterations = FindChildByName("labeliterations") as Label;
            iterations?.Font.Dispose();
            base.Dispose();
        }
    }
}