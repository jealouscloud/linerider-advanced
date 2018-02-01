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
    public sealed class ResourceSync
    {
        /// This class relies on the following assumptions:
        /// Interlocked functions are atomic, but can have performance costs as they use Thread.MemoryBarrier()
        /// Volatile.* functions are also atomic, but more limited. However, they can perform better
        /// Volatile functions force a load from the cpu cache or save of at least the variable in question
        /// The current thread will have an up to date cache of what it has personally modified
        /// 
        /// principals:
        /// Any state modification must be done between acquire/realeaselockinternal
        /// if the current thread owns a writer then we will pass a dummy reader/writer to them if asked
        /// 
        /// 
        /// 
        /// 
        /// 
        /// /// 

        private int __readers = 0;
        private int __writertid = 0;
        private int __lock_generation = 1;
        private int __writer_generation = 1;
        private int _lock_waitid = 0;
        private int _writer_waitid = 0;
        public ResourceLock TryAcquireRead()
        {
            return TryCreateReader();
        }
        public ResourceLock AcquireRead()
        {
            var reader = TryCreateReader();
            if (reader != null)
                return reader;

            var wait = new IncrementalWait();
            while (true)
            {
                if (Volatile.Read(ref __writertid) == 0)
                {
                    reader = TryCreateReader();
                    if (reader != null)
                        return reader;
                }
                wait.Wait();
            }
        }
        public ResourceLock AcquireWrite()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            // this should be right because our thread wrote to __writertid last if it is us
            // so no need to use volatile
            if (tid == __writertid)
                return new ResourceLock(false, false, this);//provide dummy because we're under a parent lock.
            

            AcquireLockInternal();
            int id = Volatile.Read(ref _writer_waitid) + 1;
            Volatile.Write(ref _writer_waitid, id);
            ReleaseLockInternal();

            var wait = new IncrementalWait();

            while (Volatile.Read(ref __writer_generation) != id)
            {
                wait.Wait();
            }
            wait.Reset();
            while (true)
            {
                if (Volatile.Read(ref __readers) == 0 && SetWriter(tid))
                    break;

                wait.Wait();
            }

            return new ResourceLock(false, true, this);
        }

        private void ReleaseRead()
        {
            AcquireLockInternal();
            //this operation is not atomic, but faster than interlocked
            //that's why we have the lock
            Volatile.Write(ref __readers, Volatile.Read(ref __readers) - 1);
            ReleaseLockInternal();
        }
        private void ReleaseWrite()
        {
            AcquireLockInternal();
            //this operation is not atomic, but faster than interlocked
            Volatile.Write(ref __writer_generation, Volatile.Read(ref __writer_generation) + 1);

            Volatile.Write(ref __writertid, 0);
            ReleaseLockInternal();

        }
        /// <summary>
        /// Allows the caller to safely modify resourcesync variables
        /// performance imperative that this state is kept a minimal amount of time
        /// </summary>
        private void AcquireLockInternal()
        {
            var waitid = Interlocked.Increment(ref _lock_waitid);
            // because we use an incrementing generation it's impossible
            // for this read to cause an invalid return, so instead of 
            // forcing a load acquire with volatile we can just check here first
            if (__lock_generation == waitid)
                return;
            var inc = new IncrementalWait();
            while (Volatile.Read(ref __lock_generation) != waitid)
            {
                inc.Wait(false);
            }
        }
        private void ReleaseLockInternal()
        {
            Interlocked.Increment(ref __lock_generation);
        }
        private ResourceLock TryCreateReader()
        {
            ResourceLock ret = null;
            if (__writertid == Thread.CurrentThread.ManagedThreadId)
                return new ResourceLock(false, false, this);//dummy reader lock

            AcquireLockInternal();
            var writerid = Volatile.Read(ref __writertid);
            if (__writertid == 0)
            {
                //this operation is not atomic, but faster than interlocked
                Volatile.Write(ref __readers, Volatile.Read(ref __readers) + 1);
                ret = new ResourceLock(true, false, this);
            }
            ReleaseLockInternal();
            return ret;
        }
        private bool SetWriter(int threadid)
        {
            bool success = false;
            AcquireLockInternal();
            if (Volatile.Read(ref __readers) == 0)
            {
                Volatile.Write(ref __writertid, threadid);
                success = true;
            }
            ReleaseLockInternal();
            return success;
        }
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
    }
}
