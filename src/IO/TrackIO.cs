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
        public static string[] EnumerateSolFiles(string folder)
        {
            var ret = Directory.GetFiles(folder, "*.*")
                .Where(x =>
                    (x.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))).ToArray();
            Array.Sort(ret, StringComparer.CurrentCultureIgnoreCase);
            return ret;
        }
        public static Dictionary<string, bool> GetTrackFeatures(Track trk)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
            if (trk.ZeroStart)
            {
                ret[TrackFeatures.zerostart] = true;
            }
            foreach (GameLine l in trk.LineLookup.Values)
            {
                var scenery = l as SceneryLine;
                if (scenery != null)
                {
                    if (Math.Abs(scenery.Width - 1) > 0.0001f)
                    {
                        ret[TrackFeatures.scenerywidth] = true;
                    }
                }
                var red = l as RedLine;
                if (red != null)
                {
                    if (red.Multiplier != 1)
                    {
                        ret[TrackFeatures.redmultiplier] = true;
                    }
                }
                var stl = l as StandardLine;
                if (stl != null)
                {
                    if (stl.Trigger != null)
                    {
                        ret[TrackFeatures.ignorable_trigger] = true;
                    }
                }
            }

            if (trk.GetVersion() == 61)
                ret[TrackFeatures.six_one] = true;
            if (!string.IsNullOrEmpty(trk.Song.Location) && trk.Song.Enabled)
            {
                ret[TrackFeatures.songinfo] = true;
            }
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
        public static string ExtractSaveName(string filepath)
        {
            var filename = Path.GetFileName(filepath);
            var index = filename.IndexOf(" ", StringComparison.Ordinal);
            if (index != -1)
            {
                var id = filename.Remove(index);
                if (int.TryParse(id, out int pt))
                {
                    filename = filename.Remove(0, index + 1);
                }
            }
            var ext = filename.IndexOf(".trk", StringComparison.OrdinalIgnoreCase);
            if (ext != -1)
            {
                filename = filename.Remove(ext);
            }
            return filename;
        }
        public static bool QuickSave(Track track)
        {
            if (track.Name == Constants.DefaultTrackName)
                return false;
            var dir = GetTrackDirectory(track);
            if (track.Filename != null)
            {
                // if we loaded this file from /Tracks and not 
                // /Tracks/{trackname}/file.trk then it doesnt have a folder
                // the user will have to decide one. we will not quicksave it.
                if (!track.Filename.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    return false;

            }
            if (Directory.Exists(dir))
            {
                var files = EnumerateTrackFiles(dir);
                if (files.Length > 0)
                {
                    if (ExtractSaveName(files[0]) == "quicksave")
                    {
                        TryMoveAndReplaceFile(files[0], dir + "quicksave_old.trk");
                    }
                }
                try
                {
                    SaveTrackToFile(track, "quicksave");
                }
                catch (Exception e)
                {
                    Program.NonFatalError("An error occured during quicksave" +
                    Environment.NewLine +
                    e.Message);
                }
                return true;
            }
            return false;
        }
        public static string SaveToSOL(Track track, string savename)
        {
            int saveindex = GetSaveIndex(track);
            var filename = SOLWriter.SaveTrack(track, saveindex + " " + savename);
            track.Filename = filename;
            return filename;
        }
        public static string SaveTrackToFile(Track track, string savename)
        {
            int saveindex = GetSaveIndex(track);
            var filename = TRKWriter.SaveTrack(track, saveindex + " " + savename);
            track.Filename = filename;
            return filename;
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
        public static void CreateAutosave(Track track)
        {
            var dir = GetTrackDirectory(track);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var sn1 = "autosave00";
            var sn2 = "autosave01";
            TryMoveAndReplaceFile(dir + sn1 + ".trk", dir + sn2 + ".trk");
            TRKWriter.SaveTrack(track, sn1);
        }
        public static void CreateTestFromTrack(Track track)
        {
            var timeline = new linerider.Game.Timeline(
                track);
            timeline.Restart(track.GetStart(), 1);
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
                    track);
                timeline.Restart(track.GetStart(), 1);
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

        private static int GetSaveIndex(Track track)
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
            return saveindex;
        }
    }
}