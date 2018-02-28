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
        //todo manually stretch samples for slow/fast play
        private readonly object _sync = new object();
        private ManualResetEvent _event = new ManualResetEvent(false);
        private int[] _buffers;
        private int _alsourceid;
        private AudioSource _stream;
        private bool _needsrefill = false;
        public float Speed { get; private set; }
        public double SongPosition
        {
            get
            {
                if (_stream == null || _stream.Channels == 0)
                    return 0;

                var queued = 0;
                float elapsed;
                lock (_sync)
                {
                    AL.GetSource(_alsourceid, ALGetSourcei.BuffersQueued, out queued);
                    AL.GetSource(_alsourceid, ALSourcef.SecOffset, out elapsed);
                }
                double buffertime = (double)_stream.SamplesPerBuffer / _stream.SampleRate / _stream.Channels;
                double offset = (queued * buffertime);
                //special case for if we're at the end of the audio.
                if (_stream.ReadSamples != _stream.SamplesPerBuffer && queued > 1)
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
        public float Duration
        {
            get
            {
                if (_stream == null || _stream.Channels == 0)
                    return 0;
                return _stream.Duration;
            }
        }
        public bool Playing
        {
            get
            {
                return AL.GetSourceState(_alsourceid) == ALSourceState.Playing;
            }
        }
        public AudioStreamer()
        {
            _alsourceid = AL.GenSource();
            _buffers = AL.GenBuffers(3);
            new Thread(BufferRefiller) { IsBackground = true }.Start();
        }
        public void LoadSoundStream(AudioSource stream)
        {
            if (_stream != null)
            {
                Empty();
                _stream.Dispose();
            }
            _stream = stream;
        }
        public void Pause()
        {
            if (Playing || _needsrefill)
            {
                lock (_sync)
                {
                    AL.SourcePause(_alsourceid);
                    _needsrefill = false;
                }
            }
        }
        public void Play(float time, float rate)
        {
            lock (_sync)
            {
                Empty();
                if (_stream.Duration > time)
                {
                    _stream.Position = time;
                    _needsrefill = true;
                    Speed = rate;
                    AL.Source(_alsourceid, ALSourcef.Gain, Settings.Volume / 100f);
                    AL.Source(_alsourceid, ALSourcef.Pitch, Math.Abs(rate));
                    for (int i = 0; i < _buffers.Length; i++)
                    {
                        QueueBuffer(_buffers[i]);
                    }
                    AL.SourcePlay(_alsourceid);
                    _event.Set();
                }
            }
        }
        private void Empty()
        {
            lock (_sync)
            {
                AL.SourceStop(_alsourceid);
                AudioDevice.Check();
                AL.GetSource(_alsourceid, ALGetSourcei.BuffersQueued, out int queued);
                for (int i = 0; i < queued; i++)
                {
                    AL.SourceUnqueueBuffer(_alsourceid);

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
            }
        }
        private void BufferRefiller()
        {
            try
            {
                while (true)
                {
                    ALSourceState state;
                    lock (_sync)
                    {
                        state = AL.GetSourceState(_alsourceid);

                        if (state == ALSourceState.Playing)
                        {
                            RefillProcessed();
                        }
                        else if (_needsrefill)
                        {
                            AL.SourcePlay(_alsourceid);
                            AudioDevice.Check();
                        }
                    }
                    if (state == ALSourceState.Playing)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        _event.WaitOne();
                        _event.Reset();
                    }
                }
            }
            catch (AudioException ae)
            {
                Program.NonFatalError(ae.ToString());
            }
        }
        private void QueueBuffer(int buffer)
        {
            if (!_needsrefill)
                return;
            int len = (Speed > 0 ? _stream.ReadBuffer() : _stream.ReadBufferReversed());
            if (len > 0)
            {
                AL.BufferData(buffer, _stream.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, _stream.Buffer, len * sizeof(short), _stream.SampleRate);
                AudioDevice.Check();
                AL.SourceQueueBuffer(_alsourceid, buffer);
                AudioDevice.Check();
            }
            if (len != _stream.SamplesPerBuffer)//we've reached the end
                _needsrefill = false;
        }
        private void RefillProcessed()
        {
            var processed = 0;
            AL.GetSource(_alsourceid, ALGetSourcei.BuffersProcessed, out processed);
            for (int i = 0; i < processed; i++)
            {
                var buffer = AL.SourceUnqueueBuffer(_alsourceid);
                QueueBuffer(buffer);
            }
        }
    }
}
