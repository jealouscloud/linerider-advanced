using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;
using linerider.Lines;
using linerider.IO.SOL;

namespace linerider.IO
{
    public class SOLWriter
    {

        public static int LineTypeForSOL(LineType t)
        {
            switch (t)
            {
                case LineType.Blue:
                    return 0;
                case LineType.Red:
                    return 1;
                case LineType.Scenery:
                    return 2;
                default:
                    throw new TrackIO.TrackLoadException("Unsupported Linetype");
            }
        }
        public static void SaveTrack(Track track)
        {
            var location = Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar + track.Name + "savedLines.sol";
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
            WriteTrack(tracks, track);
            Amf0 amf = new Amf0(bw);
            amf.WriteAmf0Object(rootobj);
            bw.WriteByte(0);
            bw.Reset(2);
            bw.WriteInt(bw.Length - 6);
            File.WriteAllBytes(location, bw.ToArray());
        }
        private static void WriteTrack(List<Amf0Object> parent, Track trk)
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
                linedata.Add(new Amf0Object(6, 0));//stl?.Prev?.ID
                linedata.Add(new Amf0Object(7, 0));//tl?.Next?.ID
                linedata.Add(new Amf0Object(8, list.Keys[i]));
                linedata.Add(new Amf0Object(9, LineTypeForSOL(line.Type)));

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
