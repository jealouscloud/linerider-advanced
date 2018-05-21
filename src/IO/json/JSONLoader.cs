using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using linerider.Game;
using linerider.IO.json;
using Utf8Json;
namespace linerider.IO
{
    public static class JSONLoader
    {
        public static Track LoadTrack(string trackfile)
        {
            Track ret = new Track();
            ret.Filename = trackfile;
            track_json trackobj;
            using (var file = File.OpenRead(trackfile))
            {
                try
                {
                    var task = JsonSerializer.DeserializeAsync<track_json>(file);
                    task.Wait();
                    trackobj = task.Result;
                    if (task.Exception != null)
                        throw task.Exception;
                }
                catch (Exception ex)
                {
                    throw new TrackIO.TrackLoadException(
                        "Json parsing error.\n" + ex.Message);
                }
            }
            switch (trackobj.version)
            {
                case "6.1":
                    ret.SetVersion(61);
                    break;
                case "6.2":
                    ret.SetVersion(62);
                    break;
                default:
                    throw new TrackIO.TrackLoadException(
                        "Unsupported physics.");
            }
            ret.Name = trackobj.label;
            if (trackobj.startPosition != null)
            {
                ret.StartOffset = new Vector2d(
                    trackobj.startPosition.x, trackobj.startPosition.y);
            }
            if (!string.IsNullOrEmpty(trackobj.linesArrayCompressed))
            {
                var json2 = LZString.decompressBase64(trackobj.linesArrayCompressed);
                trackobj.linesArray = JsonSerializer.Deserialize<object[][]>(json2);
                trackobj.linesArrayCompressed = null;
            }
            if (trackobj.linesArray?.Length > 0)
            {
                ReadLinesArray(ret, trackobj.linesArray);
            }
            else if (trackobj.lines != null)
            {
                foreach (var line in trackobj.lines)
                {
                    AddLine(ret, line);
                }
            }
            return ret;
        }
        private static void ReadLinesArray(Track track, object[][] parser)
        {
            //['type', 'id', 'x1', 'y1', 'x2', 'y2', 'extended', 'flipped', 'leftLine', 'rightLine']
            //ignore leftLine, rightLine
            foreach (var lineobj in parser)
            {
                line_json line = new line_json();
                line.type = Convert.ToInt32(lineobj[0]);
                line.id = Convert.ToInt32(lineobj[1]);
                line.x1 = Convert.ToDouble(lineobj[2]);
                line.y1 = Convert.ToDouble(lineobj[3]);
                line.x2 = Convert.ToDouble(lineobj[4]);
                line.y2 = Convert.ToDouble(lineobj[5]);
                var sz = lineobj.Length;
                if (6 < sz)
                {
                    line.extended = Convert.ToInt32(lineobj[6]);
                    if (7 < sz)
                    {
                        line.flipped = Convert.ToBoolean(lineobj[7]);
                    }
                }
                AddLine(track, line);
            }
        }
        private static void AddLine(Track track, line_json line)
        {
            switch (line.type)
            {
                case 0:
                    {
                        var add = new StandardLine(
                                new Vector2d(line.x1, line.y1),
                                new Vector2d(line.x2, line.y2),
                                Convert.ToBoolean(line.flipped));
                        add.ID = line.id;
                        add.Extension = (StandardLine.Ext)line.extended;
                        if (Convert.ToBoolean(line.leftExtended))
                            add.Extension |= StandardLine.Ext.Left;
                        if (Convert.ToBoolean(line.rightExtended))
                            add.Extension |= StandardLine.Ext.Right;
                        track.AddLine(add);
                        break;
                    }
                case 1:
                    {
                        var add = new RedLine(
                                new Vector2d(line.x1, line.y1),
                                new Vector2d(line.x2, line.y2),
                                Convert.ToBoolean(line.flipped));
                        add.ID = line.id;
                        add.Extension = (StandardLine.Ext)line.extended;
                        if (Convert.ToBoolean(line.leftExtended))
                            add.Extension |= StandardLine.Ext.Left;
                        if (Convert.ToBoolean(line.rightExtended))
                            add.Extension |= StandardLine.Ext.Right;
                        track.AddLine(add);
                        break;
                    }
                case 2:
                    {
                        var add = new SceneryLine(
                                new Vector2d(line.x1, line.y1),
                                new Vector2d(line.x2, line.y2));
                        add.ID = line.id;
                        track.AddLine(add);
                        break;
                    }
                default:
                    throw new TrackIO.TrackLoadException(
                        "Unknown line type");
            }
        }
    }
}
