using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;
using linerider.Drawing;

namespace linerider.UI
{
    public class ExportWindow : DialogBase
    {
        private RichLabel _descriptionlabel;
        private Label _error;
        private MainWindow _game;
        private const string howto = "You are about to export your track as a video file. Make sure the end of the track is marked by a flag. " +
            "It will be located in your line rider user directory (Documents/LRA).\r\n" +
            "Please allow some minutes depending on your computer speed. " +
            "The window will become unresponsive during this time.\n\n" +
            "After recording, a console window may open to encode the video. " +
            "Closing it will cancel the process and all progress will be lost.";
        public ExportWindow(GameCanvas parent, Editor editor, MainWindow window) : base(parent, editor)
        {
            _game = window;
            Title = "Export Video";
            _descriptionlabel = new RichLabel(this)
            {
                AutoSizeToContents = true
            };
            if (!SafeFrameBuffer.CanRecord)
            {
                _descriptionlabel.AddText(
                    "Video export is not supported on this machine.\n\nSorry.",
                    Skin.Colors.Text.Foreground);
            }
            else
            {
                _descriptionlabel.AddText(howto, Skin.Colors.Text.Foreground);
            }
            _descriptionlabel.Dock = Dock.Top;
            _error = new Label(this)
            {
                Dock = Dock.Top,
                TextColor = Color.Red,
                IsHidden = true,
                Margin = new Margin(0, 0, 0, 10)
            };
            AutoSizeToContents = true;
            MinimumSize = new Size(400, 300);
            MakeModal(true);
            Setup();
        }
        private void SetError(string error)
        {
            _error.IsHidden = false;
            _error.Text = "\n" + error;
        }
        private CheckProperty AddPropertyCheckbox(PropertyTable prop, string label, bool value)
        {
            var check = new CheckProperty(null);
            prop.Add(label, check);
            check.IsChecked = value;
            return check;
        }
        private void Setup()
        {
            Panel content = new Panel(this)
            {
                Dock = Dock.Fill,
                AutoSizeToContents = true,
                ShouldDrawBackground = false
            };
            Panel bottomrow = new Panel(content)
            {
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
                ShouldDrawBackground = false,

            };
            PropertyTree proptree = new PropertyTree(content)
            {
                Dock = Dock.Top,
                AutoSizeToContents = true,
                Width = 200,
                Margin = new Margin(0, 0, 0, 10)
            };
            var table = proptree.Add("Output Settings", 150);
            var qualitycb = new ComboBoxProperty(table);
            qualitycb.AddItem("720p");
            qualitycb.AddItem("1080p");
            table.Add("Quality", qualitycb);

            var smoothcheck = AddPropertyCheckbox(
                table,
                "Smooth Playback",
                Settings.RecordSmooth);
            var music = AddPropertyCheckbox(
                table,
                "Save Music",
                !Settings.MuteAudio && Settings.RecordMusic);
            if (Settings.MuteAudio)
            {
                music.Disable();
            }
            table = proptree.Add("Overlay settings", 150);
            var ppf = AddPropertyCheckbox(
                table,
                "Show P/f",
                Settings.Recording.ShowPpf);
            var fps = AddPropertyCheckbox(
                table,
                "Show FPS",
                Settings.Recording.ShowFps);
            var tools = AddPropertyCheckbox(
                table,
                "Show Tools",
                Settings.Recording.ShowTools);
            proptree.ExpandAll();
            Button Cancel = new Button(bottomrow)
            {
                Dock = Dock.Right,
                Text = "Cancel",
                Margin = new Margin(10, 0, 0, 0),
            };
            Cancel.Clicked += (o, e) =>
            {
                Close();
            };
            Button ok = new Button(bottomrow)
            {
                Dock = Dock.Right,
                Text = "Save"
            };
            if (!SafeFrameBuffer.CanRecord || !CheckRecord())
            {
                ok.Disable();
            }
            ok.Clicked += (o, e) =>
                {
                    Close();
                    Settings.Recording.ShowPpf = ppf.IsChecked;
                    Settings.Recording.ShowFps = fps.IsChecked;
                    Settings.Recording.ShowTools = tools.IsChecked;

                    Settings.Record1080p = qualitycb.SelectedItem.Text == "1080p";
                    Settings.RecordSmooth = smoothcheck.IsChecked;
                    if (!music.IsDisabled)
                    {
                        Settings.RecordMusic = music.IsChecked;
                    }
                    Settings.Save();
                    Record();
                };
        }
        private bool CheckRecord()
        {
            if (_editor.GetFlag() == null)
            {
                SetError("No flag detected. Place one at the end of the track\nso the recorder knows where to stop.");
                return false;
            }
            else if (_editor.Name == Utils.Constants.DefaultTrackName)
            {
                SetError("Please save your track before recording.");
                return false;
            }
            return true;
        }
        private void Record()
        {
            IO.TrackRecorder.RecordTrack(
                _game,
                Settings.Record1080p,
                Settings.RecordSmooth,
                Settings.RecordMusic && !Settings.MuteAudio);
        }
    }
}
