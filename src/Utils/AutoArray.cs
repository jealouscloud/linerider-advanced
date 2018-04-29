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
using System.Collections;
using System.Collections.Generic;
namespace linerider.Utils
{
    /// <summary>
    /// An array that resizes automatically similar to List or c++ vector
    /// </summary>
    public class AutoArray<T>
    {
        public T this[int index]
        {
            get
            {
                if (index >= _size)
                    throw new IndexOutOfRangeException();
                return unsafe_array[index];
            }
            set
            {
                if (index >= _size)
                    throw new IndexOutOfRangeException();
                unsafe_array[index] = value;
            }
        }
        /// <summary>
        /// How much larger to make the list on reallocate
        /// The default is 2.
        /// </summary>
        public int GrowthFactor { get; set; } = 2;
        /// <summary>
        /// The underlying array. This changes if capacity changes.
        /// It also does not do bounds checks, so be careful.
        /// </summary>
        public T[] unsafe_array;
        private int _size;
        public int Capacity
        {
            get
            {
                return unsafe_array.Length;
            }
            set
            {
                if (value != unsafe_array.Length)
                {
                    T[] newarray = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(unsafe_array, 0, newarray, 0, _size);
                    }
                    unsafe_array = newarray;
                }
            }
        }

        public int Count
        {
            get
            {
                return _size;
            }
        }
        public AutoArray()
        {
            //i picked this arbitrarily
            unsafe_array = new T[4];
        }
        public AutoArray(int capacity)
        {
            unsafe_array = new T[capacity];
        }
        /// <summary>
        /// sets the count with no checks or actions.
        /// </summary>
        public void UnsafeSetCount(int count)
        {
            EnsureCapacity(count);
            _size = count;
        }
        public void Add(T item)
        {
            if (_size == unsafe_array.Length)
                EnsureCapacity(_size + 1);
            unsafe_array[_size++] = item;
        }
        public void AddRange(IList<T> collection)
        {
            InsertRange(_size, collection);
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(unsafe_array, 0, _size);
                _size = 0;
            }
        }
        public void EnsureCapacity(int min)
        {
            if (unsafe_array.Length < min)
            {
                Capacity = min * GrowthFactor;
            }
        }
        public void Insert(int index, T item)
        {
            if (index > _size)
            {
                throw new IndexOutOfRangeException();
            }
            if (_size == unsafe_array.Length)
                EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(unsafe_array, index, unsafe_array, index + 1, _size - index);
            }
            unsafe_array[index] = item;
            _size++;
        }

        public void InsertRange(int index, IList<T> collection)
        {
            if (index > _size)
            {
                throw new IndexOutOfRangeException();
            }

            int count = collection.Count;
            if (count > 0)
            {
                EnsureCapacity(_size + count);
                if (index < _size)
                {
                    Array.Copy(unsafe_array, index, unsafe_array, index + count, _size - index);
                }
                for (int i = 0; i < collection.Count; i++)
                {
                    unsafe_array[index + i] = collection[i];
                }
                _size += count;
            }
        }
        public void RemoveAt(int index)
        {
            if (index >= _size)
            {
                throw new IndexOutOfRangeException();
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(unsafe_array, index + 1, unsafe_array, index, _size - index);
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (_size - index < count)
                throw new IndexOutOfRangeException();

            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(unsafe_array, index + count, unsafe_array, index, _size - index);
                }
            }
        }
    }
}