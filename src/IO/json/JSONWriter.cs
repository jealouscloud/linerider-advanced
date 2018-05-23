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
            track_json trackobj = new track_json();
            trackobj.label = trk.Name;
            trackobj.startPosition = new track_json.point_json()
            {
                x = trk.StartOffset.X,
                y = trk.StartOffset.Y
            };
            trackobj.startZoom = trk.StartZoom;
            trackobj.zeroStart = trk.ZeroStart;
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
            trackobj.triggers = new List<track_json.zoomtrigger_json>();
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
                    if (line is RedLine red)
                    {
                        jline.multiplier = red.Multiplier;
                    }
                    if (stl.Trigger != null && stl.Trigger.ZoomTrigger)
                    {
                        trackobj.triggers.Add(new track_json.zoomtrigger_json()
                        {
                            zoom = true,
                            ID = jline.id,
                            target = stl.Trigger.ZoomTarget,
                            frames = stl.Trigger.ZoomFrames
                        });
                    }
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
            //['type', 'id', 'x1', 'y1', 'x2', 'y2', 'extended', 'flipped', 'leftLine', 'rightLine', 'multiplier']
            List<object> ret = new List<object>(11) { line.type, line.id, line.x1, line.y1, line.x2, line.y2 };

            if (line.type != 2)
            {
                ret.Add(line.extended);
                ret.Add(line.flipped);
                if (line.multiplier > 1)
                {
                    ret.Add(-1);
                    ret.Add(-1);
                    ret.Add(line.multiplier);
                }
            }
            return ret.ToArray();
        }
    }
}
