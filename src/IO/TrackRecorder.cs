﻿//
//  TrackRecorder.cs
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gwen;
using Gwen.Controls;
using linerider.Audio;
using linerider.Drawing;
using linerider.Rendering;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using linerider.IO.ffmpeg;
using Key = OpenTK.Input.Key;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Runtime.InteropServices;
namespace linerider.IO
{
    internal static class TrackRecorder
    {
        private static byte[] _screenshotbuffer;
        public static byte[] GrabScreenshot(MainWindow game, int frontbuffer)
        {
            if (GraphicsContext.CurrentContext == null)
                throw new GraphicsContextMissingException();
            var backbuffer = game.MSAABuffer.Framebuffer;
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.ReadFramebuffer, backbuffer);
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frontbuffer);
            SafeFrameBuffer.BlitFramebuffer(0, 0, game.RenderSize.Width, game.RenderSize.Height,
                0, 0, game.RenderSize.Width, game.RenderSize.Height,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frontbuffer);

            GL.ReadPixels(0, 0, game.RenderSize.Width, game.RenderSize.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, _screenshotbuffer);
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, backbuffer);
            return _screenshotbuffer;
        }
        public static void SaveScreenshot(int width, int height, byte[] arr, string name)
        {
            var output = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect,
                ImageLockMode.ReadWrite, output.PixelFormat);
            var ptr = bmpData.Scan0;
            Marshal.Copy(arr, 0, ptr, arr.Length);

            output.UnlockBits(bmpData);
            output.Save(name, ImageFormat.Png);
        }

        public static bool Recording;
        public static bool Recording1080p;
        public static void RecordTrack(MainWindow game, bool is1080P, bool smooth, bool music)
        {
            Settings.Local.SmoothRecording = smooth;
            var flag = game.Track.GetFlag();
            if (flag == null) return;
            var resolution = new Size(is1080P ? 1920 : 1280, is1080P ? 1080 : 720);
            var oldsize = game.RenderSize;
            var invalid = false;
            using (var trk = game.Track.CreateTrackReader())
            {
                game.Track.Reset();

                var state = game.Track.GetStart();
                var frame = flag.FrameID;
                Recording = true;
                Recording1080p = is1080P;
                game.Canvas.SetSize(game.RenderSize.Width, game.RenderSize.Height);
                game.Canvas.FindChildByName("buttons").Position(Pos.CenterH);

                if (frame > 400) //many frames, will likely lag the game. Update the window as a fallback.
                {
                    if (frame > (20 * (60 * 40))) //too many frames, could lag the game very bad.
                    {
                        return;
                    }
                    game.Title = Program.WindowTitle + " [Validating flag]";
                    game.ProcessEvents();
                }
                for (var i = 0; i < frame; i++)
                {
                    state = trk.TickBasic(state);
                }
                for (var i = 0; i < state.Body.Length; i++)
                {
                    if (state.Body[i].Location != flag.State.Body[i].Location ||
                        state.Body[i].Previous != flag.State.Body[i].Previous)
                    {
                        invalid = true;
                        break;
                    }
                }
                var frontbuffer = SafeFrameBuffer.GenFramebuffer();
                SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, frontbuffer);

                var rbo2 = SafeFrameBuffer.GenRenderbuffer();
                SafeFrameBuffer.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo2);
                SafeFrameBuffer.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgb8, resolution.Width, resolution.Height);
                SafeFrameBuffer.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo2);

                SafeFrameBuffer.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                if (!invalid)
                {
                    _screenshotbuffer = new byte[game.RenderSize.Width * game.RenderSize.Height * 3];// 3 bytes per pixel
                    string errormessage = "An unknown error occured during recording.";
                    game.Title = Program.WindowTitle + " [Recording | Hold ESC to cancel]";
                    game.ProcessEvents();
                    var filename = Program.UserDirectory + game.Track.Name + ".mp4";
                    var flagbackup = flag;
                    var hardexit = false;
                    game.Track.Flag();
                    var recmodesave = Settings.Local.RecordingMode;
                    Settings.Local.RecordingMode = true;
                    game.Track.StartIgnoreFlag();
                    game.Render();
                    var dir = Program.UserDirectory + game.Track.Name + "_rec";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var firstframe = GrabScreenshot(game, frontbuffer);
                    SaveScreenshot(game.RenderSize.Width, game.RenderSize.Height, firstframe, dir + Path.DirectorySeparatorChar + "tmp" + 0 + ".png");
                    int framecount = smooth ? (frame * 60) / 40 : frame;

                    double frametime = 0;
                    Stopwatch sw = Stopwatch.StartNew();
                    for (var i = 0; i < framecount; i++)
                    {
                        if (hardexit)
                            break;
                        if (smooth)
                        {
                            var oldspot = frametime;
                            frametime += 40f / 60f;
                            if (i == 0)
                            {
                                //bugfix:
                                //frame blending uses the previous frame.
                                //so the first frame would be recorded twice,
                                //instead of blended
                                game.Track.Update(1);
                            }
                            else if ((int)frametime != (int)oldspot)
                            {
                                game.Track.Update(1);
                            }
                            var blend = frametime - Math.Truncate(frametime);
                            game.Render((float)blend);
                        }
                        else
                        {
                            game.Track.Update(1);
                            game.Render();
                        }
                        try
                        {
                            var screenshot = GrabScreenshot(game, frontbuffer);
                            SaveScreenshot(game.RenderSize.Width, game.RenderSize.Height, screenshot, dir + Path.DirectorySeparatorChar + "tmp" + (i + 1) + ".png");
                        }
                        catch
                        {
                            hardexit = true;
                            errormessage = "An error occured when saving the frame.";
                        }

                        if (Keyboard.GetState()[Key.Escape])
                        {
                            hardexit = true;
                            errormessage = "The user manually cancelled recording.";
                        }
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            game.Title = string.Format("{0} [Recording {1:P}% | Hold ESC to cancel]", Program.WindowTitle, i / (double)framecount);
                            game.ProcessEvents();
                        }
                    }
                    game.ProcessEvents();

                    if (!hardexit)
                    {
                        var parameters = new FFMPEGParameters();
                        parameters.AddOption("framerate", smooth ? "60" : "40");
                        parameters.AddOption("i", "\"" + dir + Path.DirectorySeparatorChar + "tmp%d.png" + "\"");
                        if (music)
                        {
                            var fn = Program.UserDirectory + "Songs" +
                                     Path.DirectorySeparatorChar +
                                     Settings.Local.CurrentSong.Location;

                            parameters.AddOption("i", "\"" + fn + "\"");
                            parameters.AddOption("shortest");
                        }
                        parameters.AddOption("vf", "vflip");//we save images upside down expecting ffmpeg to flip more efficiently.
                        parameters.AddOption("c:v", "libx264");
                        parameters.AddOption("preset", "veryfast");
                        parameters.AddOption("qp", "0");

                        //    parameters.AddOption("scale",is1080p?"1920:1080":"1280:720");
                        parameters.OutputFilePath = filename;
                        var failed = false;
                        if (File.Exists(filename))
                        {
                            try
                            {
                                File.Delete(filename);
                            }
                            catch
                            {
                                Program.NonFatalError("A file with the name " + game.Track.Name + ".mp4 already exists");
                                failed = true;
                                errormessage = "Cannot replace a file of the existing name " + game.Track.Name + ".mp4.";
                            }
                        }
                        if (!failed)
                        {
                            game.Title = Program.WindowTitle + " [Encoding Video | 0%]";
                            game.ProcessEvents();
                            try
                            {
                                FFMPEG.Execute(parameters, (string s) =>
                                {
                                    int idx = s.IndexOf("frame=", StringComparison.InvariantCulture);
                                    if (idx != -1)
                                    {
                                        idx += "frame=".Length;
                                        for (; idx < s.Length; idx++)
                                        {
                                            if (char.IsNumber(s[idx]))
                                                break;
                                        }
                                        var space = s.IndexOf(" ", idx, StringComparison.InvariantCulture);
                                        if (space != -1)
                                        {
                                            var sub = s.Substring(idx, space - idx);
                                            var parsedint = -1;
                                            if (int.TryParse(sub, out parsedint))
                                            {
                                                game.Title = Program.WindowTitle + string.Format(" [Encoding Video | {0:P}% | Hold ESC to cancel]", parsedint / (double)framecount);
                                                game.ProcessEvents();
                                                if (Keyboard.GetState()[Key.Escape])
                                                {
                                                    hardexit = true;
                                                    errormessage = "The user manually cancelled recording.";
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                    return true;
                                });
                            }
                            catch (Exception e)
                            {
                                Program.NonFatalError("ffmpeg error.\r\n" + e);
                                hardexit = true;
                                errormessage = "An ffmpeg error occured.";
                            }
                        }
                    }
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                        Program.NonFatalError("Unable to delete " + dir);
                    }
                    if (hardexit)
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch
                        {
                            Program.NonFatalError("Unable to delete " + filename);
                        }
                    }
                    Settings.Local.RecordingMode = recmodesave;
                    game.Title = Program.WindowTitle;
                    game.Track.RestoreFlag(flagbackup);
                    game.Track.Stop();
                    game.ProcessEvents();
                    var openwindows = game.Canvas.GetOpenWindows();
                    foreach (var window in openwindows)
                    {
                        var w = window as WindowControl;
                        w?.Close();
                    }
                    if (File.Exists(filename))
                    {
                        AudioService.Beep();
                    }
                    else
                    {
                        PopupWindow.Error(errormessage);
                    }
                }
                SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                SafeFrameBuffer.DeleteFramebuffer(frontbuffer);
                SafeFrameBuffer.DeleteRenderbuffers(1, new[] { rbo2 });
                game.RenderSize = oldsize;
                Recording = false;

                game.Canvas.SetSize(game.RenderSize.Width, game.RenderSize.Height);
                game.Canvas.FindChildByName("buttons").Position(Pos.CenterH);
                _screenshotbuffer = null;
            }
        }
    }
}