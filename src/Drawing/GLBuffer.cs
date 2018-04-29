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

using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK;

using System.Drawing;
using System.Diagnostics;

namespace linerider.Drawing
{
    unsafe class GLBuffer<T> : IDisposable
    where T : struct
    {
        protected sealed class Pinnable
        {
            public byte PinMe;
        }
        public int BufferSize { get; private set; }
        public bool Mapped => _map != IntPtr.Zero;
        protected BufferTarget _target;
        protected int _bufferid;
        protected int _objectsize;
        protected IntPtr _map = IntPtr.Zero;
        private bool _is_size4_valuetype = false;
        private static Dictionary<BufferTarget, int> BoundBuffers = new Dictionary<BufferTarget, int>();
        public GLBuffer(BufferTarget target)
        {
            _objectsize = Marshal.SizeOf(typeof(T));
            _target = target;
            _bufferid = GL.GenBuffer();
            var thistype = typeof(T);
            if (thistype == typeof(int) ||
                thistype == typeof(uint) ||
                thistype == typeof(float))
                _is_size4_valuetype = true;
            BufferSize = 0;
        }
        /// <summary>
        /// Resizes the buffer object, copying its existing data
        /// </summary>
        public void SetSize(int newsize, BufferUsageHint usageHint, bool shouldcopy = true)
        {
            T[] copy = new T[0];
            if (shouldcopy)
                copy = GetData(0, BufferSize);
            GL.BufferData(
                _target,
                newsize * _objectsize,
                IntPtr.Zero,
                usageHint);

            BufferSize = newsize;
            if (copy.Length != 0)
            {
                SetData(copy, 0, 0, Math.Min(newsize, copy.Length));
            }
        }
        public T[] GetData(int start, int length)
        {
            if (length == 0)
                return new T[0];
            T[] copy = new T[length];
            if (Mapped)
                throw new Exception("cannot call getdata while buffer is mapped");
            uint bytecount = (uint)length * (uint)_objectsize;
            var cast = Unsafe.As<Pinnable>(copy);
            fixed (void* pinned = &cast.PinMe)
            {
                byte* dst = (byte*)Unsafe.AsPointer(ref copy[0]);
                GL.GetBufferSubData(
                    _target,
                    IntPtr.Zero,
                    (int)bytecount,
                    (IntPtr)dst);
            }
            return copy;
        }
        /// <summary>
        /// Set the size of the buffer and set its contents
        /// Basically, it calls glBufferData
        /// </summary>
        public void BufferData(T[] input, int srcstart, int count, BufferUsageHint hint)
        {
            GL.BufferData(
                _target,
                count * _objectsize,
                input,
                hint);
        }

        /// <summary>
        /// Sets the specified data in the buffer, if the buffer is mapped
        /// copies to that pointer.
        /// if T is a value type like int uint or float, optimizes further.
        /// </summary>
        public void SetData(T[] input, int srcstart, int dststart, int count)
        {
            uint bytecount = (uint)count * (uint)_objectsize;
            if (dststart + count > BufferSize)
                throw new IndexOutOfRangeException(
                    "SetData failed, greater than the size of the buffer");
            if (srcstart + count > input.Length)
                throw new IndexOutOfRangeException(
                    "SetData failed, count larger than srcbuffer");
            // not technically an error, but dont get a ptr.
            if (input.Length == 0)
                return;
            var cast = Unsafe.As<Pinnable>(input);
            fixed (void* pinned = &cast.PinMe)
            {
                var ptr = (IntPtr)Unsafe.AsPointer(ref input[0]);
                var src = IntPtr.Add(ptr, _objectsize * srcstart);
                if (Mapped)
                {
                    var dst = IntPtr.Add(_map, _objectsize * dststart);
                    if (_is_size4_valuetype)
                    {
                        var intarray = Unsafe.As<int[]>(input);
                        // marshal.copy uses hardware intrinsics, so it should 
                        // copy faster
                        Marshal.Copy(intarray, srcstart, dst, count);
                    }
                    else
                    {
                        Unsafe.CopyBlock((void*)dst,
                                         (void*)src,
                                         (uint)(count * _objectsize));
                    }
                }
                else
                {
                    GL.BufferSubData(_target,
                                     new IntPtr(dststart * _objectsize),
                                     (int)bytecount,
                                     src);
                }
            }
        }
        public virtual void SetData(T value, int dststart)
        {
            SetData(new T[] { value }, 0, dststart, 1);
        }
        /// <summary>
        /// Binds the buffer to the current context
        /// call this before doing anything
        /// </summary>
        public void Bind()
        {
            if (!BoundBuffers.TryGetValue(_target, out int currentlybound) || currentlybound != _bufferid)
            {
                GL.BindBuffer(_target, _bufferid);
                BoundBuffers[_target] = _bufferid;
            }
            else
            {
                Debug.Fail("buffer is already bound");
            }
        }
        /// <summary>
        /// Unbinds the buffer.
        /// Call this after use.
        /// </summary>
        public void Unbind()
        {
            if (BoundBuffers.TryGetValue(_target, out int currentlybound) && currentlybound == _bufferid)
            {
                GL.BindBuffer(_target, 0);
                BoundBuffers[_target] = 0;
            }
            else
            {
                Debug.Fail("currentlybound != _bufferid, cant unbind buffer");
            }
        }
        /// <summary>
        /// Map the buffer from gpu memory for writing
        /// No other buffer operations can be done like glbuffersubdata or
        /// getdata while mapped.
        /// Fastest when you can use sse intrinsics to copy to it
        /// </summary>
        public void Map()
        {
            if (_map == IntPtr.Zero)
            {
                _map = GL.MapBuffer(_target, BufferAccess.WriteOnly);
            }
        }
        /// <summary>
        /// Unmap the buffer from memory allowing it to be used again.
        /// </summary>
        public void Unmap()
        {
            if (_map != IntPtr.Zero)
            {
                GL.UnmapBuffer(_target);
                _map = IntPtr.Zero;
            }
        }
        public void Dispose()
        {
            GL.DeleteBuffer(_bufferid);
        }
    }
}