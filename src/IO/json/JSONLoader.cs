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
                    trackobj = JsonSerializer.Deserialize<track_json>(file);
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
            if (trackobj.startZoom != 0)
            {
                ret.StartZoom = (float)MathHelper.Clamp(
                    trackobj.startZoom,
                    Utils.Constants.MinimumZoom,
                    Utils.Constants.MaxZoom);
            }
            ret.ZeroStart = trackobj.zeroStart;
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
            if (trackobj.triggers != null)
            {
                foreach (var trigger in trackobj.triggers)
                {
                    if (ret.LineLookup.TryGetValue(trigger.ID, out GameLine line))
                    {
                        if (line is StandardLine stl)
                        {
                            if (trigger.zoom)
                            {
                                stl.Trigger = new LineTrigger()
                                {
                                    ZoomTrigger = trigger.zoom,
                                    ZoomTarget = (float)MathHelper.Clamp(
                                        trigger.target,
                                        Utils.Constants.MinimumZoom,
                                        Utils.Constants.MaxZoom),
                                    ZoomFrames = trigger.frames
                                };
                            }
                        }
                    }
                }
            }
            return ret;
        }
        private static void ReadLinesArray(Track track, object[][] parser)
        {
            //['type', 'id', 'x1', 'y1', 'x2', 'y2', 'extended', 'flipped', 'leftLine', 'rightLine', 'multiplier']
            //ignore leftLine, rightLine
            foreach (var lineobj in parser)
            {
                line_json line = new line_json();
                int idx = 0;
                line.type = Convert.ToInt32(lineobj[idx++]);
                line.id = Convert.ToInt32(lineobj[idx++]);
                line.x1 = Convert.ToDouble(lineobj[idx++]);
                line.y1 = Convert.ToDouble(lineobj[idx++]);
                line.x2 = Convert.ToDouble(lineobj[idx++]);
                line.y2 = Convert.ToDouble(lineobj[idx++]);
                var sz = lineobj.Length;
                if (line.type != 2 && idx < sz)//non scenery
                {
                    line.extended = Convert.ToInt32(lineobj[idx++]);
                    if (idx < sz)
                    {
                        line.flipped = Convert.ToBoolean(lineobj[idx++]);
                        idx += 2;//skip leftline, rightline
                        if (line.type == 1 && idx < sz)
                        {
                            line.multiplier = Convert.ToInt32(lineobj[idx++]);
                        }
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
                        if (line.multiplier > 1)
                        {
                            add.Multiplier = line.multiplier;
                        }
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
