using System;
using System.Diagnostics;
using System.Threading;
namespace linerider.Utils
{    /// <summary>
     /// IncrementalWait starts as a spinwait, graduates to yielding thread quantum, and eventually blocks until the condition ism et.
     /// </summary>
    public class IncrementalWait
    {
        private static int SpinlockThreahold = Math.Max(2, Environment.ProcessorCount - 1);
        private static int Waiters = 0;
        private static readonly int Processors = Environment.ProcessorCount;
        private int _spincount = 0;
        private Stopwatch _stopwatch;
        const int MaxSpins = 10;
        public void Reset()
        {
            _spincount = 0;
            _stopwatch?.Stop();
        }
        public void Wait(bool allowblocking = true)
        {
            if (_stopwatch == null)
                _stopwatch = Stopwatch.StartNew();
            else if (_stopwatch.IsRunning)
                _stopwatch.Start();
            if (allowblocking && _stopwatch.ElapsedMilliseconds > 10)
            {
                Thread.Sleep(1);
            }
            else
            {
                HybridWait(_spincount++);
            }
        }
        private static void HybridWait(int spincount)
        {
            Interlocked.Increment(ref Waiters);
            if (spincount < MaxSpins && Processors > 1)
            {
                // don't starve the cpu
                if (Waiters > SpinlockThreahold)
                {
                    Thread.Yield();
                }
                else
                {
                    Thread.SpinWait(100 * (spincount + 1));    // Wait a few dozen instructions to let another processor release lock. 
                }
            }
            else
            {
                Thread.Yield();
            }
            Interlocked.Decrement(ref Waiters);
        }
    }
}