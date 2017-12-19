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

using System;
using System.Collections.Generic;
using System.Linq;
using Gwen;
using Gwen.Controls;
using Gwen.Skin;
using linerider.Tools;
using System.IO;
using System.Threading;
using linerider.Audio;
using OpenTK;
using Color = System.Drawing.Color;

namespace linerider
{
    public class GameCanvas : Canvas
    {
        public Sprite SpriteLoading;
        public Gwen.Renderer.OpenTK Renderer;
        public ColorControls ColorControls;
        public ImageButton FlagTool;
        private GLWindow game;
        private bool _draggingSlider = false;
        int _lastfpsupdate = 0;

        public bool IsModalOpen
        {
            get { return Children.FirstOrDefault(x => x is Gwen.ControlInternal.Modal) != null; }
        }

        public GameCanvas(SkinBase skin, GLWindow Game, Gwen.Renderer.OpenTK renderer) : base(skin)
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
            labeliteration.Font = new Font(renderer, "Arial", 18);
            labeliteration.SetText("");
            labeliteration.TextColor = Color.Black;
            Align.PlaceDownLeft(labeliteration, labelTrackName);

            var toprightcontainer = new ControlBase(this)
            {
                Dock = Pos.Right,
                Width = 150,
                Height = 300,
                MouseInputEnabled = false
            };
            var fps = new Label(toprightcontainer) { TextColor = Color.Black, Name = "fps" };
            var labelppf = new Label(toprightcontainer) { TextColor = Color.Black, Name = "ppf" };
            var labelPlayback = new Label(toprightcontainer) { TextColor = Color.Black, Name = "labelplayback" };
            var flagtime = new Label(this) { TextColor = Color.Black, Name = "flagtime" };
            var textheight = renderer.MeasureText(skin.DefaultFont, "").Y + 3;
            labelppf.SetPosition(0, textheight);
            labelPlayback.SetPosition(0, textheight * 2);

            var vslider = new VerticalSlider(this) { Min = 0.1f, Max = 24f, Value = game.Track.Zoom, IsTabable = false };
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
            if (game.Track.Animating)
            {
                if (Math.Abs(Environment.TickCount - _lastfpsupdate) > 500)
                {
                    _lastfpsupdate = Environment.TickCount;
                    fpsorlinecount = game.SettingRecordingMode ? "40 FPS" : Math.Round(game.Track.FpsCounter.FPS) + " FPS";
                    if (!game.SettingShowFps && game.SettingRecordingMode)
                    {
                        fpsorlinecount = "";
                    }
                }
                else
                {
                    fpsorlinecount = game.SettingRecordingMode ? "40 FPS" : fpslabel.Text;
                }
                var ppf = game.Track.RiderState.CalculateMomentum().Length;
                var pixels = Math.Round(ppf, 2);
                sppf = string.Format("{0:N2}", pixels) + " ppf";
                if (!game.SettingShowPpf && game.SettingRecordingMode)
                {
                    sppf = "";
                }
                var currts = TimeSpan.FromSeconds(game.Track.CurrentFrame / 40f);
                playback = currts.ToString("mm\\:ss") + ":" + (game.Track.CurrentFrame % 40f) + " " +
                    Math.Round(game.Scheduler.UpdatesPerSecond / 40f, 3) + "x";
                if (!game.SettingShowTimer && game.SettingRecordingMode)
                {
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
            var loadwindow = new Windows.LoadWindow(this, game);
            ShowCenteredWindow(loadwindow);
        }
        public void UpdateSOLFiles()
        {
            game.Loading = true;
            linerider.Windows.LoadWindow.UpdateSOLs();
            game.Loading = false;
        }
        public void ShowSaveWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            Windows.SaveWindow sw = new Windows.SaveWindow(this, game);
            ShowCenteredWindow(sw);
        }
        public void ShowSongWindow()
        {
            if (GetOpenWindows().Count != 0)
                return;
            game.Track.Stop();
            var songs = new Windows.SongWindow(this, game);
            ShowCenteredWindow(songs);
        }
        public void ShowDelete()
        {
            var window = PopupWindow.Create(this, game, "Do you want to delete the current track?", "Delete Track", true, true);
            window.FindChildByName("Okay", true).Clicked += (o, e) =>
            {
                game.Track.Stop();
                game.Track.ChangeTrack(new Track() { Name = "untitled" });
                game.Invalidate();
                window.Close();
            };
            window.FindChildByName("Cancel", true).Clicked += (o, e) => { window.Close(); };
        }
        public void ShowPreferences()
        {
            var trk = game.Track;
            if (trk.Animating && !trk.Paused)
            {
                trk.TogglePause();
            }
            if (GetOpenWindows().Count != 0)
                return;
            ShowCenteredWindow(new Windows.PreferencesWindow(this, game));
        }
        private void ShowCenteredWindow(WindowControl win)
        {
            win.Show();
            win.SetPosition((Width / 2) - (win.Width / 2), (Height / 2) - (win.Height / 2));

            game.Cursor = MouseCursor.Default;
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

        internal void CalculateFlag(GLTrack.Tracklocation loc)
        {
            if (loc?.State == null || FlagTool.Tooltip != null) return;
            var invalid = false;
            var state = new Rider();
            game.Track.Reset(state);
            var frame = loc.Frame;
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
                game.Track.Tick(state);
            }
            for (var i = 0; i < state.ModelAnchors.Count(); i++)
            {
                if (state.ModelAnchors[i].Position != loc.State.ModelAnchors[i].Position ||
                    state.ModelAnchors[i].Prev != loc.State.ModelAnchors[i].Prev)
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
                game.Track.EnterPlayback();
                game.Track.SetFrame(slider.Value, false);
                game.Track.ExitPlayback();
                if (!game.Track.Playing)
                {
                    game.Track.Camera.SetFrame(game.Track.RiderState.CalculateCenter(), false);
                }
            }
            if (slider.Held)
            {
                _draggingSlider = true;
                if (game.EnableSong)
                {
                    AudioService.Pause();
                }
            }
            else if (_draggingSlider)
            {
                _draggingSlider = false;
                game.Scheduler.Reset();
                if (game.EnableSong)
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