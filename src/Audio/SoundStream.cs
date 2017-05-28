//
//  SoundStream.cs
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

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;
using NVorbis;

namespace linerider.Audio
{
	internal class SoundStream : IDisposable
	{
		private enum Status
		{
			Stopped,
			Playing,
			Paused
		}

		public bool Loop = false;
		private const int BufferCount = 3;
		private readonly object _threadlock = new object();
		private readonly int _source;
		private readonly bool[] _endbuffers = new bool[BufferCount];
		private readonly ALFormat _format;
		private readonly float[] _samplebuffer;
		private readonly short[] _conversionbuffer;
		private Status _threadstartstate = Status.Stopped;
		private bool _streaming;
		private int[] _buffers = new int[BufferCount];
		private Thread _thread;
		private bool _firstbuffer;
		private bool _continue;
		private float _pitch = 1;
		private VorbisReader _reader;

		private bool IsValid
		{
			get { return _reader != null && _reader.Channels != 0; }
		}
		public bool Playing = false;
		public TimeSpan Position
		{
			get
			{
				if (IsValid)
				{
					var queued = 0;
					float sec;
					AL.GetSource(_source, ALGetSourcei.BuffersQueued, out queued);
					AL.GetSource(_source, ALSourcef.SecOffset, out sec);
					return
						TimeSpan.FromSeconds((_reader.DecodedTime.TotalSeconds - (queued - sec)));
				}
				return TimeSpan.FromSeconds(0);
			}
		}

		public SoundStream(string file)
		{
			_reader = new VorbisReader(file);

			_format = _reader.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

			_conversionbuffer = new short[_reader.SampleRate * _reader.Channels];
			_samplebuffer = new float[_conversionbuffer.Length];
			_source = AL.GenSource();
			AudioDevice.Check();
			AL.Source(_source, ALSourcei.Buffer, 0);
			AudioDevice.Check();
		}

		public SoundStream(System.IO.Stream sound)
		{
			_reader = new VorbisReader(sound, false);

			_format = _reader.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

			_conversionbuffer = new short[_reader.SampleRate * _reader.Channels];
			_samplebuffer = new float[_conversionbuffer.Length];
			_source = AL.GenSource();
			AudioDevice.Check();
			AL.Source(_source, ALSourcei.Buffer, 0);
			AudioDevice.Check();
		}

		public void Dispose()
		{
			Stop();
			if (_reader != null)
			{
				_reader.Dispose();
			}
			AL.DeleteSource(_source);
		}

		public void Play(float seconds, float rate)
		{
			if (IsValid)
			{
				if (seconds > _reader.TotalTime.TotalSeconds)
					return;
				Stop();
				_reader.DecodedTime = TimeSpan.FromSeconds(seconds);
				_streaming = true;
				AL.Source(_source, ALSourcef.Gain, Settings.Default.Volume / 100f);
				AL.Source(_source, ALSourcei.Buffer, 0);
				_pitch = rate;
				AL.Source(_source, ALSourcef.Pitch, rate);
				_threadstartstate = Status.Playing;
				_continue = false;
				Playing = true;
				_thread = new Thread(DataStreamProc)
				{ IsBackground = true };
				_thread.Start();
				while (!_continue)
					Thread.Sleep(1);
			}
		}

		public void Pause()
		{
			lock (_threadlock)
			{
				if (!_streaming)
					return;
				_threadstartstate = Status.Paused;
			}
			AL.SourcePause(_source);
			AudioDevice.Check();
		}

		public void Stop()
		{
			lock (_threadlock)
			{
				_streaming = false;
			}
			if (_thread != null)
			{
				_thread.Join();
				_thread = null;
			}
			_reader.DecodedTime = TimeSpan.FromSeconds(0);
		}

		private Status GetStatus()
		{
			var status = 0;
			AL.GetSource(_source, ALGetSourcei.SourceState, out status);
			switch ((ALSourceState)status)
			{
				case ALSourceState.Initial:
				case ALSourceState.Stopped:
					return Status.Stopped;

				case ALSourceState.Paused:
					return Status.Paused;

				case ALSourceState.Playing:
					return Status.Playing;
			}
			return Status.Stopped;
		}

