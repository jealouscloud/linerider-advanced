//
//  TrackLoader.cs
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

using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;

namespace linerider
{
    internal class TrackLoader : GameService
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

        private struct Extensionentry
        {
            public int Linkid;
            public StandardLine Line;
            public bool Next;
        }

        public static List<sol_track> LoadSol(string sol_location)
        {
            var sol = new SOL(sol_location);
			var tracks = (List<Amf0Object>)sol.RootObject.get_property("trackList");
            var ret = new List<sol_track>();
            for (var i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].data is List<Amf0Object>)
                {
                    var add = new sol_track { data = (List<Amf0Object>)tracks[i].data, filename = sol_location };
                    add.name = (string)add.get_property("label");
                    ret.Add(add);
                }
            }
            return ret;
        }

        public static string[] EnumerateTRKFiles(string folder)
        {
            return Directory.GetFiles(folder, "*.*")
                .Where(x =>
                    x.EndsWith(".trk", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x =>
                {
                    var fn = Path.GetFileName(x);
                    var index = fn.IndexOf(" ", StringComparison.Ordinal);
                    if (index != -1)
                    {
                        int pt;
                        if (int.TryParse(fn.Remove(index), out pt))
                            return pt;
                    }
                    return 0;
                }).ToArray();
        }
        public static void SaveTrackSol(Track trk)
        {
            new SOL(Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar + "savedLines.sol", trk);
        }
        public static Dictionary<string, bool> TrackFeatures(Track trk)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
            if (trk.ZeroStart)
            {
                ret["ZEROSTART"] = true;
            }
            foreach (Line l in trk.Lines)
            {
                var scenery = l as SceneryLine;
                if (scenery != null)
                {
                    if (Math.Abs(scenery.Width - 1) > 0.0001f)
                    {
                        ret["SCENERYWIDTH"] = true;
                    }
                }
                var red = l as RedLine;
                if (red != null)
                {
                    if (red.Multiplier != 1)
                    {
                        ret["REDMULTIPLIER"] = true;
                    }
                }
                var stl = l as StandardLine;
                if (stl != null)
                {
                    if (stl.Trigger != null)
                    {
                        ret["IGNORABLE_TRIGGER"] = true;
                    }
                }
            }

            if (trk.GetVersion() == 6.1m)
                ret["SIX_ONE"] = true;
            return ret;
        }
        public static void SaveTrackTrk(Track trk, string savename, string songdata = null)
        {
            var dir = Program.CurrentDirectory + "Tracks" + Path.DirectorySeparatorChar + trk.Name;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using (var file = File.Create(dir + Path.DirectorySeparatorChar + savename + ".trk"))
            {
                var bw = new BinaryWriter(file);
                bw.Write(new byte[] { (byte)'T', (byte)'R', (byte)'K', 0xF2 });
                bw.Write((byte)1);
                string features = "";
                bool[] saved_features = new bool[]
                    {
                        false,
                        false,
                        false,
                        false,
                        false,
                        false
                };
                if (songdata != null)
                {
                    saved_features[SONGINFO_INDEX] = true;
                }
                if (trk.ZeroStart)
                {
                    saved_features[ZEROSTART_INDEX] = true;
                }
                foreach (Line l in trk.Lines)
                {
                    var scenery = l as SceneryLine;
                    if (scenery != null)
                    {
                        if (Math.Abs(scenery.Width - 1) > 0.0001f)
                        {
                            saved_features[SCENERYWIDTH_INDEX] = true;
                        }
                    }
                    var red = l as RedLine;
                    if (red != null)
                    {
                        if (red.Multiplier != 1)
                        {
                            saved_features[REDMULTIPLIER_INDEX] = true;
                        }
                    }
                    var stl = l as StandardLine;
                    if (stl != null)
                    {
                        if (stl.Trigger != null)
                        {
                            saved_features[IGNORABLE_TRIGGER_INDEX] = true;
                        }
                    }
                }

                if (trk.GetVersion() == 6.1m)
                    saved_features[SIX_ONE_INDEX] = true;
                for (int i = 0; i < supported_features.Length; i++)
                {
                    if (saved_features[i])
                    {
                        features += supported_features[i] + ";";
                    }
                }
                bw.Write((short)features.Length);
                bw.Write(Encoding.ASCII.GetBytes(features));
                if (saved_features[SONGINFO_INDEX])
                {
                    bw.Write(songdata);
                }
                bw.Write(trk.Start.X);
                bw.Write(trk.Start.Y);
                bw.Write(trk.Lines.Count);
                for (var i = 0; i < trk.Lines.Count; i++)
                {
                    byte type = (byte)trk.Lines[i].GetLineType();
                    if (trk.Lines[i] is StandardLine)
                    {
                        var l = ((StandardLine)trk.Lines[i]);
                        if (l.inv)
                            type |= 1 << 7;
                        type |= (byte)((((byte)((StandardLine)trk.Lines[i]).Extension)) << 5); //bits: 2

                        bw.Write(type);
                        if (saved_features[REDMULTIPLIER_INDEX])
                        {
                            if (trk.Lines[i] is RedLine)
                            {
                                bw.Write((byte)(trk.Lines[i] as RedLine).Multiplier);
                            }
                        }
                        if (saved_features[IGNORABLE_TRIGGER_INDEX])
                        {
                            if (l.Trigger != null)
                            {
                                if (l.Trigger.Zoomtrigger) // check other triggers here for at least one
                                {
                                    bw.Write(l.Trigger.Zoomtrigger);
                                    if (l.Trigger.Zoomtrigger)
                                    {
                                        bw.Write(l.Trigger.ZoomTarget);
                                        bw.Write((short)l.Trigger.ZoomFrames);
                                    }
                                }
                                else
                                {
                                    bw.Write(false);
                                }
                            }
                            else
                            {
                                bw.Write(false);//zoomtrigger=false
                            }
                        }
                        bw.Write(l.ID);
                        if (l.Extension != StandardLine.ExtensionDirection.None)
                        {
                            //bugfix for accidental saving of incorrect next / prev
                            if (l.Extension == StandardLine.ExtensionDirection.Both)
                            {
                                bw.Write(l.Next == null ? -1 : l.Next.ID);
                                bw.Write(l.Prev == null ? -1 : l.Prev.ID);
                            }
                            else if (l.Extension == StandardLine.ExtensionDirection.Left)
                            {
                                bw.Write(-1);
                                bw.Write(l.Prev == null ? -1 : l.Prev.ID);
                            }
                            else if (l.Extension == StandardLine.ExtensionDirection.Right)
                            {
                                bw.Write(l.Next == null ? -1 : l.Next.ID);
                                bw.Write(-1);
                            }
                        }
                    }
                    else
                    {
                        bw.Write(type);
                        if (saved_features[SCENERYWIDTH_INDEX])
                        {
                            var scenery = trk.Lines[i] as SceneryLine;
                            if (scenery != null)
                            {
                                byte b = (byte)(Math.Round(scenery.Width, 1) * 10);
                                bw.Write(b);
                            }
                        }
                    }

                    bw.Write(trk.Lines[i].Position.X);
                    bw.Write(trk.Lines[i].Position.Y);
                    bw.Write(trk.Lines[i].Position2.X);
                    bw.Write(trk.Lines[i].Position2.Y);
                }
            }
        }

        public static Track LoadTrackTRK(string trackfile, string trackname)
        {
            var ret = new Track();
            ret.Name = trackname;
            var addedlines = new Dictionary<int, StandardLine>();
            var extensions = new List<Extensionentry>();
            var location = trackfile;
            using (var file =
                    File.Open(location, FileMode.Open))
            {
                var br = new BinaryReader(file);
                int magic = br.ReadInt32();
                if (magic == ('T' | 'R' << 8 | 'K' << 16 | 0xF2 << 24))
                {
                    byte version = br.ReadByte();
                    string[] features = Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16())).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (version != 1)
                        throw new Exception("Unsupported version");
                    bool redmultipier = false;
                    bool scenerywidth = false;
                    bool supports61 = false;
                    bool songinfo = false;
                    bool ignorabletrigger = false;
                    for (int i = 0; i < features.Length; i++)
                    {
                        switch (features[i])
                        {
                            case "REDMULTIPLIER":
                                redmultipier = true;
                                break;

                            case "SCENERYWIDTH":
                                scenerywidth = true;
                                break;

                            case "6.1":
                                supports61 = true;
                                break;

                            case "SONGINFO":
                                songinfo = true;
                                break;

                            case "IGNORABLE_TRIGGER":
                                ignorabletrigger = true;
                                break;

                            case "ZEROSTART":
                                ret.ZeroStart = true;
                                break;
                            default:
                                throw new Exception("Unsupported feature");
                        }
                    }
                    if (supports61)
                    {
                        ret.SetVersion(6.1m);
                    }
                    else
                    {
                        ret.SetVersion(6.2m);
                    }
                    if (songinfo)
                    {
                        var song = br.ReadString();
                        try
                        {
                            var strings = song.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries); 
                            var fn = Program.CurrentDirectory + "Songs" +
									 Path.DirectorySeparatorChar +
									 strings[0];
							if (File.Exists(fn))
							{
								if (AudioPlayback.LoadFile(ref fn))
								{
									game.CurrentSong = new Song(Path.GetFileName(fn),float.Parse(strings[1]));
									game.EnableSong = true;
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
                    ret.Start = new Vector2d(br.ReadDouble(), br.ReadDouble());
                    var lines = br.ReadInt32();
                    for (var i = 0; i < lines; i++)
                    {
                        Line l;
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
                                    tr.Zoomtrigger = true;
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
                                prvID = br.ReadInt32();
                                nxtID = br.ReadInt32();
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
                                bl.SetExtension(lim);
                                l = bl;
                                if (prvID != -1)
                                    extensions.Add(new Extensionentry { Line = bl, Linkid = prvID, Next = false });
                                if (nxtID != -1)
                                    extensions.Add(new Extensionentry { Line = bl, Linkid = nxtID, Next = true });
                                bl.Trigger = tr;
                                break;

                            case LineType.Red:
                                var rl = new RedLine(new Vector2d(x1, y1), new Vector2d(x2, y2), inv);
                                rl.ID = ID;
                                rl.SetExtension(lim);
                                if (redmultipier)
                                {
                                    rl.Multiplier = multiplier;
                                }
                                l = rl;
                                if (prvID != -1)
                                    extensions.Add(new Extensionentry { Line = rl, Linkid = prvID, Next = false });
                                if (nxtID != -1)
                                    extensions.Add(new Extensionentry { Line = rl, Linkid = nxtID, Next = true });
                                rl.Trigger = tr;
                                break;

                            case LineType.Scenery:
                                l = new SceneryLine(new Vector2d(x1, y1), new Vector2d(x2, y2)) { Width = linewidth };

                                break;

                            default:
                                throw new Exception("Invalid line type");
                        }
                        if (l is StandardLine)
                        {
                            if (!addedlines.ContainsKey(l.ID))
                            {
                                addedlines[ID] = (StandardLine)l;
                                ret.AddLines(l);
                            }
                        }
                        else
                        {
                            ret.AddLines(l);
                        }
                    }
                }
            }
            foreach (var v in extensions)
            {
                if (v.Next)
                {
                    StandardLine sl;
                    if (addedlines.TryGetValue(v.Linkid, out sl))
                    {
                        //if (sl.Extension == StandardLine.ExtensionDirection.Right || sl.Extension == StandardLine.ExtensionDirection.Both)
                        {
                            v.Line.Next = sl;
                            sl.Prev = v.Line;
                        }
                    }
                }
                else //prev
                {
                    StandardLine sl;
                    if (addedlines.TryGetValue(v.Linkid, out sl))
                    {
                        //if (sl.Extension == StandardLine.ExtensionDirection.Left || sl.Extension == StandardLine.ExtensionDirection.Both)
                        {
                            v.Line.Prev = sl;
                            sl.Next = v.Line;
                        }
                    }
                }
            }
            ret.ResetUndo();
            ret.ResetChanges();
            return ret;
        }
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
                    throw new Exception("Unsupported Linetype");
            }
        }
        public static Track LoadTrack(sol_track trackdata)
        {
            var ret = new Track { Name = trackdata.name };
            var buffer = (List<Amf0Object>)trackdata.get_property("data");
            var addedlines = new Dictionary<int, StandardLine>();
            var version = trackdata.data.First(x => x.name == "version").data as string;

            if (version == "6.1")
            {
                ret.SetVersion(6.1m);
            }
            else
            {
                ret.SetVersion(6.2m);
            }
            try
            {
                var options = (List<Amf0Object>) trackdata.get_property("trackData");
                if (options.Count >= 2)
                {
                    try
                    {
                        ret.ZeroStart = (bool) options.Find(x => x.name == "2").get_property("5");
                    }
                    catch
                    {
                        //ignored
                    }
                }
            }
            catch
            {
                //ignored
            }
            var extensions = new List<Extensionentry>();
            for (var i = buffer.Count - 1; i >= 0; --i)
            {
                var line = (List<Amf0Object>)buffer[i].data;
                var type = Convert.ToInt32(line[9].data, CultureInfo.InvariantCulture);
                switch (type)
                {
                    case 0:
                    {
                        var l =
                            new StandardLine(
                                new Vector2d(Convert.ToDouble(line[0].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[1].data, CultureInfo.InvariantCulture)),
                                new Vector2d(Convert.ToDouble(line[2].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[3].data, CultureInfo.InvariantCulture)),
                                Convert.ToBoolean(line[5].data, CultureInfo.InvariantCulture))
                            {
                                ID = Convert.ToInt32(line[8].data, CultureInfo.InvariantCulture)
                            };
                        l.SetExtension(Convert.ToInt32(line[4].data, CultureInfo.InvariantCulture));
                        if (line[6].data != null)
                        {
                            var prev = Convert.ToInt32(line[6].data, CultureInfo.InvariantCulture);
                            extensions.Add(new Extensionentry {Line = l, Linkid = prev, Next = false});
                        }
                        if (line[7].data != null)
                        {
                            var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
                            extensions.Add(new Extensionentry {Line = l, Linkid = next, Next = true});
                        }
                        if (!addedlines.ContainsKey(l.ID))
                        {
                            ret.AddLines(l);
                            addedlines[l.ID] = l;
                        }
                    }
                        break;

                    case 1:
                    {
                        var l =
                            new RedLine(
                                new Vector2d(Convert.ToDouble(line[0].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[1].data, CultureInfo.InvariantCulture)),
                                new Vector2d(Convert.ToDouble(line[2].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[3].data, CultureInfo.InvariantCulture)),
                                Convert.ToBoolean(line[5].data, CultureInfo.InvariantCulture))
                            {
                                ID = Convert.ToInt32(line[8].data, CultureInfo.InvariantCulture)
                            };
                        l.SetExtension(Convert.ToInt32(line[4].data, CultureInfo.InvariantCulture));
                        if (line[6].data != null)
                        {
                            var prev = Convert.ToInt32(line[6].data, CultureInfo.InvariantCulture);
                            extensions.Add(new Extensionentry {Line = l, Linkid = prev, Next = false});
                        }
                        if (line[7].data != null)
                        {
                            var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
                            extensions.Add(new Extensionentry {Line = l, Linkid = next, Next = true});
                        }
                        if (!addedlines.ContainsKey(l.ID))
                        {
                            ret.AddLines(l);
                            addedlines[l.ID] = l;
                        }
                    }
                        break;

                    case 2:
                        ret.AddLines(
                            new SceneryLine(
                                new Vector2d(Convert.ToDouble(line[0].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[1].data, CultureInfo.InvariantCulture)),
                                new Vector2d(Convert.ToDouble(line[2].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[3].data, CultureInfo.InvariantCulture))));
                        break;

                    default:
                        throw new Exception("Unknown line type");
                }
            }
            foreach (var v in extensions)
            {
                if (v.Next)
                {
                    StandardLine sl;
                    if (addedlines.TryGetValue(v.Linkid, out sl))
                    {
                        v.Line.Next = sl;
                        sl.Prev = v.Line;
                    }
                }
                else //prev
                {
                    StandardLine sl;
                    if (addedlines.TryGetValue(v.Linkid, out sl))
                    {
                        v.Line.Prev = sl;
                        sl.Next = v.Line;
                    }
                }
            }
            var startlineprop = trackdata.get_property("startLine");
            var startline = startlineprop as List<Amf0Object>;
            if (startline == null && startlineprop is double)
            {
                var conv = Convert.ToInt32(startlineprop, CultureInfo.InvariantCulture);
                if (conv >= ret.Lines.Count || conv < 0)
                {
                    startline = new List<Amf0Object>();
                    startline.Add(new Amf0Object { data = 100 });
                    startline.Add(new Amf0Object { data = 100 });
                }
            }
            else if (startlineprop is double)
            {
                var conv = Convert.ToInt32(startlineprop, CultureInfo.InvariantCulture);
                startline = new List<Amf0Object>();
                startline.Add(new Amf0Object { data = ret.Lines[conv].Position.X });
                startline.Add(new Amf0Object { data = ret.Lines[conv].Position.Y - 50 * 0.5 });
            }
            ret.Start.X = Convert.ToDouble(startline[0].data, CultureInfo.InvariantCulture);
            ret.Start.Y = Convert.ToDouble(startline[1].data, CultureInfo.InvariantCulture);
            ret.ResetUndo();
            ret.ResetChanges();
            return ret;
        }
    }

    class sol_track
    {
        public string filename;
        public string name;
        public List<Amf0Object> data;

        public override string ToString()
        {
            return name;
        }

        public object get_property(string name)
        {
            for (var i = 0; i < data.Count; i++)
            {
                if (data[i].name == name)
                    return data[i].data;
            }
            throw new Exception("No property of the name " + name + " was found.");
        }
    }
}