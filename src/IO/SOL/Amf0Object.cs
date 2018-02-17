using System;
using System.Collections.Generic;
namespace linerider.IO.SOL
{
	public class Amf0Object
	{
		public enum Amf0Type
		{
			AMF0_NUMBER,//0
			AMF0_BOOLEAN,//1
			AMF0_STRING,//2
			AMF0_OBJECT,//3
			AMF0_MOVIECLIP,//4 /* not supported */
			AMF0_NULL,//5
			AMF0_UNDEFINED,//6
			AMF0_REFERENCE,//7 /* not supported */
			AMF0_ECMA_ARRAY,//8
			AMF0_OBJECT_END,
			AMF0_STRICT_ARRAY, /* not supported */
			AMF0_DATE, /* not supported */
			AMF0_LONG_STRING, /* not supported */
			AMF0_UNSUPPORTED, /* not supported */
			AMF0_RECORDSET, /* not supported */
			AMF0_XML, /* not supported */
			AMF0_TYPED_OBJECT /* not supported */
		}
		public string name;
		public Amf0Type type;
		public object data;
		public object get_property(string name)
		{
			if (data is List<Amf0Object>)
			{
				var l = data as List<Amf0Object>;
				for (int i = 0; i < l.Count; i++)
				{
					if (l[i].name == name)
						return l[i].data;
				}
			}
			throw new Exception("No property of the name " + name + " was found.");
		}
		public Amf0Object()
		{
		}
		/// <summary>
		/// Creates an AMF0_OBJECT with pname
		/// </summary>
		public Amf0Object(int pname)
		{
			name = pname.ToString(Program.Culture);
			type = Amf0Type.AMF0_OBJECT;
		}
		/// <summary>
		/// Creates an AMF0_OBJECT with pname
		/// </summary>
		public Amf0Object(string pname)
		{
			name = pname;
			type = Amf0Type.AMF0_OBJECT;
		}
		/// <summary>
		/// Creates an AMF0_STRING with pname and value
		/// </summary>
		public Amf0Object(string pname, string value)
		{
			name = pname;
			type = Amf0Type.AMF0_STRING;
			data = value;
		}
		/// <summary>
		/// Creates an AMF0_NUMBER with pname and value
		/// </summary>
		public Amf0Object(string pname, double value)
		{
			name = pname;
			type = Amf0Type.AMF0_NUMBER;
			data = value;
		}
		/// <summary>
		/// Creates an AMF0_NUMBER with pname and value
		/// </summary>
		public Amf0Object(int pname, double value)
		{
			name = pname.ToString(Program.Culture);
			type = Amf0Type.AMF0_NUMBER;
			data = value;
		}
		/// <summary>
		/// Creates an AMF0_NUMBER with pname and value
		/// </summary>
		public Amf0Object(int pname, int value)
		{
			name = pname.ToString(Program.Culture);
			type = Amf0Type.AMF0_NUMBER;
			data = value;
		}
		/// <summary>
		/// Creates an AMF0_BOOLEAN with pname and value
		/// </summary>
		public Amf0Object(string pname, bool value)
		{
			name = pname;
			type = Amf0Type.AMF0_BOOLEAN;
			data = value;
		}
		/// <summary>
		/// Creates an AMF0_BOOLEAN with pname and value
		/// </summary>
		public Amf0Object(int pname, bool value)
		{
			name = pname.ToString(Program.Culture);
			type = Amf0Type.AMF0_BOOLEAN;
			data = value;
		}
		/// <summary>
		/// Creates an Number or null with pname and value
		/// </summary>
		public Amf0Object(int pname, int? value)
		{
			name = pname.ToString(Program.Culture);
			if (value.HasValue)
			{
				type = Amf0Type.AMF0_NUMBER;
				data = value;
			}
			else
			{
				type = Amf0Type.AMF0_NULL;
			}
		}
	}
}
