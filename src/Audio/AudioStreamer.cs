//
//  GLWindow.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.Threading;
using NVorbis;

namespace linerider.Audio
{
    class AudioStreamer
    {
        private readonly object _sync = new object();
        private int[] _buffers;
        private int _source;
        private AudioSource _stream;
        private bool runthread = true;
        private bool ShouldQueueMusic = false;
        public float Speed = 1;
        public double SongPosition
        {
            get
            {
                if (_stream == null || _stream.Channels == 0)
                    return 0;

                var queued = 0;
                float elapsed;
                AL.GetSource(_source, ALGetSourcei.BuffersQueued, out queued);
                AL.GetSource(_source, ALSourcef.SecOffset, out elapsed);
                double buffertime = (double)_stream.Buffer.Length / _stream.SampleRate / _stream.Channels;
                double offset = (queued * buffertime);
                //special case for if we're at the end of the audio.
                if (_stream.ReadSamples != _stream.Buffer.Length && queued > 1)
                {
                    offset -= buffertime - ((double)_stream.ReadSamples / _stream.SampleRate / _stream.Channels);
                }
                if (Speed < 0)
                {
                    return _stream.Position + (offset - elapsed);
                }
                else
                {
                    return _stream.Position - (offset - elapsed);
                }
            }
        }
        public bool Playing
        {
            get
            {
                return AL.GetSourceState(_source) == ALSourceState.Playing;
            }
        }
        public AudioStreamer()
        {
            _source = AL.GenSource();
            _buffers = AL.GenBuffers(3);
            new Thread(BufferRefiller) { IsBackground = true }.Start();
        }
        public void LoadSoundStream(AudioSource stream)
        {
            if (_stream != null)
                _stream.Dispose();
            _stream = stream;
        }
        public void Pause()
        {
            lock (_sync)
            {
                AL.SourcePause(_source);
                ShouldQueueMusic = false;
            }
        }
        public void Play(float time, float rate)
        {
            lock (_sync)
            {
                var test = SongPosition;
                Empty();
                if (_stream.Duration > time)
                {
                    _stream.Position = time;
                    ShouldQueueMusic = true;
                    Speed = rate;
                    AL.Source(_source, ALSourcef.Gain, Settings.Volume / 100);
                    AL.Source(_source, ALSourcef.Pitch, rate);
                    for (int i = 0; i < _buffers.Length; i++)
                    {
                        QueueBuffer(_buffers[i]);
                    }
                    AL.SourcePlay(_source);
                }
            }
        }
        private void Empty()
        {
            lock (_sync)
            {
                AL.SourceStop(_source);


                AudioDevice.Check();
                int queued;
                AL.GetSource(_source, ALGetSourcei.BuffersQueued, out queued);
                for (int i = 0; i < queued; i++)
                {
                    AL.SourceUnqueueBuffer(_source);

                    AudioDevice.Check();
                }
                try
                {
                    AudioDevice.Check();
                }
                catch
                {
                    //unqueueing buffers can fail
                }
                return;
            }
        }
        private void QueueBuffer(int buffer)
        {
            if (!ShouldQueueMusic)
                return;
            int len = (Speed > 0 ? _stream.ReadBuffer() : _stream.ReadBufferReversed());
            if (len > 0)
            {
                AL.BufferData(buffer, _stream.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, _stream.Buffer, len * sizeof(short), _stream.SampleRate);
                AudioDevice.Check();
                AL.SourceQueueBuffer(_source, buffer);
                AudioDevice.Check();
            }
            if (len != _stream.Buffer.Length)//we've reached the end
                ShouldQueueMusic = false;
        }
        private void BufferRefiller()
        {
            try
            {
                while (runthread)
                {
                    lock (_sync)
                    {
                        if (AL.GetSourceState(_source) == ALSourceState.Playing)
                        {
                            var processed = 0;
                            AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out processed);
                            for (int i = 0; i < processed; i++)
                            {
                                var buffer = AL.SourceUnqueueBuffer(_source);
                                QueueBuffer(buffer);
                            }
                        }
                        else if (ShouldQueueMusic)
                        {
                            AL.SourcePlay(_source);
                            AudioDevice.Check();
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (AudioException ae)
            {
                Program.NonFatalError(ae.ToString());
            }
        }
    }
}
