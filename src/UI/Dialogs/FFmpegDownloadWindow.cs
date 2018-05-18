using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;
using linerider.IO.ffmpeg;
using System.Diagnostics;

namespace linerider.UI
{
    public class FFmpegDownloadWindow : DialogBase
    {
        private ProgressBar _progress;
        private Label _text;
        private string ffmpeg_download
        {
            get
            {
                if (OpenTK.Configuration.RunningOnMacOS)
                    return "https://github.com/jealouscloud/lra-ffmpeg/releases/download/ffmpeg4.0-x64/ffmpeg-mac.zip";
                else if (OpenTK.Configuration.RunningOnWindows)
                    return "https://github.com/jealouscloud/lra-ffmpeg/releases/download/ffmpeg4.0-x64/ffmpeg-win.zip";
                else if (OpenTK.Configuration.RunningOnUnix)
                    return "https://github.com/jealouscloud/lra-ffmpeg/releases/download/ffmpeg4.0-x64/ffmpeg-linux.zip";
                return null;
            }
        }
        private WebClient _webclient;
        private long _lastbytes = 0;
        private Stopwatch _downloadwatch;
        public FFmpegDownloadWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Downloading FFmpeg";
            AutoSizeToContents = true;
            MakeModal(true);
            Setup();
            MinimumSize = new Size(250, MinimumSize.Height);
            _text = new Label(this)
            {
                Dock = Dock.Top,
                Text = "Downloading..."
            };
            _progress = new ProgressBar(this)
            {
                Dock = Dock.Bottom,
            };
        }
        protected override void CloseButtonPressed(ControlBase control, EventArgs args)
        {
            base.CloseButtonPressed(control, args);
            if (_webclient != null)
            {
                _webclient.CancelAsync();
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            _webclient.Dispose();
        }
        private void DownloadComplete(string fn)
        {
            var dir = FFMPEG.ffmpeg_dir;
            string error = null;
            try
            {
                var archive = ZipFile.OpenRead(fn);
                if (archive.GetEntry("ffmpeg.exe") == null &&
                    archive.GetEntry("ffmpeg") == null)
                {
                    error = "Unable to locate ffmpeg in archive";
                }
                else
                {
                    Directory.CreateDirectory(dir);
                    ZipFile.ExtractToDirectory(fn, dir);
                }
            }
            catch (Exception e)
            {
                error = "An unknown error occured when extracting ffmpeg\n" + e.Message;
            }
            GameCanvas.QueuedActions.Enqueue(() =>
            {
                if (error == null)
                {
                    if (!File.Exists(FFMPEG.ffmpeg_path))
                    {
                        _canvas.ShowError("Download completed, but ffmpeg could not be found");
                    }
                    else
                    {
                        MessageBox.Show(_canvas, "ffmpeg was successfully downloaded\nYou can now record tracks.", "Success!", true, true);
                    }
                }
                Close();
            });
        }
        private void UpdateDownloadSpeed(long currentbytes)
        {
            var elapsed = _downloadwatch.Elapsed;
            if (elapsed.TotalSeconds < 0.5)
                return;

            var diff = currentbytes - _lastbytes;
            var rate = diff / elapsed.TotalSeconds;
            var kbs = (int)(rate / 1024);
            var mbs = kbs / 1024;
            if (mbs > 0)
            {
                _text.Text = $"Downloading... {mbs} mb/s";
            }
            else
            {
                _text.Text = $"Downloading... {kbs} kb/s";
            }
            _lastbytes = currentbytes;
            _downloadwatch.Restart();
        }
        private void Setup()
        {
            try
            {
                string filename = Path.GetTempFileName();
                _webclient = new WebClient();
                _webclient.DownloadProgressChanged += (o, e) =>
                {
                    _progress.Value = e.ProgressPercentage / 100f;
                    UpdateDownloadSpeed(e.BytesReceived);
                };
                _webclient.DownloadFileCompleted += (o, e) =>
                {
                    if (e.Error != null)
                    {
                        if (!e.Cancelled)
                        {
                            GameCanvas.QueuedActions.Enqueue(() =>
                            {
                                Close();
                                _canvas.ShowError("Download failed\n\n" + e.Error);
                            });
                        }
                    }
                    else if (!e.Cancelled)
                    {
                        DownloadComplete(filename);
                    }
                };
                string address = ffmpeg_download;
                if (address == null)
                {
                    _canvas.ShowError("Download failed:\r\nUnknown platform");
                    return;
                }
                _downloadwatch = Stopwatch.StartNew();
                _webclient.DownloadFileAsync(new Uri(address), filename);

            }
            catch (Exception e)
            {
                if (_webclient != null)
                {
                    _webclient.CancelAsync();//cleanup
                }
                _canvas.ShowError("Download failed\n\n" + e);
                Close();
            }
        }
    }
}
