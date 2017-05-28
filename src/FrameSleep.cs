//
//  FrameSleep.cs
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
using System.Runtime.InteropServices;
using System.Threading;
using Timer = System.Timers.Timer;

namespace linerider
{
    public static class FrameSleep
    {
        private class Win32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct TimeCaps
            {
                public UInt32 wPeriodMin;
                public UInt32 wPeriodMax;
            };

            [DllImport("winmm.dll")]
            public static extern uint timeEndPeriod(uint uMilliseconds);

            [DllImport("winmm.dll")]
            public static extern uint timeBeginPeriod(uint uMilliseconds);

            [DllImport("winmm.dll", SetLastError = true)]
            public static extern UInt32 timeGetDevCaps(ref TimeCaps timeCaps,
            UInt32 sizeTimeCaps);
        }

        private static readonly bool Supportwinsleep = false;
        private static readonly bool Supporttimersleep = false;
        private static Timer timer;
        private static AutoResetEvent are = new AutoResetEvent(false);
        private static Win32.TimeCaps timecaps = new Win32.TimeCaps();

        static FrameSleep()
        {
                try
                {
                    if (Win32.timeGetDevCaps(ref timecaps, (uint)Marshal.SizeOf(typeof(Win32.TimeCaps))) != 0)
                        throw new Exception("Unexpected error for timeBeginPeriod");
                    if (Win32.timeBeginPeriod(timecaps.wPeriodMin) != 0)
                        throw new Exception("Unexpected error for timeBeginPeriod");
                    Thread.Sleep(1);
                    Win32.timeEndPeriod(timecaps.wPeriodMin);
                    Supportwinsleep = true;
                }
                catch
                {
                    timer = new Timer();
                    timer.AutoReset = false;
                    timer.Elapsed += (o, e) =>
                    {
                        are.Set();
                    };
                    Supporttimersleep = true;
                }
        }

        /// <summary>
        /// WARNING ON WINDOWS THIS DOES NOT SUPPORT MULTIPLE THREADS SLEEPING
        /// </summary>
        public static void Sleep(long interval)
        {
            if (interval <= 0)
                return;
            if (Supportwinsleep)
            {
                if (Win32.timeBeginPeriod(timecaps.wPeriodMin) != 0)
                    throw new Exception("Unexpected error for timeBeginPeriod");
                Thread.Sleep((int)interval);
                Win32.timeEndPeriod(timecaps.wPeriodMin);
            }
            else if (Supporttimersleep)
            {
                are.Reset();
                timer.Interval = interval;
                timer.Start();
                are.WaitOne();
            }
            else
            {
                Thread.Sleep((int)interval);
            }
        }
    }
}