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
namespace linerider.IO
{
	class BigEndianReader
	{
		private byte[] mArray;
		private int _length = 0;
		private int _position = 0;

		public int Length
		{
			get { return _length; }
		}

		public int Remaining
		{
			get { return _length - _position; }
		}

		public byte[] Array
		{
			get { return mArray; }
		}

		public int Position
		{
			get { return _position; }
		}

		public BigEndianReader(byte[] data)
		{
			mArray = data;
			_length = mArray.Length;
		}

		private int Advance(int length)
		{
			int sPosition;
			if (length < 0)
				throw new ArgumentOutOfRangeException("length", "The parameter cannot be a negative number");

			sPosition = _position;
			_position += length;
			if (Remaining < 0)
			{
				_position = sPosition; //Restore old position
				throw new Exception("There isn't enough data left in the reader");
			}
			return sPosition;
		}

		public void Reset(int position)
		{
			this._position = position;
		}

		/// <summary>
		/// Skips bytes in the stream
		/// </summary>
		/// <param name="length">amount of bytes to skip</param>
		public void Skip(int length)
		{
			_position += length;
		}

		public byte Peek()
		{
			return mArray[_position];
		}

		/// <summary>
		/// Reads an unsigned byte from the stream
		/// </summary>
		/// <returns> an unsigned byte from the stream</returns>
		public byte ReadByte()
		{
			return this.mArray[Advance(1)];
		}

		public byte[] ReadBytes(int count)
		{
			byte[] b = new byte[count];
			Buffer.BlockCopy(mArray, Advance(count), b, 0, count);
			return b;
		}

		/// <summary>
		/// Reads a bool from the stream
		/// </summary>
		/// <returns>A bool</returns>
		public bool ReadBoolean()
		{
			return BitConverter.ToBoolean(mArray, Advance(1));
		}

		/// <summary>
		/// Reads a signed short from the stream
		/// </summary>
		/// <returns>A signed short</returns>
		public short ReadInt16()
		{
			var a16 = ReadBytes(2);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a16);
			return BitConverter.ToInt16(a16, 0);
		}

		/// <summary>
		/// Reads an unsigned short from the stream
		/// </summary>
		/// <returns>A signed short</returns>
		public ushort ReadUInt16()
		{
			var a16 = ReadBytes(2);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a16);
			return BitConverter.ToUInt16(a16, 0);
		}

		/// <summary>
		/// Reads a signed int from the stream
		/// </summary>
		/// <returns>A signed int</returns>
		public int ReadInt32()
		{
			var a32 = ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a32);
			return BitConverter.ToInt32(a32, 0);
		}

		/// <summary>
		/// Reads an unsigned int from the stream
		/// </summary>
		/// <returns>A signed int</returns>
		public uint ReadUInt32()
		{
			var a32 = ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a32);
			return BitConverter.ToUInt32(a32, 0);
		}

		/// <summary>
		/// Reads a signed long from the stream
		/// </summary>
		/// <returns>A signed long</returns>
		public long ReadInt64()
		{
			var a64 = ReadBytes(8);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a64);
			return BitConverter.ToInt64(a64, 0);
		}

		/// <summary>
		/// Reads a double from the stream
		/// </summary>
		/// <returns>A signed long</returns>
		public double ReadDouble()
		{
			var a64 = ReadBytes(8);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a64);
			return BitConverter.ToDouble(a64, 0);
		}

		/// <summary>
		/// Reads a single from the stream
		/// </summary>
		/// <returns>A signed long</returns>
		public double ReadSingle()
		{
			var a32 = ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(a32);
			return BitConverter.ToSingle(a32, 0);
		}

		/// <summary>
		/// Reads an ASCII string from the stream
		/// </summary>
		/// <param name="length">Amount of bytes</param>
		/// <returns>An ASCII string</returns>
		public string ReadString(int length, char nullchar = '.')
		{
			return ByteArrayToASCII(ReadBytes(length), nullchar);
		}

		/// <summary>
		/// Reads a maple string from the stream
		/// </summary>
		/// <returns>A maple string</returns>
		public string ReadMapleString()
		{
			return ReadString(ReadInt16());
		}

		public byte[] ToArray()
		{
			byte[] ret = new byte[mArray.Length];
			Buffer.BlockCopy(mArray, 0, ret, 0, mArray.Length);
			return ret;
		}

		public static String ByteArrayToASCII(byte[] bytes, char replacement = '.')
		{
			char[] ret = new char[bytes.Length];
			for (int x = 0; x < bytes.Length; x++)
			{
				if (bytes[x] < 32 && bytes[x] >= 0)
				{
					ret[x] = replacement;
				}
				else
				{
					int chr = ((short)bytes[x]) & 0xFF;
					ret[x] = (char)chr;
				}
			}
			if (replacement != '.')
			{
				return new String(ret).Replace(replacement.ToString(), "");
			}
			else
			{
				return new String(ret);
			}
		}
	}
}
