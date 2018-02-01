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
using System.Runtime.CompilerServices;

namespace linerider.Utils
{
    public sealed class ResourceSync
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
                    _parent.UnsafeExitRead();
                }
                if (_write)
                {
                    _parent.UnsafeExitWrite();
                }
                _disposed = true;
            }
        }
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public ResourceLock TryAcquireRead()
        {
            if (_lock.TryEnterReadLock(0))
            {
                return new ResourceLock(true, false, this);
            }
            return null;
        }
        public ResourceLock AcquireRead()
        {
            UnsafeEnterRead();
            return new ResourceLock(true, false, this);
        }
        public ResourceLock AcquireWrite()
        {
            UnsafeEnterWrite();
            return new ResourceLock(false, true, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeEnterRead()
        {
            _lock.EnterReadLock();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeExitRead()
        {
            _lock.ExitReadLock();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeEnterWrite()
        {
            _lock.EnterWriteLock();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeExitWrite()
        {
            _lock.ExitWriteLock();
        }
    }
}
