using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;
using linerider.Game;
using linerider.IO.SOL;
namespace linerider.IO
{
    public static class SOLLoader
    {
        public static List<sol_track> LoadSol(string sol_location)
        {
            var sol = new SOLFile(sol_location);
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
        public static Track LoadTrack(sol_track trackdata)
        {
            var ret = new Track { Name = trackdata.name, Filename = trackdata.filename };
            var buffer = (List<Amf0Object>)trackdata.get_property("data");
            List<GameLine> lineslist = new List<GameLine>(buffer.Count);
            var addedlines = new Dictionary<int, StandardLine>(buffer.Count);
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
                            l.Extension = (StandardLine.Ext)(
                                Convert.ToInt32(
                                    line[4].data,
                                    CultureInfo.InvariantCulture));
                            if (line[6].data != null)
                            {
                                var prev = Convert.ToInt32(line[6].data, CultureInfo.InvariantCulture);
                            }
                            if (line[7].data != null)
                            {
                                var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
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
                            l.Extension = (StandardLine.Ext)(
                                Convert.ToInt32(
                                    line[4].data,
                                    CultureInfo.InvariantCulture));
                            if (line[6].data != null)
                            {
                                var prev = Convert.ToInt32(line[6].data, CultureInfo.InvariantCulture);
                            }
                            if (line[7].data != null)
                            {
                                var next = Convert.ToInt32(line[7].data, CultureInfo.InvariantCulture);
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
                        throw new TrackIO.TrackLoadException("Unknown line type");
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
                ret.AddLine(line);
            }
            return ret;
        }
    }
}
