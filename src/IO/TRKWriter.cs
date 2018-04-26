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
        public static string SaveTrack(Track trk, string savename)
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
                string featurestring = "";
                var lines = trk.GetLines();
                var featurelist = TrackIO.GetTrackFeatures(trk);
                featurelist.TryGetValue(TrackFeatures.songinfo, out bool songinfo);
                featurelist.TryGetValue(TrackFeatures.redmultiplier, out bool redmultiplier);
                featurelist.TryGetValue(TrackFeatures.zerostart, out bool zerostart);
                featurelist.TryGetValue(TrackFeatures.scenerywidth, out bool scenerywidth);
                featurelist.TryGetValue(TrackFeatures.six_one, out bool six_one);
                featurelist.TryGetValue(TrackFeatures.ignorable_trigger, out bool ignorable_trigger);
                foreach (var feature in featurelist)
                {
                    if (feature.Value)
                    {
                        featurestring += feature.Key + ";";
                    }
                }
                bw.Write((short)featurestring.Length);
                bw.Write(Encoding.ASCII.GetBytes(featurestring));
                if (songinfo)
                {
                    bw.Write(trk.Song.ToString());
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
                        if (redmultiplier)
                        {
                            if (line is RedLine red)
                            {
                                bw.Write((byte)red.Multiplier);
                            }
                        }
                        if (ignorable_trigger)
                        {
                            if (l.Trigger != null)
                            {
                                if (l.Trigger.ZoomTrigger) // check other triggers here for at least one
                                {
                                    bw.Write(l.Trigger.ZoomTrigger);
                                    if (l.Trigger.ZoomTrigger)
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
                        if (scenerywidth)
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