		private void DataStreamProc()
		{
			//based on a couple of different audio library playback methods. most notably sfml here.
			try
			{
				var requeststop = false;
				lock (_threadlock)
				{
					if (_threadstartstate == Status.Stopped)
					{
						_streaming = false;
						_continue = true;
						return;
					}
				}
				AudioDevice.Check();
				_buffers = AL.GenBuffers(BufferCount);
				for (var i = 0; i < BufferCount; i++)
					_endbuffers[i] = false;
				_firstbuffer = true;
				requeststop = FillQueue();
				bool requirefirstplay = requeststop;
				_continue = true;
				AudioDevice.Check();
				lock (_threadlock)
				{
					if (_threadstartstate == Status.Paused)
					{
						AL.SourcePause(_source);
						AudioDevice.Check();
					}
				}
				for (;;)
				{
					lock (_threadlock)
					{
						if (!_streaming)
							break;
					}
					if (GetStatus() == Status.Stopped)
					{
						if (!requeststop || requirefirstplay)
						{
							requirefirstplay = false;
							AL.Source(_source, ALSourcef.Pitch, _pitch);
							AL.SourcePlay(_source);
						}
						else
						{
							lock (_threadlock)
							{
								_streaming = false;
							}
						}
					}
					var processed = 0;
					AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out processed);
					AudioDevice.Check();
					while (processed-- != 0)
					{
						var buffer = AL.SourceUnqueueBuffer(_source);
						AudioDevice.Check();

						var buffernum = 0;
						for (var i = 0; i < BufferCount; i++)
						{
							if (_buffers[i] == buffer)
							{
								buffernum = i;
								break;
							}
						}
						if (_endbuffers[buffernum])
						{
							_reader.DecodedTime = new TimeSpan(0);
							_endbuffers[buffernum] = false;
						}
						else
						{
							int size, bits;
							AL.GetBuffer(buffer, ALGetBufferi.Size, out size);
							AL.GetBuffer(buffer, ALGetBufferi.Bits, out bits);
							if (bits == 0)
							{
								lock (_threadlock)
								{
									_streaming = false;
									requeststop = true;
									Program.NonFatalError(
										"bits in sound stream are 0. Corrupt audio format?");
									break;
								}
							}
						}
						if (!requeststop)
						{
							if (FillAndAddBuffer(buffernum))
								requeststop = true;
						}
					}
					if (GetStatus() != Status.Stopped)
						Thread.Sleep(10);
				}
				AL.SourceStop(_source);
				ClearQueue();
				AL.Source(_source, ALSourcei.Buffer, 0);
				AL.DeleteBuffers(_buffers);
			}
			catch (AudioDeviceException) //audiodevice disposed
			{
			}
			Playing = false;
		}

		private bool FillAndAddBuffer(int bufnum)
		{
			var requestStop = false;
			var readsamples = _reader.ReadSamples(_samplebuffer, 0, _samplebuffer.Length);
			for (var i = 0; i < readsamples; i++)
			{
				var temp = (int)(32767f * _samplebuffer[i]);
				if (temp > short.MaxValue) temp = short.MaxValue;
				else if (temp < short.MinValue) temp = short.MinValue;
				_conversionbuffer[i] = (short)temp;
			}
			if (_firstbuffer)
			{
				var earlysamples = Math.Min(_samplebuffer.Length / 20, readsamples);
				//10th of a second unless its too much
				for (var i = 0; i < earlysamples; i++)
				{
					var divisor = (20 - (19 * (i / (float)earlysamples)));
					if (divisor < 1)
						divisor = 1;
					_conversionbuffer[i] /= (short)divisor;
				}
				_firstbuffer = false;
			}
			if (readsamples != _samplebuffer.Length)
			{
				_endbuffers[bufnum] = true;
				if (Loop)
				{
					_reader.DecodedTime = TimeSpan.FromSeconds(0);

					// If we previously had no data, try to fill the buffer once again
					if (readsamples == 0)
					{
						return FillAndAddBuffer(bufnum);
					}
				}
				else
				{
					requestStop = true;
				}
			}
			if (readsamples != 0)
			{
				var buffer = _buffers[bufnum];

				AL.BufferData(buffer, _format, _conversionbuffer, readsamples * sizeof(short),
					_reader.SampleRate);
				AudioDevice.Check();
				AL.SourceQueueBuffer(_source, buffer);
				AudioDevice.Check();
			}
			return requestStop;
		}

		private bool FillQueue()
		{
			var requestStop = false;
			for (var i = 0; (i < BufferCount) && !requestStop; i++)
			{
				if (FillAndAddBuffer(i))
					requestStop = true;
			}
			return requestStop;
		}

		private void ClearQueue()
		{
			var queued = 0;
			AL.GetSource(_source, ALGetSourcei.BuffersQueued, out queued);

			AudioDevice.Check();
			for (var i = 0; i < queued; i++)
				AL.SourceUnqueueBuffer(_source);
			try
			{
				AudioDevice.Check();
			}
			catch
			{
				//unqueueing buffers can fail
			}
		}
	}
}