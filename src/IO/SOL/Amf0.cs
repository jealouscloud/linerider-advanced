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
using System.Text;
using System.Collections.Generic;
namespace linerider.IO.SOL
{
	class Amf0
	{
		public readonly bool IsWriter;
		private BigEndianReader br;
		private BigEndianWriter bw;
		/// <summary>
		/// Creates an amf0 reader.
		/// </summary>
		/// <param name="file">File.</param>
		public Amf0(BigEndianReader file)
		{
			br = file;
			IsWriter = false;
		}
		/// <summary>
		/// Creates an amf0 Writer
		/// </summary>
		public Amf0(BigEndianWriter writer)
		{
			IsWriter = true;
			bw = writer;
		}
		public void WriteAmf0Object(Amf0Object obj)
		{
			bw.WriteMapleString(obj.name);
			if (obj.data == null)
			{
				bw.WriteByte(5);
			}
			else if (obj.data is List<Amf0Object>)
			{
				if (obj.type == Amf0Object.Amf0Type.AMF0_ECMA_ARRAY)
				{
					bw.WriteByte((byte)obj.type);
					var list = obj.data as List<Amf0Object>;
					bw.WriteInt(list.Count);
					int counter = 0;
					foreach (var lobj in list)
					{
						if (lobj.name == null)
						{
							lobj.name = counter++.ToString(Program.Culture);
						}
						WriteAmf0Object(lobj);
					}
					bw.WriteShort(0);
					bw.WriteByte(9);
				}
				else//OBJECT
				{
					obj.type = Amf0Object.Amf0Type.AMF0_OBJECT;
					bw.WriteByte((byte)obj.type);
					var list = obj.data as List<Amf0Object>;
					int counter = 0;
					foreach (var lobj in list)
					{
						if (lobj.name == null)
						{
							lobj.name = counter++.ToString(Program.Culture);
						}
						WriteAmf0Object(lobj);
					}
					bw.WriteShort(0);
					bw.WriteByte(9);
				}
			}
			else if (obj.data is int || obj.data is double || obj.data is float)
			{
				if (!(obj.data is double))
				{
					var data = Convert.ToDouble(obj.data);
					obj.data = data;
				}
				obj.type = Amf0Object.Amf0Type.AMF0_NUMBER;
				bw.WriteByte((byte)obj.type);
				bw.WriteDouble((double)obj.data);
			}
			else if (obj.data is string)
			{
				obj.type = Amf0Object.Amf0Type.AMF0_STRING;
				bw.WriteByte((byte)obj.type);
				bw.WriteMapleString((string)obj.data);
			}
			else if (obj.data is bool)
			{
				bw.WriteByte((byte)Amf0Object.Amf0Type.AMF0_BOOLEAN);
				bw.WriteByte(Convert.ToByte(obj.data));
			}
			else
			{
				throw new Exception("Unable to write type to sol file");
			}
		}
		public void SaveToFile(string filename)
		{
			if (bw != null)
			{
				System.IO.File.WriteAllBytes(filename, bw.ToArray());
			}
		}
		public List<Amf0Object> ReadAmf0(bool rootobject = false)
		{
			List<Amf0Object> retlist = new List<Amf0Object>();
			while (true)
			{
				Amf0Object ret = new Amf0Object();
				if (br.Remaining < 2 && rootobject)
					return retlist;
				ret.name = Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16()));//object name
				ret.type = (Amf0Object.Amf0Type)br.ReadByte();//type
				switch (ret.type)
				{
					case Amf0Object.Amf0Type.AMF0_NUMBER://NUMBER
						ret.data = br.ReadDouble();
						break;

					case Amf0Object.Amf0Type.AMF0_BOOLEAN://BOOLEAN
						ret.data = br.ReadBoolean();
						break;

					case Amf0Object.Amf0Type.AMF0_STRING:
						ret.data = Encoding.UTF8.GetString(br.ReadBytes(br.ReadInt16()));
						break;

					case Amf0Object.Amf0Type.AMF0_OBJECT://OBJECT
						{
							ret.data = ReadAmf0();
						}
						break;

					case Amf0Object.Amf0Type.AMF0_NULL:
					case Amf0Object.Amf0Type.AMF0_UNDEFINED:
						ret.data = null;
						break;

					case Amf0Object.Amf0Type.AMF0_ECMA_ARRAY://ecma array
						{
							br.ReadInt32();
							var ecma = ReadAmf0();
							ret.data = ecma;
							//							if (ecma.Count != l)
							//								throw new Exception("Corrupt ECMA array in SOL file");
						}
						break;

					case Amf0Object.Amf0Type.AMF0_OBJECT_END:
						return retlist;

					default:
						throw new Exception("Error reading SOL file (40)");
				}
				retlist.Add(ret);
			}
		}
	}
}
