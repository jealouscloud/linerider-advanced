//
//  RectLRTB.cs
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
using System.Collections;
using System.Collections.Generic;
namespace linerider.Utils
{
    /// <summary>
    /// A class heavily referencing the .net list<T>
    /// </summary>
    public class ArrayWrapper<T>
    {
        public T[] Arr;
        private int _size;
        public int Capacity
        {
            get
            {
                return Arr.Length;
            }
            set
            {
                if (value != Arr.Length)
                {
                    T[] newarray = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(Arr, 0, newarray, 0, _size);
                    }
                    Arr = newarray;
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
        public ArrayWrapper(int capacity)
        {
            Arr = new T[capacity];
        }
        public void Add(T item)
        {
            if (_size == Arr.Length)
                EnsureCapacity(_size + 1);
            Arr[_size++] = item;
        }
        public void AddRange(IList<T> collection)
        {
            InsertRange(_size, collection);
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(Arr, 0, _size); 
                _size = 0;
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(Arr, 0, array, arrayIndex, _size);
        }

        private void EnsureCapacity(int min)
        {
            if (Arr.Length < min)
            {
                Capacity = min * 2;
            }
        }
        public void Insert(int index, T item)
        {
            if (index > _size)
            {
                throw new IndexOutOfRangeException();
            }
            if (_size == Arr.Length)
                EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(Arr, index, Arr, index + 1, _size - index);
            }
            Arr[index] = item;
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
                    Array.Copy(Arr, index, Arr, index + count, _size - index);
                }
                for(int i = 0; i < collection.Count; i++)
                {
                    Arr[index+i] = collection[i];
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
                Array.Copy(Arr, index + 1, Arr, index, _size - index);
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
                    Array.Copy(Arr, index + count, Arr, index, _size - index);
                }
            }
        }
    }
}