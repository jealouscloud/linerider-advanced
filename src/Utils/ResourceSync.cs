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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace linerider
{
    public class ResourceSync
    {
        private readonly object _syncroot = new object();
        public sealed class ResourceLock : IDisposable
        {
            private bool _disposed = false;
            private bool _read;
            private bool _write;
            private ResourceSync _parent;
            public ResourceLock(bool read, bool write, ResourceSync parent)
            {
                _read = read;
                _write = write;
                _parent = parent;
            }
            public void Dispose()
            {
                if (_disposed)
                    return;
                if (_read)
                {
                    _parent.ReleaseRead();
                }
                if (_write)
                {
                    _parent.ReleaseWrite();
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
            ~ResourceLock()
            {
                throw new InvalidOperationException(String.Format("ResourceLock object finalized [{1:X}]: {0}", this, GetHashCode()));
                //Debug.Print(String.Format("IDisposable object finalized: {0}", GetType()));
            }
        }
        private volatile int _readlocks = 0;
        private volatile int _writertid = 0;
        private ConcurrentQueue<int> _writerqueue = new ConcurrentQueue<int>();
        public ResourceLock TryAcquireRead()
        {
            if (Thread.CurrentThread.ManagedThreadId == _writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.
            Interlocked.Increment(ref _readlocks);
            if (_writertid == 0)
            {
                return new ResourceLock(true, false, this);
            }
            Interlocked.Decrement(ref _readlocks);
            return null;
        }
        public ResourceLock AcquireRead()
        {
            if (Thread.CurrentThread.ManagedThreadId == _writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.

            Stopwatch sw = Stopwatch.StartNew();
            Interlocked.Increment(ref _readlocks);
            while (_writertid != 0)
            {
                HybridWait(sw.ElapsedMilliseconds);
            }
            return new ResourceLock(true, false, this);
        }
        public ResourceLock AcquireWrite()
        {
            if (Thread.CurrentThread.ManagedThreadId == _writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.
            Stopwatch sw = Stopwatch.StartNew(); ;
            bool writetaken = false;
            bool isnext = false;
            var tid = Thread.CurrentThread.ManagedThreadId;
            int nextout = 0;
            _writerqueue.Enqueue(tid);

            while (_readlocks != 0 && !writetaken)
            {
                if (!isnext && _writerqueue.TryPeek(out nextout))
                {
                    isnext = nextout == tid;
                }
                if (isnext && Interlocked.CompareExchange(ref _writertid, tid, 0) == tid)
                {
                    writetaken = true;
                    int q = 0;
                    if (!_writerqueue.TryDequeue(out q) || q != tid)
                    {
                        throw new Exception("Resource synchronize error");
                    }
                    if (_readlocks == 0)
                        break;
                }
                HybridWait(sw.ElapsedMilliseconds);
            }
            _writertid = tid;
            return new ResourceLock(false, true, this);
        }

        private void ReleaseRead()
        {
            Interlocked.Decrement(ref _readlocks);
        }
        private void ReleaseWrite()
        {
            _writertid = 0;
        }
        private void HybridWait(long elapsed)
        {
            if (elapsed > 100)
            {
                Thread.Sleep(1);
            }
            else if (elapsed > 16)
            {
                Thread.Sleep(0);
            }
            else
            {
                Thread.SpinWait(100);
            }
        }
    }
}
