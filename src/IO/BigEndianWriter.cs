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
using System.IO;
using System.Text;
namespace linerider.IO
{
	class BigEndianWriter
	{
		protected MemoryStream _buffer;
		/// <summary>
		/// The main writer tool
		/// </summary>
		private readonly BinaryWriter _binWriter;

		/// <summary>
		/// Amount of data writen in the writer
		/// </summary>
		public int Length
		{
			get { return (int)_buffer.Length; }
		}

		/// <summary>
		/// Creates a new instance of PacketWriter
		/// </summary>
		public BigEndianWriter()
			: this(0)
		{
		}

		/// <summary>
		/// Creates a new instance of PacketWriter
		/// </summary>
		/// <param name="size">Starting size of the buffer</param>
		public BigEndianWriter(int size)
		{
			_buffer = new MemoryStream(size);
			_binWriter = new BinaryWriter(_buffer, Encoding.UTF8);
		}

		public BigEndianWriter(byte[] data)
		{
			_buffer = new MemoryStream(data);
			_binWriter = new BinaryWriter(_buffer, Encoding.UTF8);
		}

		public void Reset(int length)
		{
			_buffer.Seek(length, SeekOrigin.Begin);
		}

		public void WriteByte(int @byte)
		{
			_binWriter.Write((byte)@byte);
		}

		public void WriteBytes(byte[] @bytes)
		{
			_binWriter.Write(@bytes);
		}

		public void WriteBool(bool @bool)
		{
			_binWriter.Write(@bool);
		}

		public void WriteShort(int @short)
		{
			var bytes = BitConverter.GetBytes((short)@short);

			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(bytes);
			_binWriter.Write(bytes);
		}

		public void WriteInt(int @int)
		{
			var bytes = BitConverter.GetBytes(@int);

			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(bytes);
			_binWriter.Write(bytes);
		}

		public void WriteLong(long @long)
		{
			var bytes = BitConverter.GetBytes(@long);

			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(bytes);
			_binWriter.Write(bytes);
		}

		public void WriteDouble(double @double)
		{
			var bytes = BitConverter.GetBytes(@double);

			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(bytes);
			_binWriter.Write(bytes);
		}

		public void WriteSingle(float @float)
		{
			var bytes = BitConverter.GetBytes(@float);

			if (BitConverter.IsLittleEndian)
				System.Array.Reverse(bytes);
			_binWriter.Write(bytes);
		}

		public void WriteString(String @string)
		{
			_binWriter.Write(@string.ToCharArray());
		}

		public void WriteMapleString(String @string)
		{
			WriteShort((short)@string.Length);
			WriteString(@string);
		}
		public byte[] ToArray()
		{
			return _buffer.ToArray();
		}
	}
}
