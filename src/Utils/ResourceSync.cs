//
//  ResourceSync.cs
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
            private bool _upgradableread = false;
            private bool _read;
            private bool _write;
            private ResourceSync _parent;
            public bool WritersWaiting
            {
                get
                {
                    return _parent._lock.WaitingWriteCount != 0;
                }
            }
            public ResourceLock(bool read, bool write, bool upgradableread, ResourceSync parent)
            {
                _read = read;
                _write = write;
                _parent = parent;
                _upgradableread = upgradableread;
            }
            public static ResourceLock Reader(ResourceSync parent)
            {
                return new ResourceLock(true, false, false, parent);
            }
            public static ResourceLock Writer(ResourceSync parent)
            {
                return new ResourceLock(false, true, false, parent);
            }
            public static ResourceLock UpgradableReader(ResourceSync parent)
            {
                return new ResourceLock(false, false, true, parent);
            }
            public void UpgradeToWriter()
            {
                if (_disposed)
                    return;
                if (!_upgradableread)
                {
                    throw new InvalidOperationException("Attempt to upgrade a non upgradable resource reader");
                }
                if (_write)
                {
                    throw new InvalidOperationException("Attempt to upgrade an already upgraded resource reader");
                }
                _parent.UnsafeEnterWrite();
                _write = true;

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
                if (_upgradableread)
                {
                    _parent.UnsafeExitUpgradableRead();
                }
                _disposed = true;
            }
        }
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public ResourceLock TryAcquireRead()
        {
            if (_lock.TryEnterReadLock(0))
            {
                return ResourceLock.Reader(this);
            }
            return null;
        }
        public ResourceLock AcquireRead()
        {
            UnsafeEnterRead();
            return ResourceLock.Reader(this);
        }
        public ResourceLock AcquireUpgradableRead()
        {
            UnsafeEnterUpgradableRead();
            return ResourceLock.UpgradableReader(this);
        }
        public ResourceLock AcquireWrite()
        {
            UnsafeEnterWrite();
            return ResourceLock.Writer(this);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeEnterUpgradableRead()
        {
            _lock.EnterUpgradeableReadLock();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeExitUpgradableRead()
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
}
