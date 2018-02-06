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
using System.Linq;
using System.Collections.Generic;
using linerider.Lines;
namespace linerider.Game
{
    public struct Rider
    {
        public readonly SimulationPoint[] Body;
        public readonly SimulationPoint[] Scarf;
        public bool Crashed;
        public bool SledBroken;
        public struct PhysicsInfo
        {
            public int top;
            public int left;
            public int right;
            public int bottom;
            public bool ContainsCell(GridPoint cell)
            {
                return cell.X >= left && cell.X <= right && cell.Y >= top && cell.Y <= bottom;
            }
        }
        public PhysicsInfo PhysInfo;
        public Rider(SimulationPoint[] body, SimulationPoint[] scarf, bool dead, bool sledbroken, PhysicsInfo pi)
        {
            Body = body;
            Scarf = scarf;
            Crashed = dead;
            SledBroken = sledbroken;
            PhysInfo = pi;
        }
        public static Rider Create(Vector2d start, Vector2d momentum)
        {
            var joints = new SimulationPoint[RiderConstants.DefaultRider.Length];
            var scarf = new SimulationPoint[RiderConstants.DefaultScarf.Length + 1];
            PhysicsInfo physinfo = new PhysicsInfo();
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
                var cellx = (int)Math.Floor(joints[i].Location.X / 14);
                var celly = (int)Math.Floor(joints[i].Location.Y / 14);

                physinfo.left = Math.Min(cellx - 1, physinfo.left);
                physinfo.top = Math.Min(celly - 1, physinfo.top);
                physinfo.right = Math.Max(cellx + 1, physinfo.right);
                physinfo.bottom = Math.Max(celly + 1, physinfo.bottom);
            }
            scarf[0] = joints[RiderConstants.BodyShoulder];
            for (int i = 0; i < RiderConstants.DefaultScarf.Length; i++)
            {
                var pos = scarf[0].Location + RiderConstants.DefaultScarf[i];
                scarf[i + 1] = new SimulationPoint(pos, pos, Vector2d.Zero, 0.9);
            }
            return new Rider(joints, scarf, false, false, physinfo);
        }


