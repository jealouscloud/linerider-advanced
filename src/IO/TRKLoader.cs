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
                string[] features = Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16())).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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
                                Settings.Local.CurrentSong = new Song(Path.GetFileName(fn), float.Parse(strings[1]));
                                Settings.Local.EnableSong = true;
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
                    LineTrigger tr = ignorabletrigger ? new LineTrigger() : null;
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
                    switch (lt)
                    {
                        case LineType.Blue:
                            var bl = new StandardLine(new Vector2d(x1, y1), new Vector2d(x2, y2), inv);
                            bl.ID = ID;
                            bl.Extension = (StandardLine.Ext)lim;
                            l = bl;
                            bl.Trigger = tr;
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
                            rl.Trigger = tr;
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
            }
            return ret;
        }
    }
}
