//
//  Rider.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace linerider
{
    public class Rider
    {
        #region Fields

        public bool Crashed;
        public List<Rider> iterations = null;
        public DynamicObject[] ModelAnchors;
        public DynamicLine[] ModelLines;
        public DynamicObject[] ScarfAnchors;
        public DynamicLine[] ScarfLines;
        public short[] Endpoints;
        public bool SledBroken;

        #endregion Fields

        #region Constructors

        public Rider()
        {
            Reset(new Vector2d(0, 0), null);
        }

        #endregion Constructors

        #region Methods

        public Rider Clone()
        {
            Rider ret = new Rider(this);
            return ret;
        }

        public Rider Lerp(Rider rider2, float percent)
        {
            Rider ret = new Rider(this);
            for (int i = 0; i < ret.ModelAnchors.Length; i++)
            {
                ret.ModelAnchors[i].Position = Vector2d.Lerp(ret.ModelAnchors[i].Position, rider2.ModelAnchors[i].Position, percent);
                ret.ModelAnchors[i].Prev = Vector2d.Lerp(ret.ModelAnchors[i].Prev, rider2.ModelAnchors[i].Prev, percent);
            }
            for (int i = 0; i < ret.ScarfAnchors.Length; i++)
            {
                ret.ScarfAnchors[i].Position = Vector2d.Lerp(ret.ScarfAnchors[i].Position, rider2.ScarfAnchors[i].Position, percent);
                ret.ScarfAnchors[i].Prev = Vector2d.Lerp(ret.ScarfAnchors[i].Prev, rider2.ScarfAnchors[i].Prev, percent);
            }
            return ret;
        }

        public System.Drawing.Point GetAnchorOffset(int anchorID)
        {
            return new System.Drawing.Point(Endpoints[anchorID] >> 8, Endpoints[anchorID] & 0xFF);
        }

        public void SetAnchorOffset(int anchorID, System.Drawing.Point p)
        {
            Endpoints[anchorID] = (short)(((byte)p.X << 8) | (byte)p.Y);
        }

        public void Reset(Vector2d offset, Track trk)
        {
            Crashed = false;
            SledBroken = false;
            ModelAnchors = new[]
            {
                new DynamicObject(new Vector2d(0, 0), 0.8),
                new DynamicObject(new Vector2d(0, 10), 0),
                new DynamicObject(new Vector2d(30, 10), 0),
                new DynamicObject(new Vector2d(35, 0), 0),
                new DynamicObject(new Vector2d(10, 0), 0.8),
                new DynamicObject(new Vector2d(10, -11), 0.8),
                new DynamicObject(new Vector2d(23, -10), 0.1),
                new DynamicObject(new Vector2d(23, -10), 0.1),
                new DynamicObject(new Vector2d(20, 10), 0),
                new DynamicObject(new Vector2d(20, 10), 0)
            };
            Endpoints = new short[ModelAnchors.Length];
            var scarf1 = new List<ScarfObject>(new[]
            {
                new ScarfObject(new Vector2d(7, -10.0), 0.9),
                new ScarfObject(new Vector2d(3, -10.0), 0.9),
                new ScarfObject(new Vector2d(0, -10), 0.9),
                new ScarfObject(new Vector2d(-4.0, -10), 0.9),
                new ScarfObject(new Vector2d(-7, -10), 0.9),
                new ScarfObject(new Vector2d(-11, -10), 0.9),
            });
            var scarfs = new List<ScarfObject>(scarf1.ToArray());
            ScarfAnchors = scarfs.ToArray();

            for (var i = 0; i < ModelAnchors.Length; i++)
            {
                ModelAnchors[i].Position *= 0.5;
            }

            for (var i = 0; i < ScarfAnchors.Length; i++)
            {
                ScarfAnchors[i].Position *= 0.5;
            }
            double momentum = (trk == null || !trk.ZeroStart) ? 0.8 * 0.5 : 0;
            for (var i = 0; i < ModelAnchors.Length; i++)
            {
                ModelAnchors[i].Position += offset;
                ModelAnchors[i].Prev = ModelAnchors[i].Position - new Vector2d(momentum, 0);
            }
            for (var i = 0; i < ScarfAnchors.Length; i++)
            {
                ScarfAnchors[i].Position += offset;
                ScarfAnchors[i].Prev = ScarfAnchors[i].Position;
                ScarfAnchors[i].Prev = ScarfAnchors[i].Position - new Vector2d(momentum, 0);
            }
            CreateLines();
        }

        public void SatisfyBoundaries(Track track, ConcurrentDictionary<int, StandardLine> collisions)
        {
            int index = 0;
            foreach (DynamicObject anchor in ModelAnchors)
            {
                var ax = (int)Math.Floor(anchor.Position.X / 14);
                var ay = (int)Math.Floor(anchor.Position.Y / 14);
                System.Drawing.Point anchoroffset = new System.Drawing.Point(0, 0);

                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var gr = track.Chunks.GetChunk(ax + x, ay + y);
                        if (gr != null)
                        {
                            var values = gr.Values;
                            foreach (Line l in values)
                            {
                                if (l.Interact(anchor))
                                {
                                    if (collisions != null)
                                    {
                                        collisions[l.ID] = (StandardLine)l;
                                    }
                                }
                                var dx1 = ax - (int)Math.Floor(anchor.Position.X / 14);
                                var dy1 = ay - (int)Math.Floor(anchor.Position.Y / 14);
                                if (dx1 != 0)
                                {
                                    if ((dx1 > 0 && anchoroffset.X < dx1) || (dx1 < 0 && anchoroffset.X > dx1))
                                        anchoroffset.X = dx1;
                                }
                                if (dy1 != 0)
                                {
                                    if ((dy1 > 0 && anchoroffset.Y < dy1) || (dy1 < 0 && anchoroffset.Y > dy1))
                                        anchoroffset.Y = dy1;
                                }
                            }
                        }
                    }
                }
                SetAnchorOffset(index, anchoroffset);
                index++;
            }
        }
        public bool GwellSatisfyBoundaries(Track track, int anchorid)
        {
            int index = 0;
            foreach (DynamicObject anchor in ModelAnchors)
            {
                var ax = (int)Math.Floor(anchor.Position.X / 14);
                var ay = (int)Math.Floor(anchor.Position.Y / 14);

                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var gr = track.Chunks.GetChunk(ax + x, ay + y);
                        if (gr != null)
                        {
                            var values = gr.Values;
                            foreach (Line l in values)
                            {
                                if (l.Interact(anchor))
                                {
                                    if (index == anchorid)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
                index++;
            }
            return false;
        }


        public void SatisfyDistance()
        {
            for (var i = 0; i < ModelLines.Length; i++)
            {
                ModelLines[i].satisfyDistance(this);
            }
        }

        public void SatisfyScarf()
        {
            foreach (var l in ScarfLines)
            {
                l.satisfyDistance(this);
            }
        }

        public void TickMomentum()
        {
            for (var i = 0; i < ModelAnchors.Length; i++)
            {
                ModelAnchors[i].Tick();
            }
            var ppfvector = ModelAnchors[5].Momentum + ModelAnchors[6].Momentum;
            var pixels = Math.Round(Math.Abs(((ppfvector.X + ppfvector.Y) / 4)), 2);
            var val = Math.Min(pixels * 0.1, 4);
            Random r = new Random((int)(pixels * 100));
            if (r.Next((int)val, 5) == 4)
            {
                bool left = r.Next(0, 2) == 1;//flutter left or right
                val = Math.Min(val / 40, 0.015);
                for (int i = 0; i < ScarfAnchors.Length; i++)
                {
                    var add = (ScarfAnchors[i].Prev - ScarfAnchors[i].Position).PerpendicularLeft * val *
                              r.NextDouble();
                    if (!left)
                        ScarfAnchors[i].Position += add;
                    else
                        ScarfAnchors[i].Position -= add;
                }
            }
            for (var i = 0; i < ScarfAnchors.Length; i++)
            {
                ScarfAnchors[i].Tick();
            }
        }

        #endregion Methods

        protected Rider(Rider r)
        {
            Crashed = r.Crashed;
            SledBroken = r.SledBroken;
            ModelAnchors = new DynamicObject[r.ModelAnchors.Length];
            Endpoints = new short[ModelAnchors.Length];
            for (int i = 0; i < ModelAnchors.Length; i++)
            {
                ModelAnchors[i] = r.ModelAnchors[i].Clone();
            }
            ScarfAnchors = new DynamicObject[r.ScarfAnchors.Length];
            for (int i = 0; i < ScarfAnchors.Length; i++)
            {
                ScarfAnchors[i] = r.ScarfAnchors[i].Clone();
            }
            CreateLines();

            for (int i = 0; i < ModelLines.Length; i++)
            {
                ModelLines[i].restLength = r.ModelLines[i].restLength;
                if (ModelLines[i] is BindLine)
                {
                    (ModelLines[i] as BindLine).UpdateEndurance();
                }
            }
            if (iterations == null)
                iterations = new List<Rider>();
            if (r.iterations != null)
            {
                for (int i = 0; i < r.iterations.Count; i++)
                {
                    iterations.Add(r.iterations[i].Clone());
                }
            }
        }

        private void CreateLines()
        {
            ModelLines = new[]
            {
                new DynamicLine(ModelAnchors[0], ModelAnchors[1]),
                new DynamicLine(ModelAnchors[1], ModelAnchors[2]),
                new DynamicLine(ModelAnchors[2], ModelAnchors[3]),
                new DynamicLine(ModelAnchors[3], ModelAnchors[0]),
                new DynamicLine(ModelAnchors[0], ModelAnchors[2]),
                new DynamicLine(ModelAnchors[3], ModelAnchors[1]),
                new BindLine(ModelAnchors[0], ModelAnchors[4]),
                new BindLine(ModelAnchors[1], ModelAnchors[4]),
                new BindLine(ModelAnchors[2], ModelAnchors[4]),
                new DynamicLine(ModelAnchors[5], ModelAnchors[4]),
                new DynamicLine(ModelAnchors[5], ModelAnchors[6]),
                new DynamicLine(ModelAnchors[5], ModelAnchors[7]),
                new DynamicLine(ModelAnchors[4], ModelAnchors[8]),
                new DynamicLine(ModelAnchors[4], ModelAnchors[9]),
                new DynamicLine(ModelAnchors[5], ModelAnchors[7]),
                new BindLine(ModelAnchors[5], ModelAnchors[0]),
                new BindLine(ModelAnchors[3], ModelAnchors[6]),
                new BindLine(ModelAnchors[3], ModelAnchors[7]),
                new BindLine(ModelAnchors[8], ModelAnchors[2]),
                new BindLine(ModelAnchors[9], ModelAnchors[2]),
                new RepelLine(ModelAnchors[5], ModelAnchors[8]),
                new RepelLine(ModelAnchors[5], ModelAnchors[9])
            };
            ScarfLines = new DynamicLine[]
            {
                    new ScarfLine(ModelAnchors[5], ScarfAnchors[0]),
                    new ScarfLine(ScarfAnchors[0], ScarfAnchors[1]),
                    new ScarfLine(ScarfAnchors[1], ScarfAnchors[2]),
                    new ScarfLine(ScarfAnchors[2], ScarfAnchors[3]),
                    new ScarfLine(ScarfAnchors[3], ScarfAnchors[4]),
                    new ScarfLine(ScarfAnchors[4], ScarfAnchors[5])
            };
        }
    }
}