        public Vector2d CalculateCenter()
        {
            if (Crashed)
                return Body[4].Location;
            var anchorsaverage = new Vector2d();
            foreach (var anchor in Body)
            {
                anchorsaverage += anchor.Location;
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
                joints[i] = new SimulationPoint(Vector2d.Lerp(r1.Body[i].Location, r2.Body[i].Location, percent), Vector2d.Zero, Vector2d.Zero, 0);
            }
            for (int i = 0; i < scarf.Length; i++)
            {
                scarf[i] = new SimulationPoint(Vector2d.Lerp(r1.Scarf[i].Location, r2.Scarf[i].Location, percent), Vector2d.Zero, Vector2d.Zero, 0);
            }
            return new Rider(joints, scarf, dead, r1.SledBroken, new PhysicsInfo());
        }
        /// <summary>
        /// Processes the lines in a cell, but does not run triggers or collision checks.
        /// </summary>
        public static SimulationPoint ProcessCell(SimulationCell cell, SimulationPoint joint)
        {
            foreach (var line in cell)
            {
                line.Interact(ref joint);
            }
            return joint;
        }
        /// <summary>
        /// Processes the lines in a cell
        /// </summary>
        public static SimulationPoint ProcessCell(SimulationCell cell, SimulationPoint joint, Dictionary<int, Line> collisions, List<LineTrigger> activetriggers)
        {
            foreach (var line in cell)
            {
                if (line.Interact(ref joint))
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
            return joint;
        }
        private static void ProcessLines(SimulationGrid grid, SimulationPoint[] joints, ref PhysicsInfo physinfo, Dictionary<int, Line> collisions = null, List<LineTrigger> activetriggers = null)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                var cellx = (int)Math.Floor(joints[i].Location.X / 14);
                var celly = (int)Math.Floor(joints[i].Location.Y / 14);

                physinfo.left = Math.Min(cellx - 1, physinfo.left);
                physinfo.top = Math.Min(celly - 1, physinfo.top);
                physinfo.right = Math.Max(cellx + 1, physinfo.right);
                physinfo.bottom = Math.Max(celly + 1, physinfo.bottom);
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var lines = grid.GetCell(cellx + x, celly + y);
                        if (lines != null)
                            joints[i] = ProcessCell(lines, joints[i], collisions, activetriggers);
                    }
                }
            }
        }
        public static void ProcessScarfBones(SimulationPoint[] joints, Bone[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                var j1 = joints[bone.joint1];
                var j2 = joints[bone.joint2];
                var d = j1.Location - j2.Location;
                var len = d.Length;
                if (!bone.OnlyRepel || len < bone.RestLength)
                {
                    double scalar = ((len - bone.RestLength) / len);
                    joints[bone.joint2] = j2.CreateNewLocation(j2.Location + (d * scalar));
                }
            }
        }
        public static void ProcessBones(SimulationPoint[] joints, Bone[] bones, ref bool dead, bool diagnose = false, List<int> breaks = null)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                var j1 = joints[bone.joint1];
                var j2 = joints[bone.joint2];
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
                        joints[bone.joint1] = j1.CreateNewLocation(j1.Location - d);
                        joints[bone.joint2] = j2.CreateNewLocation(j2.Location + d);
                    }
                }
            }
        }
        public Rider Simulate(Track track, Dictionary<int, Line> collisions)
        {
            SimulationPoint[] joints = new SimulationPoint[Body.Length];
            SimulationPoint[] scarf = new SimulationPoint[Scarf.Length];
            bool dead = Crashed;
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = Body[i].StepMomentum();
            }
            PhysicsInfo nfo = new PhysicsInfo();
            using (track.Grid.Sync.AcquireRead())
            {
                for (int i = 0; i < 6; i++)
                {
                    ProcessBones(joints, track.Bones, ref dead);
                    ProcessLines(track.Grid, joints, ref nfo, collisions, track.ActiveTriggers);
                }
            }
            bool sledbroken = false;
            var nose = joints[RiderConstants.SledTR].Location - joints[RiderConstants.SledTL].Location;
            var tail = joints[RiderConstants.SledBL].Location - joints[RiderConstants.SledTL].Location;
            var body = joints[RiderConstants.BodyShoulder].Location - joints[RiderConstants.BodyButt].Location;
            if ((nose.X * tail.Y) - (nose.Y * tail.X) < 0 || // tail fakie
                (nose.X * body.Y) - (nose.Y * body.X) > 0)   // body fakie
            {
                dead = true;
                sledbroken = true;
            }

            for (int i = 0; i < scarf.Length; i++)
            {
                scarf[i] = Scarf[i].StepMomentumFriction();
            }
            scarf[0] = joints[RiderConstants.BodyShoulder];
            ProcessScarfBones(scarf, RiderConstants.ScarfBones);
            return new Rider(joints, scarf, dead, sledbroken, nfo);
        }
        public HashSet<int> Diagnose(Track track, int maxiteration = 6)
        {
            var ret = new HashSet<int>();
            if (Crashed)
                return ret;

            SimulationPoint[] joints = Body.ToArray();
            bool dead = Crashed;
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = joints[i].StepMomentum();
            }

            PhysicsInfo n = new PhysicsInfo();
            List<int> breaks = new List<int>();
            using (track.Grid.Sync.AcquireRead())
            {
                for (int i = 0; i < maxiteration; i++)
                {
                    ProcessBones(joints, track.Bones, ref dead, true, breaks);
                    if (dead)
                    {
                        return new HashSet<int>(breaks);
                    }
                    ProcessLines(track.Grid, joints, ref n);
                }
            }
            var nose = joints[RiderConstants.SledTR].Location - joints[RiderConstants.SledTL].Location;
            var tail = joints[RiderConstants.SledBL].Location - joints[RiderConstants.SledTL].Location;
            var body = joints[RiderConstants.BodyShoulder].Location - joints[RiderConstants.BodyButt].Location;
            if ((nose.X * tail.Y) - (nose.Y * tail.X) < 0) // tail fakie
                                                           // body fakie
            {
                dead = true;
                ret.Add(-1);
            }
            if ((nose.X * body.Y) - (nose.Y * body.X) > 0)
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