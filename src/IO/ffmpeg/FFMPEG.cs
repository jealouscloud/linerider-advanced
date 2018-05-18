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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;

namespace linerider.IO.ffmpeg
{
    public static class FFMPEG
    {
        private const int MaximumBuffers = 25;
        private static bool inited = false;
        public static bool HasExecutable
        {
            get
            {
                return File.Exists(ffmpeg_path);
            }
        }
        public static string ffmpeg_dir
        {
            get
            {
                string dir = Program.UserDirectory + "ffmpeg" + Path.DirectorySeparatorChar;
                if (OpenTK.Configuration.RunningOnMacOS)
                    dir += "mac" + Path.DirectorySeparatorChar;
                else if (OpenTK.Configuration.RunningOnWindows)
                    dir += "win" + Path.DirectorySeparatorChar;
                else if (OpenTK.Configuration.RunningOnUnix)
                {
                    dir += "linux" + Path.DirectorySeparatorChar;
                }
                else
                {
                    return null;
                }
                return dir;
            }
        }
        public static string ffmpeg_path
        {
            get
            {
                var dir = ffmpeg_dir;
                if (dir == null)
                    return null;
                if (OpenTK.Configuration.RunningOnWindows)
                    return dir + "ffmpeg.exe";
                else
                    return dir + "ffmpeg";
            }
        }
        static FFMPEG()
        {
        }
        private static void TryInitialize()
        {
            if (inited)
                return;
            inited = true;
            if (ffmpeg_path == null)
                throw new Exception("Unable to detect platform for ffmpeg");
            MakeffmpegExecutable();
        }
        public static string ConvertSongToOgg(string file, Func<string, bool> stdout)
        {
            TryInitialize();
            if (!file.EndsWith(".ogg", true, Program.Culture))
            {
                var par = new IO.ffmpeg.FFMPEGParameters();
                par.AddOption("i", "\"" + file + "\"");
                par.OutputFilePath = file.Remove(file.IndexOf(".", StringComparison.Ordinal)) + ".ogg";
                if (File.Exists(par.OutputFilePath))
                {
                    if (File.Exists(file))
                    {
                        File.Delete(par.OutputFilePath);
                    }
                    else
                    {
                        return par.OutputFilePath;
                    }
                }
                Execute(par, stdout);

                file = par.OutputFilePath;
            }
            return file;
        }
        public static void Execute(FFMPEGParameters parameters, Func<string, bool> stdout)
        {
            TryInitialize();
            if (String.IsNullOrWhiteSpace(ffmpeg_path))
            {
                throw new Exception("Path to FFMPEG executable cannot be null");
            }

            if (parameters == null)
            {
                throw new Exception("FFMPEG parameters cannot be completely null");
            }
            using (Process ffmpegProcess = new Process())
            {
                ProcessStartInfo info = new ProcessStartInfo(ffmpeg_path)
                {
                    Arguments = parameters.ToString(),
                    WorkingDirectory = Path.GetDirectoryName(ffmpeg_dir),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                ffmpegProcess.StartInfo = info;
                ffmpegProcess.Start();
                if (stdout != null)
                {
                    while (true)
                    {
                        string str = "";
                        try
                        {
                            str = ffmpegProcess.StandardError.ReadLine();
                        }
                        catch
                        {
                            Console.WriteLine("stdout log failed");
                            break;
                            //ignored 
                        }
                        if (ffmpegProcess.HasExited)
                            break;
                        if (str == null)
                            str = "";
                        if (!stdout.Invoke(str))
                        {
                            ffmpegProcess.Kill();
                            return;
                        }
                    }
                }
                else
                {
                    /*if (debug)
					{
						string processOutput = ffmpegProcess.StandardError.ReadToEnd();
					}*/

                    ffmpegProcess.WaitForExit();
                }
            }

        }
        private static void MakeffmpegExecutable()
        {
            if (OpenTK.Configuration.RunningOnUnix)
            {
                try
                {
                    using (Process chmod = new Process())
                    {
                        ProcessStartInfo info = new ProcessStartInfo("/bin/chmod")
                        {
                            Arguments = "+x ffmpeg",
                            WorkingDirectory = Path.GetDirectoryName(ffmpeg_dir),
                            UseShellExecute = false,
                        };
                        chmod.StartInfo = info;
                        chmod.Start();
                        if (!chmod.WaitForExit(1000))
                        {
                            chmod.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    linerider.Utils.ErrorLog.WriteLine(
                        "chmod error on ffmpeg" + Environment.NewLine + e.ToString());
                }
            }
        }
    }
}