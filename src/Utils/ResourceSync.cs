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

namespace linerider.Utils
{
    public class ResourceSync
    {
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
            }
        }
        private volatile int __readers = 0;
        private volatile int __writertid = 0;
        private volatile int __lock_generation = 1;
        private volatile int __writer_generation = 1;
        private int _lock_waitid = 0;
        private int _writer_waitid = 0;
        public ResourceLock TryAcquireRead()
        {
            if (Thread.CurrentThread.ManagedThreadId == __writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.
            if (__writertid == 0 && AddReader())
            {
                return new ResourceLock(true, false, this);
            }
            return null;
        }
        public ResourceLock AcquireRead()
        {
            if (Thread.CurrentThread.ManagedThreadId == __writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.

            var wait = new IncrementalWait();
            while (true)
            {
                if (__writertid == 0 && AddReader())
                    break;
                wait.Wait();
            }
            return new ResourceLock(true, false, this);
        }
        public ResourceLock AcquireWrite()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            if (tid == __writertid)
                return new ResourceLock(false, false, this);//they dont really get a lock, we're already in one.
            var id = Interlocked.Increment(ref _writer_waitid);
            var wait = new IncrementalWait();

            while (__writer_generation != id)
            {
                wait.Wait();
            }
            wait.Reset();
            while (true)
            {
                if (__readers == 0 && SetWriter(tid))
                    break;
                wait.Wait();
            }

            return new ResourceLock(false, true, this);
        }

        private void ReleaseRead()
        {
            EnterStateExclusive();
            --__readers;
            ExitStateExclusive();
        }
        private void ReleaseWrite()
        {
            EnterStateExclusive();
            ++__writer_generation;
            __writertid = 0;
            ExitStateExclusive();

        }
        /// <summary>
        /// Allows the caller to safely modify resourcesync variables
        /// performance imperative that this state is kept a minimal amount of time
        /// </summary>
        private void EnterStateExclusive()
        {
            var waitid = Interlocked.Increment(ref _lock_waitid);
            if (__lock_generation == waitid)
                return;
            var inc = new IncrementalWait();
            while (true)
            {
                inc.Wait(false);
                if (__lock_generation == waitid)
                    break;
            }
        }
        private void ExitStateExclusive()
        {
            Interlocked.Increment(ref __lock_generation);
        }
        private bool AddReader()
        {
            bool success = false;
            EnterStateExclusive();
            if (__writertid == 0)
            {
                ++__readers;
                success = true;
            }
            ExitStateExclusive();
            return success;
        }
        private bool SetWriter(int threadid)
        {
            bool success = false;
            EnterStateExclusive();
            if (__readers == 0)
            {
                __writertid = threadid;
                success = true;
            }
            ExitStateExclusive();
            return success;
        }
    }
}
