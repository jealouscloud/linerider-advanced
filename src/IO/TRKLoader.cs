using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;
using linerider.Game;
using System.Diagnostics;

namespace linerider.IO
{
    public static class TRKLoader
    {
        private static string[] supported_features = {
    "REDMULTIPLIER",
    "SCENERYWIDTH",
    "6.1","SONGINFO",
    "IGNORABLE_TRIGGER",
    "ZEROSTART",
        };

        private const int REDMULTIPLIER_INDEX = 0;
        private const int SCENERYWIDTH_INDEX = 1;
        private const int SIX_ONE_INDEX = 2;
        private const int SONGINFO_INDEX = 3;
        private const int IGNORABLE_TRIGGER_INDEX = 4;
        private const int ZEROSTART_INDEX = 5;
        private static float ParseFloat(string f)
        {
            if (!float.TryParse(
                f,
                NumberStyles.Float,
                Program.Culture,
                out float ret))
                throw new TrackIO.TrackLoadException(
                    "Unable to parse string into float");
            return ret;
        }
        private static int ParseInt(string f)
        {
            if (!int.TryParse(
                f,
                NumberStyles.Float,
                Program.Culture,
                out int ret))
                throw new TrackIO.TrackLoadException(
                    "Unable to parse string into int");
            return ret;
        }
        private static void ParseMetadata(Track ret, BinaryReader br)
        {
            var count = br.ReadInt16();
            for (int i = 0; i < count; i++)
            {
                var metadata = ReadString(br).Split('=');
                switch (metadata[0])
                {
                    case TrackMetadata.startzoom:
                        ret.StartZoom = ParseFloat(metadata[1]);
                        break;
                    case TrackMetadata.triggers:
                        string[] triggers = metadata[1].Split('&');
                        foreach (var t in triggers)
                        {
                            string[] tdata = t.Split(':');
                            TriggerType ttype;
                            try
                            {
                                ttype = (TriggerType)int.Parse(tdata[0]);
                            }
                            catch
                            {
                                throw new TrackIO.TrackLoadException(
                                    "Unsupported trigger type");
                            }
                            GameTrigger newtrigger;
                            switch (ttype)
                            {
                                case TriggerType.Zoom:
                                    var target = ParseFloat(tdata[1]);
                                    var start = ParseInt(tdata[2]);
                                    var end = ParseInt(tdata[3]);
                                    newtrigger = new GameTrigger()
                                    {
                                        Start = start,
                                        End = end,
                                        TriggerType = TriggerType.Zoom,
                                        ZoomTarget = target,
                                    };
                                    break;
                                default:
                                    throw new TrackIO.TrackLoadException(
                                        "Unsupported trigger type");
                            }
                            ret.Triggers.Add(newtrigger);
                        }
                        break;
                }
            }
        }
        public static Track LoadTrack(string trackfile, string trackname)
        {
            var ret = new Track();
            ret.Filename = trackfile;
            ret.Name = trackname;
            var addedlines = new Dictionary<int, StandardLine>();
            var location = trackfile;
            var bytes = File.ReadAllBytes(location);
            using (var file =
                    new MemoryStream(bytes))
            {
                var br = new BinaryReader(file);
                int magic = br.ReadInt32();
                if (magic != ('T' | 'R' << 8 | 'K' << 16 | 0xF2 << 24))
                    throw new TrackIO.TrackLoadException("File was read as .trk but it is not valid");
                byte version = br.ReadByte();
                string[] features = ReadString(br).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (version != 1)
                    throw new TrackIO.TrackLoadException("Unsupported version");
                bool redmultipier = false;
                bool scenerywidth = false;
                bool supports61 = false;
                bool songinfo = false;
                bool ignorabletrigger = false;
                for (int i = 0; i < features.Length; i++)
                {
                    switch (features[i])
                    {
                        case TrackFeatures.redmultiplier:
                            redmultipier = true;
                            break;

                        case TrackFeatures.scenerywidth:
                            scenerywidth = true;
                            break;

                        case TrackFeatures.six_one:
                            supports61 = true;
                            break;

                        case TrackFeatures.songinfo:
                            songinfo = true;
                            break;

                        case TrackFeatures.ignorable_trigger:
                            ignorabletrigger = true;
                            break;

                        case TrackFeatures.zerostart:
                            ret.ZeroStart = true;
                            break;
                        default:
                            throw new TrackIO.TrackLoadException("Unsupported feature");
                    }
                }
                if (supports61)
                {
                    ret.SetVersion(61);
                }
                else
                {
                    ret.SetVersion(62);
                }
                if (songinfo)
                {
                    var song = br.ReadString();
                    try
                    {
                        var strings = song.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        var fn = Program.UserDirectory + "Songs" +
                                 Path.DirectorySeparatorChar +
                                 strings[0];
                        if (File.Exists(fn))
                        {
                            if (AudioService.LoadFile(ref fn))
                            {
                                ret.Song = new Song(Path.GetFileName(fn), float.Parse(strings[1], Program.Culture));
                            }
                            else
                            {
                                Program.NonFatalError("An unknown error occured trying to load the song file");
                            }
                        }

                    }
                    catch
                    {
                        // ignored
                    }
                }
                ret.StartOffset = new Vector2d(br.ReadDouble(), br.ReadDouble());
                var lines = br.ReadInt32();
                List<LineTrigger> linetriggers = new List<LineTrigger>();
                for (var i = 0; i < lines; i++)
                {
                    GameLine l;
                    byte ltype = br.ReadByte();
                    var lt = (LineType)(ltype & 0x1F);//we get 5 bits
                    var inv = (ltype >> 7) != 0;
                    var lim = (ltype >> 5) & 0x3;
                    var ID = -1;
                    var prvID = -1;
                    var nxtID = -1;
                    var multiplier = 1;
                    var linewidth = 1f;
                    LineTrigger tr = null;
                    if (redmultipier)
                    {
                        if (lt == LineType.Red)
                        {
                            multiplier = br.ReadByte();
                        }
                    }
                    if (lt == LineType.Blue || lt == LineType.Red)
                    {
                        if (ignorabletrigger)
                        {
                            bool zoomtrigger = br.ReadBoolean();
                            if (zoomtrigger)
                            {
                                tr = new LineTrigger();
                                tr.ZoomTrigger = true;
                                var target = br.ReadSingle();
                                var frames = br.ReadInt16();
                                tr.ZoomFrames = frames;
                                tr.ZoomTarget = target;
                            }
                            else
                            {
                                tr = null;
                            }
                        }
                        ID = br.ReadInt32();
                        if (lim != 0)
                        {
                            prvID = br.ReadInt32();//ignored
                            nxtID = br.ReadInt32();//ignored
                        }
                    }
                    if (lt == LineType.Scenery)
                    {
                        if (scenerywidth)
                        {
                            float b = br.ReadByte();
                            linewidth = b / 10f;
                        }
                    }
                    var x1 = br.ReadDouble();
                    var y1 = br.ReadDouble();
                    var x2 = br.ReadDouble();
                    var y2 = br.ReadDouble();

                    if (tr != null)
                    {
                        tr.LineID = ID;
                        linetriggers.Add(tr);
                    }
                    switch (lt)
                    {
                        case LineType.Blue:
                            var bl = new StandardLine(new Vector2d(x1, y1), new Vector2d(x2, y2), inv);
                            bl.ID = ID;
                            bl.Extension = (StandardLine.Ext)lim;
                            l = bl;
                            break;

                        case LineType.Red:
                            var rl = new RedLine(new Vector2d(x1, y1), new Vector2d(x2, y2), inv);
                            rl.ID = ID;
                            rl.Extension = (StandardLine.Ext)lim;
                            if (redmultipier)
                            {
                                rl.Multiplier = multiplier;
                            }
                            l = rl;
                            break;

                        case LineType.Scenery:
                            l = new SceneryLine(new Vector2d(x1, y1), new Vector2d(x2, y2)) { Width = linewidth };

                            break;

                        default:
                            throw new TrackIO.TrackLoadException("Invalid line type at ID " + ID);
                    }
                    if (l is StandardLine)
                    {
                        if (!addedlines.ContainsKey(l.ID))
                        {
                            addedlines[ID] = (StandardLine)l;
                            ret.AddLine(l);
                        }
                    }
                    else
                    {
                        ret.AddLine(l);
                    }
                }
                ret.Triggers = TriggerConverter.ConvertTriggers(linetriggers, ret);
                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    var meta = br.ReadInt32();
                    if (meta == ('M' | 'E' << 8 | 'T' << 16 | 'A' << 24))
                    {
                        ParseMetadata(ret, br);
                    }
                    else
                    {
                        throw new TrackIO.TrackLoadException("Expected metadata tag but got " + meta.ToString("X8"));
                    }
                }
            }
            return ret;
        }
        private static string ReadString(BinaryReader br)
        {
            return Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16()));
        }
    }
}
