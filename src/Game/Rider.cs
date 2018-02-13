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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using linerider.Lines;
using linerider.Utils;

namespace linerider.Game
{

    public struct Rider
    {
        /// <summary>
        /// Represents the bounds of the grid cells used to simulate this rider state.
        /// </summary>
        public readonly RectLRTB PhysicsBounds;
        public readonly ImmutablePointCollection Body;
        public readonly ImmutablePointCollection Scarf;
        public readonly bool Crashed;
        public readonly bool SledBroken;
        private Rider(SimulationPoint[] body, SimulationPoint[] scarf, RectLRTB physbounds, bool dead, bool sledbroken)
        {
            Body = new ImmutablePointCollection(body);
            Scarf = new ImmutablePointCollection(scarf);
            Crashed = dead;
            SledBroken = sledbroken;
            PhysicsBounds = physbounds;
        }
        public static Rider Create(Vector2d start, Vector2d momentum)
        {
            var joints = new SimulationPoint[RiderConstants.DefaultRider.Length];
            var scarf = new SimulationPoint[RiderConstants.DefaultScarf.Length + 1];
            RectLRTB pbounds = new RectLRTB();
            for (int i = 0; i < joints.Length; i++)
            {
                var coord = (RiderConstants.DefaultRider[i] + start);
                var prev = coord - momentum;
                switch (i)
                {
                    case RiderConstants.SledTL:
                    case RiderConstants.BodyButt:
                    case RiderConstants.BodyShoulder:
                        joints[i] = new SimulationPoint(coord, prev, Vector2d.Zero, 0.8);
                        break;
                    case RiderConstants.BodyHandLeft:
                    case RiderConstants.BodyHandRight:
                        joints[i] = new SimulationPoint(coord, prev, Vector2d.Zero, 0.1);
                        break;
                    default:
                        joints[i] = new SimulationPoint(coord, prev, Vector2d.Zero, 0.0);
                        break;
                }
                if (i == 0)
                {
                    pbounds = new RectLRTB(joints[0]);
                }
                else
                {
                    var cellx = (int)Math.Floor(joints[i].Location.X / 14);
                    var celly = (int)Math.Floor(joints[i].Location.Y / 14);

                    pbounds.left = Math.Min(cellx - 1, pbounds.left);
                    pbounds.top = Math.Min(celly - 1, pbounds.top);
                    pbounds.right = Math.Max(cellx + 1, pbounds.right);
                    pbounds.bottom = Math.Max(celly + 1, pbounds.bottom);
                }
            }
            scarf[0] = joints[RiderConstants.BodyShoulder];
            for (int i = 0; i < RiderConstants.DefaultScarf.Length; i++)
            {
                var pos = scarf[0].Location + RiderConstants.DefaultScarf[i];
                scarf[i + 1] = new SimulationPoint(pos, pos, Vector2d.Zero, 0.9);
            }
            return new Rider(joints, scarf, pbounds, false, false);
        }


        public Vector2d CalculateCenter()
        {
            if (Crashed)
                return Body[4].Location;
            var anchorsaverage = new Vector2d();
            for (int i = 0; i < Body.Length; i++)
            {
                anchorsaverage += Body[i].Location;
            }
            return anchorsaverage / Body.Length;
        }

