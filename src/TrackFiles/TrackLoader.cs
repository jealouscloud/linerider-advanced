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
using linerider.Lines;

namespace linerider
{
    internal class TrackLoader : GameService
    {
        public class TrackLoadException : Exception
        {
            public TrackLoadException(string message) : base(message)
            {
            }
        }
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
            new SOL(Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar + trk.Name + "savedLines.sol", trk);
        }
        public static Dictionary<string, bool> TrackFeatures(Track trk)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
            if (trk.ZeroStart)
            {
                ret["ZEROSTART"] = true;
            }
            foreach (GameLine l in trk.LineLookup.Values)
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

            if (trk.GetVersion() == 61)
                ret["SIX_ONE"] = true;
            return ret;
        }
        /// Checks a relative filename for validity
        public static bool CheckValidFilename(string relativefilename)
        {
            if (Path.GetFileName(relativefilename) != relativefilename ||
                Path.IsPathRooted(relativefilename) ||
                relativefilename.Length == 0)
                return false;
            try
            {
                //the ctor checks validity pretty well
                //it also does not have the requirement of the file existing
                var info = new FileInfo(relativefilename);
                var attr = info.Attributes;
                if (attr != (FileAttributes)(-1) &&
                attr.HasFlag(FileAttributes.Directory))
                    throw new ArgumentException();

            }
            catch
            {
                return false;
            }
            var invalidchars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < relativefilename.Length; i++)
            {
                if (invalidchars.Contains(relativefilename[i]))
                {
                    return false;
                }
            }

            return true;
        }
        private static string GetTrackDirectory(Track track)
        {
            return Program.UserDirectory + "Tracks" + Path.DirectorySeparatorChar
             + track.Name + Path.DirectorySeparatorChar;
        }
        public static void SaveTrackToFile(Track track, string savename, string songdata = null)
        {
            var dir = GetTrackDirectory(track);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var trackfiles =
                TrackLoader.EnumerateTRKFiles(dir);
            int saveindex = 0;
            for (var i = 0; i < trackfiles.Length; i++)
            {
                var s = Path.GetFileNameWithoutExtension(trackfiles[i]);
                var index = s.IndexOf(" ", StringComparison.Ordinal);
                if (index != -1)
                {
                    s = s.Remove(index);
                }
                if (int.TryParse(s, out saveindex))
                {
                    break;
                }
            }
            saveindex++;
            SaveTrackTrk(track, saveindex + " " + savename, songdata);
        }
        private static bool TryMoveAndReplaceFile(string fname, string fname2)
        {
            if (File.Exists(fname))
            {
                try
                {
                    if (File.Exists(fname2))
                    {
                        File.Delete(fname2);
                    }
                    File.Move(fname, fname2);
                    return true;
                }
                catch
                {
                    //failed
                }
            }
            return false;
        }
        public static void CreateAutosave(Track track, string songdata = null)
        {
            var dir = GetTrackDirectory(track);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var sn1 = "autosave00";
            var sn2 = "autosave01";
            TryMoveAndReplaceFile(dir + sn1 + ".trk", dir + sn2 + ".trk");
            SaveTrackTrk(track, sn1, songdata);
        }
        private static string SaveTrackTrk(Track trk, string savename, string songdata = null)
        {
            var dir = GetTrackDirectory(trk);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var filename = dir + savename + ".trk";
            using (var file = File.Create(filename))
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
                var lines = trk.GetSortedLines();
                foreach (GameLine l in lines)
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

                if (trk.GetVersion() == 61)
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
                bw.Write(trk.StartOffset.X);
                bw.Write(trk.StartOffset.Y);
                bw.Write(lines.Length);
                foreach (var line in lines)
                {
                    byte type = (byte)line.Type;
                    if (line is StandardLine l)
                    {
                        if (l.inv)
                            type |= 1 << 7;
                        var ext = (byte)l.Extension;
                        type |= (byte)((ext & 0x03) << 5); //bits: 2
                        bw.Write(type);
                        if (saved_features[REDMULTIPLIER_INDEX])
                        {
                            if (line is RedLine red)
                            {
                                bw.Write((byte)red.Multiplier);
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
                            if (line is SceneryLine scenery)
                            {
                                byte b = (byte)(Math.Round(scenery.Width, 1) * 10);
                                bw.Write(b);
                            }
                        }
                    }

                    bw.Write(line.Position.X);
                    bw.Write(line.Position.Y);
                    bw.Write(line.Position2.X);
                    bw.Write(line.Position2.Y);
                }
            }
            return filename;
        }
        public static void CreateTestFromTrack(Track track)
        {
            track.Reset();
            int framecount = 40 * 60 * 5;
            for (int i = 0; i < framecount; i++)
            {
                track.AddFrame();
            }
            var filename = SaveTrackTrk(track, track.Name + ".test");
            if (System.IO.File.Exists(filename + ".result"))
                System.IO.File.Delete(filename + ".result");
            using (var f = System.IO.File.Create(filename + ".result"))
            {
                var bw = new BinaryWriter(f);
                bw.Write((int)framecount);
                var state = track.RiderStates[track.RiderStates.Count - 1];
                for (int i = 0; i < state.Body.Length; i++)
                {
                    bw.Write(state.Body[i].Location.X);
                    bw.Write(state.Body[i].Location.Y);
                }
            }
        }
        public static bool TestCompare(Track track, string dir)
        {
            var testfile = dir + track.Name + ".test.trk.result";
            if (!File.Exists(testfile))
            {
                return false;
            }
            using (var file =
                    File.Open(testfile, FileMode.Open))
            {
                var br = new BinaryReader(file);
                var frame = br.ReadInt32();
                track.Reset();
                for (int i = 0; i < frame; i++)
                {
                    track.AddFrame();
                }
                //track.Chunks.fg.PrintMetrics();
                var state = track.RiderStates[track.RiderStates.Count - 1];
                for (int i = 0; i < state.Body.Length; i++)
                {
                    var x = br.ReadDouble();
                    var y = br.ReadDouble();
                    var riderx = state.Body[i].Location.X;
                    var ridery = state.Body[i].Location.Y;
                    if (x != riderx || y != ridery)
                        return false;
                }
            }
            return true;
        }
        public static Track LoadTrackTRK(string trackfile, string trackname)
        {
            var ret = new Track();
            ret.Name = trackname;
            var addedlines = new Dictionary<int, StandardLine>();
            var extensions = new List<Extensionentry>();
            var location = trackfile;
            var bytes = File.ReadAllBytes(location);
            using (var file =
                    new MemoryStream(bytes))
            {
                var br = new BinaryReader(file);
                int magic = br.ReadInt32();
                if (magic == ('T' | 'R' << 8 | 'K' << 16 | 0xF2 << 24))
                {
                    byte version = br.ReadByte();
                    string[] features = Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt16())).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (version != 1)
                        throw new TrackLoadException("Unsupported version");
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
                                throw new TrackLoadException("Unsupported feature");
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
                                throw new TrackLoadException("Invalid line type at ID " +ID);
                        }
                        if (l is StandardLine)
                        {
                            if (!addedlines.ContainsKey(l.ID))
                            {
                                addedlines[ID] = (StandardLine)l;
                                ret.AddLine(l, true);
                            }
                        }
                        else
                        {
                            ret.AddLine(l, true);
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
                    throw new TrackLoadException("Unsupported Linetype");
            }
        }
        public static Track LoadTrack(sol_track trackdata)
        {
            var ret = new Track { Name = trackdata.name };
            List<GameLine> lineslist = new List<GameLine>();
            var buffer = (List<Amf0Object>)trackdata.get_property("data");
            var addedlines = new Dictionary<int, StandardLine>();
            var version = trackdata.data.First(x => x.name == "version").data as string;

            if (version == "6.1")
            {
                ret.SetVersion(61);
            }
            else
            {
                ret.SetVersion(62);
            }
            try
            {
                var options = (List<Amf0Object>)trackdata.get_property("trackData");
                if (options.Count >= 2)
                {
                    try
                    {
                        ret.ZeroStart = (bool)options.Find(x => x.name == "2").get_property("5");
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
                                extensions.Add(new Extensionentry { Line = l, Linkid = prev, Next = false });
                            }
                            if (line[7].data != null)
                            {
                                var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
                                extensions.Add(new Extensionentry { Line = l, Linkid = next, Next = true });
                            }
                            if (!addedlines.ContainsKey(l.ID))
                            {
                                lineslist.Add(l);
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
                                extensions.Add(new Extensionentry { Line = l, Linkid = prev, Next = false });
                            }
                            if (line[7].data != null)
                            {
                                var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
                                extensions.Add(new Extensionentry { Line = l, Linkid = next, Next = true });
                            }
                            if (!addedlines.ContainsKey(l.ID))
                            {
                                lineslist.Add(l);
                                addedlines[l.ID] = l;
                            }
                        }
                        break;

                    case 2:
                        lineslist.Add(
                            new SceneryLine(
                                new Vector2d(Convert.ToDouble(line[0].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[1].data, CultureInfo.InvariantCulture)),
                                new Vector2d(Convert.ToDouble(line[2].data, CultureInfo.InvariantCulture),
                                    Convert.ToDouble(line[3].data, CultureInfo.InvariantCulture))));
                        break;

                    default:
                        throw new TrackLoadException("Unknown line type");
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
                startline.Add(new Amf0Object { data = lineslist[conv].Position.X });
                startline.Add(new Amf0Object { data = lineslist[conv].Position.Y - 50 * 0.5 });
            }
            ret.StartOffset = new Vector2d(
                Convert.ToDouble(startline[0].data, CultureInfo.InvariantCulture),
                Convert.ToDouble(startline[1].data, CultureInfo.InvariantCulture));
            foreach (var line in lineslist)
            {
                ret.AddLine(line, true);
            }
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