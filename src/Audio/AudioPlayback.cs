//
//  AudioPlayback.cs
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
using System.Threading;
using OpenTK;
using OpenTK.Input;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using linerider.Audio;

namespace linerider.Audio
{
	public class AudioPlayback : GameService
	{
		private static string _currentsong = null;
		private static AudioDevice _device;
		private static SoundStream _music;
		public static float SongPosition
		{
			get
			{
				if (_music == null)
					return 0;
				return (float)_music.Position.TotalSeconds;
			}
		}

		public static void Init()
		{
			if (_device == null)
			{
				_device = new AudioDevice();
			}
		}

		public static bool LoadFile(ref string file)
		{
			Init();
			if (_currentsong == file)
				return false;
			if (_music != null)
			{
				Stop();
				_music.Dispose();
			}
			TimeSpan duration = TimeSpan.Zero;
			bool hardexit = false;
			file = ffmpeg.FFMPEG.ConvertSongToOgg(file, (string obj) =>
			{
				var idx = obj.IndexOf("Duration: ", StringComparison.InvariantCulture);
				if (idx != -1)
				{
					idx += "Duration: ".Length;
					string length = obj.Substring(idx, obj.IndexOf(",", idx, StringComparison.InvariantCulture) - idx);
					var ts = TimeSpan.Parse(length);
					duration = ts;
				}
				idx = obj.IndexOf("time=", StringComparison.InvariantCulture);
				if (idx != -1)
				{
					idx += "time=".Length;
					string length = obj.Substring(idx, obj.IndexOf(" ", idx, StringComparison.InvariantCulture) - idx);
					var ts = TimeSpan.Parse(length);
					game.Title = Program.WindowTitle + string.Format(" [Converting song | {0:P}% | Hold ESC to cancel]", ts.TotalSeconds / duration.TotalSeconds);// "[" + (ts.TotalSeconds / duration.TotalSeconds) + "% converting song]";
				}

				if (Keyboard.GetState()[Key.Escape])
				{
					hardexit = true;
					return false;
				}
				return true;
			});
			game.Title = Program.WindowTitle;
			if (hardexit)
				return false;
			try
			{
				if (File.Exists(file))
				{
					SoundStream music = new SoundStream(file);
					_music = music;
					_currentsong = file;
					return true;
				}
				return false;
			}
			catch (Exception e)
			{
				Program.NonFatalError("Unable to load song file " + e);
				return false;
			}
		}

		public static void Pause()
		{
			_music?.Pause();
		}

		public static void Resume(float seconds, float rate)
		{
			_music?.Play(seconds, rate);
		}

		public static void Stop()
		{
			_music?.Stop();
		}

		public static void CloseDevice()
		{
			if (_device != null)
			{
				_device.Dispose();
				_device = null;
			}
		}
	}
}