        public Vector2d CalculateMomentum()
        {
            var mo = Vector2d.Zero;
            for (int i = 0; i < Body.Length; i++)
            {
                mo += Body[i].Momentum;
            }
            mo /= Body.Length;
            return mo;
        }
        public static Rider Lerp(Rider r1, Rider r2, float percent)
        {
            SimulationPoint[] joints = new SimulationPoint[r1.Body.Length];
            SimulationPoint[] scarf = new SimulationPoint[r1.Scarf.Length];
            bool dead = r1.Crashed;
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = r1.Body[i].Replace(Vector2d.Lerp(r1.Body[i].Location, r2.Body[i].Location, percent));
            }
            for (int i = 0; i < scarf.Length; i++)
            {
                scarf[i] = r1.Scarf[i].Replace(Vector2d.Lerp(r1.Scarf[i].Location, r2.Scarf[i].Location, percent));
            }
            return new Rider(joints, scarf, r1.PhysicsBounds, dead, r1.SledBroken);
        }
        private unsafe static void ProcessLines(ISimulationGrid grid, SimulationPoint[] body, ref RectLRTB physinfo,
        Dictionary<int, Line> collisions = null, List<LineTrigger> activetriggers = null)
        {
            int bodylen = body.Length;
            for (int i = 0; i < bodylen; i++)
            {
                var startpos = body[i].Location;
                var cellx = (int)Math.Floor(startpos.X / 14);
                var celly = (int)Math.Floor(startpos.Y / 14);

                //every itreration is at least 3x3, so asjust the info for that
                physinfo.left = Math.Min(cellx - 1, physinfo.left);
                physinfo.top = Math.Min(celly - 1, physinfo.top);
                physinfo.right = Math.Max(cellx + 1, physinfo.right);
                physinfo.bottom = Math.Max(celly + 1, physinfo.bottom);
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var cell = grid.GetCell(cellx + x, celly + y);
                        if (cell == null)
                            continue;
                        foreach (var line in cell)
                        {
                            if (line.Interact(ref body[i]))
                            {
                                if (collisions != null)
                                {
                                    collisions[line.ID] = line;
                                }
                                if (line.Trigger != null && activetriggers != null)
                                {
                                    if (!activetriggers.Contains(line.Trigger))
                                    {
                                        activetriggers.Add(line.Trigger);
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }
        public static void ProcessScarfBones(Bone[] bones, SimulationPoint[] scarf)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                var j1 = scarf[bone.joint1];
                var j2 = scarf[bone.joint2];
                var d = j1.Location - j2.Location;
                var len = d.Length;
                if (!bone.OnlyRepel || len < bone.RestLength)
                {
                    double scalar = ((len - bone.RestLength) / len);
                    scarf[bone.joint2] = j2.Replace(j2.Location + (d * scalar));
                }
            }
        }
        public unsafe static void ProcessBones(Bone[] bones, SimulationPoint[] body, ref bool dead, List<int> breaks = null)
        {
            int bonelen = bones.Length;
            for (int i = 0; i < bonelen; i++)
            {
                var bone = bones[i];
                var j1 = body[bone.joint1];
                var j2 = body[bone.joint2];
                var d = j1.Location - j2.Location;
                var len = d.Length;
                if (!bone.OnlyRepel || len < bone.RestLength)
                {
                    var scalar = (len - bone.RestLength) / len * 0.5;
                    // instead of 0 checking dista the rationale is technically dista could be really really small
                    // and round off into infinity which gives us the NaN error.
                    if (double.IsInfinity(scalar))
                    {
                        scalar = 0;
                    }
                    if (bone.Breakable && (dead || scalar > bone.RestLength * RiderConstants.EnduranceFactor))
                    {
                        dead = true;
                        breaks?.Add(i);
                    }
                    else
                    {
                        d *= scalar;
                        body[bone.joint1] = j1.Replace(j1.Location - d);
                        body[bone.joint2] = j2.Replace(j2.Location + d);
                    }
                }
            }
        }
        public static DoubleRect GetBounds(Rider r)
        {
            double left, right, top, bottom;
            right = left = r.Body[0].Location.X;
            top = bottom = r.Body[0].Location.Y;
            for(int i = 0; i < r.Body.Length; i++)
            {
                var pos = r.Body[i].Location;
                right = Math.Max(pos.X, right);
                left = Math.Min(pos.X, left);
                top = Math.Min(pos.Y, top);
                bottom = Math.Max(pos.Y, bottom);
            }
            DoubleRect ret = new DoubleRect(left,top,right - left,bottom - top);
            return ret;
        }
        public Rider Simulate(Track track, Dictionary<int, Line> collisions, int maxiteration = 6)
        {
            return Simulate(track.Grid, track.Bones, track.ActiveTriggers, collisions, maxiteration);
        }
        public Rider Simulate(ISimulationGrid grid, Bone[] bones, List<LineTrigger> activetriggers, Dictionary<int, Line> collisions, int maxiteration = 6)
        {
            SimulationPoint[] body = new SimulationPoint[Body.Length];
            SimulationPoint[] scarf = new SimulationPoint[Scarf.Length];
            int scarflen = Scarf.Length;
            int bodylen = Body.Length;
            bool dead = Crashed;
            bool sledbroken = false;
            for (int i = 0; i < bodylen; i++)
            {
                body[i] = Body[i].Step();
            }
            RectLRTB phys = new RectLRTB(body[0]);
            using (grid.Sync.AcquireRead())
            {
                for (int i = 0; i < maxiteration; i++)
                {
                    ProcessBones(bones, body, ref dead);
                    ProcessLines(grid, body, ref phys, collisions, activetriggers);
                }
            }
            var nose = body[RiderConstants.SledTR].Location - body[RiderConstants.SledTL].Location;
            var tail = body[RiderConstants.SledBL].Location - body[RiderConstants.SledTL].Location;
            var head = body[RiderConstants.BodyShoulder].Location - body[RiderConstants.BodyButt].Location;
            if ((nose.X * tail.Y) - (nose.Y * tail.X) < 0 || // tail fakie
                (nose.X * head.Y) - (nose.Y * head.X) > 0)   // head fakie
            {
                dead = true;
                sledbroken = true;
            }
            for (int i = 1; i < scarflen; i++)
            {
                scarf[i] = Scarf[i].StepFriction();
            }
            scarf[0] = body[RiderConstants.BodyShoulder];
            ProcessScarfBones(RiderConstants.ScarfBones, scarf);
            return new Rider(body, scarf, phys, dead, sledbroken);
        }
        public List<int> Diagnose(Track track, Dictionary<int, Line> collisions = null, int maxiteration = 6)
        {
            var ret = new List<int>();
            if (Crashed)
                return ret;

            SimulationPoint[] body = new SimulationPoint[Body.Length];
            int bodylen = Body.Length;
            bool dead = Crashed;
            for (int i = 0; i < bodylen; i++)
            {
                body[i] = Body[i].Step();
            }
            RectLRTB phys = new RectLRTB(body[0]);
            List<int> breaks = new List<int>();
            using (track.Grid.Sync.AcquireRead())
            {
                for (int i = 0; i < maxiteration; i++)
                {
                    ProcessBones(track.Bones, body, ref dead, breaks);
                    if (dead)
                    {
                        return breaks;
                    }
                    ProcessLines(track.Grid, body, ref phys, collisions);
                }
            }
            var nose = body[RiderConstants.SledTR].Location - body[RiderConstants.SledTL].Location;
            var tail = body[RiderConstants.SledBL].Location - body[RiderConstants.SledTL].Location;
            var head = body[RiderConstants.BodyShoulder].Location - body[RiderConstants.BodyButt].Location;
            if ((nose.X * tail.Y) - (nose.Y * tail.X) < 0) // tail fakie

            {
                dead = true;
                ret.Add(-1);
            }
            if ((nose.X * head.Y) - (nose.Y * head.X) > 0)// head fakie
            {
                dead = true;
                ret.Add(-2);
            }

            return ret;
        }
        public Line[] GetScarfLines()
        {
            Line[] ret = new Line[Scarf.Length - 1];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new Line(Scarf[i].Location, Scarf[i + 1].Location);
            }
            return ret;
        }
    }
}