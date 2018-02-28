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
using linerider.Utils;
using linerider.Game;

namespace linerider.IO
{
    public class TrackIO : GameService
    {
        public class TrackLoadException : Exception
        {
            public TrackLoadException(string message) : base(message)
            {
            }
        }


        public static string[] EnumerateTrackFiles(string folder)
        {
            return Directory.GetFiles(folder, "*.*")
                .Where(x =>
                    (x.EndsWith(".trk", StringComparison.OrdinalIgnoreCase))).
                    OrderByDescending(x =>
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
        public static string GetTrackName(string trkfile)
        {
            string trackname = Path.GetFileNameWithoutExtension(trkfile);
            var dirname = Path.GetDirectoryName(trkfile);
            var dirs = Directory.GetDirectories(Constants.TracksDirectory);
            foreach (var dir in dirs)
            {
                if (string.Equals(
                    dirname,
                    dir,
                    StringComparison.InvariantCulture))
                {
                    trackname = Path.GetFileName(dirname);
                    break;
                }
            }
            return trackname;
        }
        public static string GetTrackDirectory(Track track)
        {
            return Utils.Constants.TracksDirectory +
            track.Name +
            Path.DirectorySeparatorChar;
        }
        public static string SaveTrackToFile(Track track, string savename, string songdata = null)
        {
            var dir = GetTrackDirectory(track);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var trackfiles =
                TrackIO.EnumerateTrackFiles(dir);
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
          //  return JSONWriter.SaveTrack(track,saveindex + " " + savename);
           return TRKWriter.SaveTrack(track, saveindex + " " + savename, songdata);
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
            TRKWriter.SaveTrack(track, sn1, songdata);
        }
        public static void CreateTestFromTrack(Track track)
        {
            var timeline = new linerider.Game.Timeline(
                track,
                new List<LineTrigger>());
            timeline.Restart(track.GetStart());
            int framecount = 40 * 60 * 5;
            
            var filename = TRKWriter.SaveTrack(track, track.Name + ".test");
            if (System.IO.File.Exists(filename + ".result"))
                System.IO.File.Delete(filename + ".result");
            using (var f = System.IO.File.Create(filename + ".result"))
            {
                var bw = new BinaryWriter(f);
                bw.Write((int)framecount);
                var state = timeline.GetFrame(framecount);
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
                var timeline = new linerider.Game.Timeline(
                    track,
                    new List<LineTrigger>());
                timeline.Restart(track.GetStart());
                //track.Chunks.fg.PrintMetrics();
                var state = timeline.GetFrame(frame);
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

    }
}