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
using linerider.Lines;
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

            SortedList<int, GameLine> list = new SortedList<int, GameLine>();
            var lines = trk.GetSortedLines();
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var id = lines[i].ID;
                if (id < 0)
                {
                    id = Math.Abs(id) + trk._idcounter + 100;
                }
                list.Add(id, lines[i]);
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
                linedata.Add(new Amf0Object(9, TrackLoader.LineTypeForSOL(line.Type)));

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
                kevans.Add(new Amf0Object(2) { type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY, data = importantpart });
                trackdata.Add(new Amf0Object("trackData") { data = kevans, type = Amf0Object.Amf0Type.AMF0_ECMA_ARRAY });
            }
        }
    }
}