//
//  FFMPEG.cs
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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;

namespace linerider.ffmpeg
{
	public static class FFMPEG
	{
		public static string FFMPEGExecutableFilePath;

		private const int MaximumBuffers = 25;

		static FFMPEG()
		{
		}
		private static bool inited = false;
		private static void TryInitialize()
		{
			if (inited)
				return;
			inited = true;
			string ffmpegname = Program.CurrentDirectory + Program.BinariesFolder + Path.DirectorySeparatorChar + "ffmpeg";
			if (OpenTK.Configuration.RunningOnMacOS)
				ffmpegname += "-mac";
			else if (OpenTK.Configuration.RunningOnWindows)
				ffmpegname += "-win.exe";
			else if (OpenTK.Configuration.RunningOnUnix)
			{
				ffmpegname += "-linux";
			}
			else
			{
				throw new Exception("Unable to detect platform for ffmpeg");
			}
			FFMPEGExecutableFilePath = ffmpegname;
		}
		public static string ConvertSongToOgg(string file, Func<string, bool> stdout)
		{
			TryInitialize();
			if (!file.EndsWith(".ogg", true, Program.Culture))
			{
				var par = new ffmpeg.FFMPEGParameters();
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
			if (String.IsNullOrWhiteSpace(FFMPEGExecutableFilePath))
			{
				throw new Exception("Path to FFMPEG executable cannot be null");
			}

			if (parameters == null)
			{
				throw new Exception("FFMPEG parameters cannot be completely null");
			}

			using (Process ffmpegProcess = new Process())
			{
				ProcessStartInfo info = new ProcessStartInfo(FFMPEGExecutableFilePath)
				{
					Arguments = parameters.ToString(),
					WorkingDirectory = Path.GetDirectoryName(FFMPEGExecutableFilePath),
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

	}

}