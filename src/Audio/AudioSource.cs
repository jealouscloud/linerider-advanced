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
using NVorbis;

namespace linerider.Audio
{
    class AudioSource : IDisposable
    {
        private VorbisReader _stream;
        private float[] _stream_buffer;
        private double _duration;
        public int SamplesPerBuffer { get; private set; }
        public short[] Buffer;
        public int ReadSamples = 0;
        public int Channels => _stream.Channels;
        public int SampleRate => _stream.SampleRate;
        public float Position
        {
            get
            {
                return (float)_stream.DecodedTime.TotalSeconds;
            }
            set
            {
                _stream.DecodedTime = TimeSpan.FromSeconds(value);
            }
        }

        public float Duration
        {
            get
            {
                return (float)_duration;
            }
        }

        public AudioSource(VorbisReader stream)
        {
            _stream = stream;
            SamplesPerBuffer = (stream.SampleRate * stream.Channels) / 3;
            _stream_buffer = new float[SamplesPerBuffer];
            Buffer = new short[SamplesPerBuffer];
            _duration = stream.TotalTime.TotalSeconds;
        }
        public int ReadBuffer()
        {
            ReadSamples = _stream.ReadSamples(_stream_buffer, 0, _stream_buffer.Length);
            for (var i = 0; i < ReadSamples; i++)
            {
                var temp = (int)(32767f * _stream_buffer[i]);
                if (temp > short.MaxValue) temp = short.MaxValue;
                else if (temp < short.MinValue) temp = short.MinValue;
                Buffer[i] = (short)temp;
            }
            return ReadSamples;
        }
        public int ReadBufferReversed()
        {
            int len = (int)Math.Min(_stream.DecodedPosition, _stream_buffer.Length / _stream.Channels);
            _stream.DecodedPosition -= len;
            ReadSamples = _stream.ReadSamples(_stream_buffer, 0, len * _stream.Channels);
            for (var i = 0; i < ReadSamples; i++)
            {
                var temp = (int)(32767f * _stream_buffer[i]);
                if (temp > short.MaxValue) temp = short.MaxValue;
                else if (temp < short.MinValue) temp = short.MinValue;
                Buffer[(ReadSamples - 1) - i] = (short)temp;
            }
            _stream.DecodedPosition -= len;
            return ReadSamples;
        }


        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
