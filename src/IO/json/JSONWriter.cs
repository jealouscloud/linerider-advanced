using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Utf8Json;
using System.Diagnostics;
using linerider.Game;
using linerider.IO.json;

namespace linerider.IO
{
    public static class JSONWriter
    {
        public static string SaveTrack(Track trk, string savename)
        {
            var sw = Stopwatch.StartNew();
            Track ret = new Track();
            track_json trackobj = new track_json();
            trackobj.label = trk.Name;
            trackobj.startPosition = new track_json.point_json()
            {
                x = trk.StartOffset.X,
                y = trk.StartOffset.Y
            };
            int ver = trk.GetVersion();
            switch (ver)
            {
                case 61:
                    trackobj.version = "6.1";
                    break;
                case 62:
                    trackobj.version = "6.2";
                    break;
            }
            var sort = trk.GetSortedLines();
            trackobj.linesArray = new object[sort.Length][];
            int idx = 0;
            foreach (var line in sort)
            {
                line_json jline = new line_json();
                switch (line.Type)
                {
                    case LineType.Blue:
                        jline.type = 0;
                        break;
                    case LineType.Red:
                        jline.type = 1;
                        break;
                    case LineType.Scenery:
                        jline.type = 2;
                        break;
                }
                jline.id = line.ID;
                jline.x1 = line.Position.X;
                jline.y1 = line.Position.Y;
                jline.x2 = line.Position2.X;
                jline.y2 = line.Position2.Y;
                if (line is StandardLine stl)
                {
                    if (stl.Extension.HasFlag(StandardLine.Ext.Left))
                        jline.leftExtended = true;
                    if (stl.Extension.HasFlag(StandardLine.Ext.Right))
                        jline.rightExtended = true;
                    jline.extended = (int)stl.Extension;
                    jline.flipped = stl.inv;
                }
                trackobj.linesArray[idx++] = line_to_linearrayline(jline);
            }
            var dir = TrackIO.GetTrackDirectory(trk);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var filename = dir + savename + ".track.json";
            using (var file = File.Create(filename))
            {
                var bytes = JsonSerializer.Serialize<track_json>(trackobj);
                file.Write(bytes, 0, bytes.Length);
            }
            return filename;
        }
        private static object[] line_to_linearrayline(line_json line)
        {
            //['type', 'id', 'x1', 'y1', 'x2', 'y2', 'extended', 'flipped', 'leftLine', 'rightLine']
            if (line.type != 2)
                return new object[] { line.type, line.id, line.x1, line.y1, line.x2, line.y2, line.extended, line.flipped };
            //scenery
            return new object[] { line.type, line.id, line.x1, line.y1, line.x2, line.y2 };
        }
    }
}
