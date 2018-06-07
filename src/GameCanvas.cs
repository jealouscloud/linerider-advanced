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
        public static readonly Queue<Action> QueuedActions = new Queue<Action>();
        public ZoomSlider ZoomSlider;
        public Gwen.Renderer.OpenTK Renderer;
        private RightInfoBar _infobar;
        private TrackInfoBar _trackinfobar;
        private ControlBase _topcontainer;
        private TimelineWidget _timeline;
        private Toolbar _toolbar;
        private LoadingSprite _loadingsprite;
        private MainWindow game;
        public PlatformImpl Platform;
        public bool Loading { get; set; }
        public Color TextForeground
        {
            get
            {
                return Settings.NightMode ? Skin.Colors.Text.Highlight : Skin.Colors.Text.Foreground;
            }
        }
        public bool IsModalOpen
        {
            get { return Children.FirstOrDefault(x => x is Gwen.ControlInternal.Modal) != null; }
        }
        private Tooltip _usertooltip;
        public bool Scrubbing => _timeline.Playhead.Held;
        public readonly Fonts Fonts;

        public GameCanvas(
            SkinBase skin,
            MainWindow Game,
            Gwen.Renderer.OpenTK renderer,
            Fonts fonts) : base(skin)
        {
            game = Game;
            Fonts = fonts;
            this.Renderer = renderer;
            Platform = new PlatformImpl(Game);
            Gwen.Platform.Neutral.Implementation = Platform;
            CreateUI();
            OnThink += Think;
        }
        private void Think(object sender, EventArgs e)
        {
            //process recording junk
            var rec = IO.TrackRecorder.Recording;
            ZoomSlider.IsHidden = rec;
            _toolbar.IsHidden = rec && !Settings.Recording.ShowTools;
            _timeline.IsHidden = rec;
            //
            _loadingsprite.IsHidden = rec || !Loading;
            var selectedtool = CurrentTools.SelectedTool;
            _usertooltip.IsHidden = !(selectedtool.Active && selectedtool.Tooltip != "");
            if (!_usertooltip.IsHidden)
            {
                if (_usertooltip.Text != selectedtool.Tooltip)
                {
                    _usertooltip.Text = selectedtool.Tooltip;
                    _usertooltip.Layout();
                }
                var mousePos = Gwen.Input.InputHandler.MousePosition;
                var bounds = _usertooltip.Bounds;
                var offset = Util.FloatRect(
                    mousePos.X - bounds.Width * 0.5f,
                    mousePos.Y - bounds.Height - 10,
                    bounds.Width,
                    bounds.Height);
                offset = Util.ClampRectToRect(offset, Bounds);
                _usertooltip.SetPosition(offset.X, offset.Y);
            }
        }
        private void CreateUI()
        {
            _usertooltip = new Tooltip(this) { IsHidden = true };
            _loadingsprite = new LoadingSprite(this)
            {
                Positioner = (o) =>
                {
                    return new System.Drawing.Point(
                        Width - _loadingsprite.Width,
                        0);
                },
            };
            _loadingsprite.SetImage(GameResources.loading);
            _toolbar = new Toolbar(this, game.Track) { Y = 0 };
            ZoomSlider = new ZoomSlider(this, game.Track);
            _timeline = new TimelineWidget(this, game.Track);
            _topcontainer = new Panel(this)
            {
                Height = 100,
                Dock = Dock.Top,
                ShouldDrawBackground = false,
                MouseInputEnabled = false,
            };
            _infobar = new RightInfoBar(_topcontainer, game.Track);
            _trackinfobar = new TrackInfoBar(_topcontainer, game.Track);
        }
        private string GetTitle()
        {
            string name = game.Track.Name;
            var changes = Math.Min(999, game.Track.TrackChanges);
            if (changes > 0)
            {
                name += " (*)\n";
                if (changes > 50)
                {
                    int rounded = changes;
                    if (changes < 999)
                    {
                        if (changes >= 200)
                        {
                            rounded = (changes / 100) * 100;
                        }
                        else
                        {
                            rounded = (changes / 50) * 50;
                        }
                    }
                    name += (rounded) + "+ changes";
                }
            }
            return name;
        }
        protected override void OnChildAdded(ControlBase child)
        {
            if (child is Gwen.ControlInternal.Modal || child is WindowControl)
            {
                CurrentTools.SelectedTool.Stop();
            }
        }
        public override void Think()
        {
            while (QueuedActions.Count != 0)
            {
                QueuedActions.Dequeue().Invoke();
            }
            base.Think();
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
            var window = MessageBox.Show(this, "Would you like to download the latest version?", "Update Available! v" + Program.NewVersion, MessageBox.ButtonType.OkCancel);
            window.RenameButtons("Go to Download");
            window.Dismissed += (o, e) =>
            {
                if (window.Result == DialogResult.OK)
                {
                    try
                    {
                        OpenUrl(@"https://github.com/jealouscloud/linerider-advanced/releases/latest");
                    }
                    catch
                    {
                        ShowError("Unable to open your browser.");
                    }
                }
            };
            Program.NewVersion = null;
        }
        public void ShowError(string message)
        {
            MessageBox.Show(this, message, "Error!");
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
                    foreach (var modalchild in child.Children)
                    {
                        if (modalchild is WindowControl w)
                        {
                            ret.Add(w);
                        }
                    }
                }
            }
            return ret;
        }
        public override void Dispose()
        {
            Fonts.Dispose();
            base.Dispose();
        }
        private void ShowDialog(WindowControl window)
        {
            if (game.Track.Playing)
                game.Track.TogglePause();
            game.StopTools();
            window.ShowCentered();
        }
        public void ShowSaveDialog()
        {
            ShowDialog(new SaveWindow(this, game.Track));
        }
        public void ShowLoadDialog()
        {
            ShowDialog(new LoadWindow(this, game.Track));
        }
        public void ShowPreferencesDialog()
        {
            ShowDialog(new PreferencesWindow(this, game.Track));
        }
        public void ShowTrackPropertiesDialog()
        {
            ShowDialog(new TrackInfoWindow(this, game.Track));
        }
        public void ShowExportVideoWindow()
        {
            if (File.Exists(linerider.IO.ffmpeg.FFMPEG.ffmpeg_path))
            {
                ShowDialog(new ExportWindow(this, game.Track, game));
            }
            else
            {
                ShowffmpegMissing();
            }
        }
        public void ShowGameMenuWindow()
        {
            ShowDialog(new GameMenuWindow(this, game.Track));
        }
        public void ShowffmpegMissing()
        {
            var mbox = MessageBox.Show(
                this,
                "This feature requires ffmpeg for encoding.\n" +
                "Automatically download it?",
                "ffmpeg not found",
                MessageBox.ButtonType.YesNo,
                true,
                true);
            mbox.Dismissed += (o, e) =>
              {
                  if (e == DialogResult.Yes)
                  {
                      ShowDialog(new FFmpegDownloadWindow(this, game.Track));
                  }
              };
        }
        public void ShowLineWindow(Game.GameLine line, int x, int y)
        {
            var wnd = new LineWindow(this, game.Track, line);
            ShowDialog(wnd);
            wnd.SetPosition(x - wnd.Width / 2, y - wnd.Height / 2);
        }
    }
}