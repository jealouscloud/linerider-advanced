//
//  SOL.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK;
namespace linerider
{
	internal class SOL
	{
		public Amf0Object RootObject = new Amf0Object();
		public SOL(string location)
		{
			var bytes = File.ReadAllBytes(location);
			BigEndianReader br = new BigEndianReader(bytes);
			///HEADER///
			br.ReadInt16();//sol_version
			br.ReadInt32();//file lengthh
			if (br.ReadInt32() != 0x5443534F)//TCSO
				throw new Exception("Invalid magic number, maybe this isn't an SOL file?");
			br.ReadBytes(6);//padding
			RootObject.name = Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16()));//shared object name
			if (RootObject.name != "savedLines")
				throw new Exception("invalid root object");
			if (br.ReadInt32() != 0)
				throw new Exception("Invalid AMF version");//amf version, we only support 0o
														   ///items///			
			Amf0 amf = new Amf0(br);
			RootObject.data = amf.ReadAmf0(true);
		}
		/// <summary>
		/// Saves track as SOL at location
		/// </summary>
		public SOL(string location, Track export)
		{
			BigEndianWriter bw = new BigEndianWriter();
			bw.WriteShort(0x00BF);//sol version
			bw.WriteInt(0);//length, placeholder            
			bw.WriteString("TCSO");
			bw.WriteBytes(new byte[] { 0, 4, 0, 0, 0, 0 });
			bw.WriteMapleString("savedLines");
			bw.WriteInt(0);//padding
			Amf0Object rootobj = new Amf0Object();
			rootobj.name = "trackList";
			rootobj.type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY;
			var tracks = new List<Amf0Object>();
			rootobj.data = tracks;
			WriteTrack(tracks, export);
			Amf0 amf = new Amf0(bw);
			amf.WriteAmf0Object(rootobj);
			bw.WriteByte(0);
			bw.Reset(2);
			bw.WriteInt(bw.Length - 6);
			File.WriteAllBytes(location, bw.ToArray());
		}
		private void WriteTrack(List<Amf0Object> parent, Track trk)
		{
			Amf0Object track = new Amf0Object(parent.Count);
			parent.Add(track);
			var trackdata = new List<Amf0Object>();
			track.data = trackdata;
			trackdata.Add(new Amf0Object("label", trk.Name));
			trackdata.Add(new Amf0Object("version", "6.2"));
			trackdata.Add(new Amf0Object("level", trk.Lines.Count));
			var sl = new Amf0Object("startLine");
			var dataobj = new Amf0Object("data") { type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY };

			var data = new List<Amf0Object>();
			dataobj.data = data;
			sl.data = new List<Amf0Object>() { new Amf0Object(0, trk.StartOffset.X), new Amf0Object(1, trk.StartOffset.Y) };

			trackdata.Add(sl);
			trackdata.Add(dataobj);

			SortedList<int, Line> list = new SortedList<int, Line>();
			for (int i = trk.Lines.Count - 1; i >= 0; i--)
			{
				var id = trk.Lines[i].ID;
				if (id < 0)
				{
					id = Math.Abs(id) + trk._idcounter + 100;
				}
				list.Add(id,trk.Lines[i]);
			}
			int counter = 0;
			for (int i = list.Values.Count - 1; i >= 0; i--)
			{
				var line = list.Values[i];
				var stl = line as StandardLine;
				var lineobj = new Amf0Object(counter++);
				var linedata = new List<Amf0Object>();
				linedata.Add(new Amf0Object(0, line.Position.X));
				linedata.Add(new Amf0Object(1, line.Position.Y));
				linedata.Add(new Amf0Object(2, line.Position2.X));
				linedata.Add(new Amf0Object(3, line.Position2.Y));
				linedata.Add(new Amf0Object(4, stl != null ? (int)((StandardLine)line).Extension : 0));
				linedata.Add(new Amf0Object(5, stl != null ? (bool)((StandardLine)line).inv : false));
				linedata.Add(new Amf0Object(6, stl?.Prev?.ID));
				linedata.Add(new Amf0Object(7, stl?.Next?.ID));
				linedata.Add(new Amf0Object(8, list.Keys[i]));
				linedata.Add(new Amf0Object(9, TrackLoader.LineTypeForSOL(line.GetLineType())));

				lineobj.type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY;
				lineobj.data = linedata;
				data.Add(lineobj);
			}
			if (trk.ZeroStart)
			{
				List<Amf0Object> kevans = new List<Amf0Object>();
				kevans.Add(new Amf0Object(0) { type = Amf0Object.Amf0Type.AMF0_NULL });
				List<Amf0Object> in1 = new List<Amf0Object>();
				in1.Add(new Amf0Object(0) { type = Amf0Object.Amf0Type.AMF0_NULL });
				in1.Add(new Amf0Object(1) { type = Amf0Object.Amf0Type.AMF0_NULL });
				in1.Add(new Amf0Object(2) { type = Amf0Object.Amf0Type.AMF0_NULL });
				kevans.Add(new Amf0Object(1) { type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY, data = in1 });
				List<Amf0Object> importantpart = new List<Amf0Object>(3);
				importantpart.Add(new Amf0Object(0) { type = Amf0Object.Amf0Type.AMF0_NULL });
				importantpart.Add(new Amf0Object(1) { type = Amf0Object.Amf0Type.AMF0_NULL });
				importantpart.Add(new Amf0Object(2) { type = Amf0Object.Amf0Type.AMF0_NULL });
				importantpart.Add(new Amf0Object(3) { type = Amf0Object.Amf0Type.AMF0_NULL });
				importantpart.Add(new Amf0Object(4) { type = Amf0Object.Amf0Type.AMF0_NULL });
				importantpart.Add(new Amf0Object(5, true));
				kevans.Add(new Amf0Object(2) { type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY, data = importantpart});
				trackdata.Add(new Amf0Object("trackData") { data = kevans, type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY });
			}
		}
		/*
        private sol_object WriteLine(Line l, int num, string name)
        {
            var stl = l as StandardLine;
            sol_object ret = new sol_object();
            var data = new List<sol_object>();
            ret.data = data;
            ret.type = 8;
            ret.name = name;
            var objects = new object[] { l.Position.X, l.Position.Y,
                l.Position2.X, l.Position2.Y,
                0,
                stl == null ? 0.0 : (double)Convert.ToDouble(stl.inv),//[5]
                stl?.Prev?.ID,//[6]
                stl?.Next?.ID,//[7]
                num,
                (int)TrackLoader.LineTypeForSOL(l.GetLineType()) };
			objects[4] = stl == null ? 0 : (int)stl.Extension;
            int c = 0;

            foreach (object o in objects)
            {
                sol_object obj = new sol_object();
                obj.data = o;
                obj.name = c.ToString(Program.Culture);
                if (o == null)
                {
                    obj.type = 5;
                    obj.data=null;
                }
                else
                {
                    var type = o.GetType();
                    if (type == typeof(double))
                    {
                        obj.type = 0;
                    }
                    else if (type == typeof(int))
                    {
                        obj.type = 0;
                        obj.data = Convert.ToDouble(obj.data);
                    }
                    else if (type == typeof(bool))
                    {
                        obj.type = 0;
                        obj.data = Convert.ToDouble(obj.data);
                    }
                    else if (o is Nullable<int>)
                    {
                        switch ((o as Nullable<int>).HasValue)
                        {
                            case false:
                                obj.type = 5;
                                obj.data = null;
                                break;
                            case true:
                                obj.type = 0;
                                obj.data = Convert.ToDouble(obj.data);
                                break;
                        }
                    }
                }
                data.Add(obj);
                c++;
            }
            return ret;
        }*/
		/*
        private List<sol_object> VectorObject(Vector2d vec, string name, string name2)
        {
            sol_object ret = new sol_object();
            ret.name = name;
            ret.data = vec.X;
            ret.type = 0;
            sol_object ret2 = new sol_object();
            ret2.name = name2;
            ret2.data = vec.Y;
            ret2.type = 0;
            return new List<sol_object>() { ret, ret2 };
        }
        void WriteObject(BigEndianWriter bw, sol_object obj)
        {
            bw.WriteMapleString(obj.name);
            if (obj.data == null)
            {
                bw.WriteByte(5);
            }
            if (obj.data is List<sol_object>)
            {
                if (obj.type == 8)
                {
                    bw.WriteByte(obj.type);
                    var list = obj.data as List<sol_object>;
                    bw.WriteInt(list.Count);
                    int counter =0;
                    foreach (var lobj in list)
                    {
                        if (lobj.name == null)
                        {
                        lobj.name = counter++.ToString(Program.Culture);
                        }
                        WriteObject(bw, lobj);
                    }
                    bw.WriteShort(0);
                    bw.WriteByte(9);
                }
                else//OBJECT
                {
                    bw.WriteByte(3);
                    obj.type = 3;
                    var list = obj.data as List<sol_object>;
                    int counter = 0;
                    foreach (var lobj in list)
                    {
                        if (lobj.name == null)
                        {
                            lobj.name = counter++.ToString(Program.Culture);
                        }
                        WriteObject(bw, lobj);
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
                obj.type = 0;
                bw.WriteByte(obj.type);
                bw.WriteDouble((double)obj.data);
            }
            else if (obj.data is string)
            {
                obj.type = 2;
                bw.WriteByte(obj.type);
                bw.WriteMapleString((string)obj.data);
            }
            else if (obj.data is bool)
            {
                bw.WriteByte(1);
                bw.WriteByte(Convert.ToByte(obj.data));
            }
        //    objectcache.Add(obj);
        }*/
	}

	/*
    public class sol_object
    {
        public string name;
        public byte type;
        public object data;

        public override string ToString()
        {
            return name + ":" + data.ToString();
        }

        public object get_property(string name)
        {
            if (data is List<sol_object>)
            {
                var l = data as List<sol_object>;
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].name == name)
                        return l[i].data;
                }
            }
            throw new Exception("No property of the name " + name + " was found.");
        }
    }*/
}