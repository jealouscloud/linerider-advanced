using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;
using linerider.Game;

namespace linerider.IO
{
    public static class TRKWriter
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

        public static string SaveTrack(Track trk, string savename, string songdata = null)
        {
            var dir = TrackIO.GetTrackDirectory(trk);
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
                var lines = trk.GetLines();
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
                        if (l.Extension != StandardLine.Ext.None)
                        {
                            // this was extension writing
                            // but we no longer support this.
                            bw.Write(-1);
                            bw.Write(-1);
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
    }
}
