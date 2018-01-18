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
        public bool Crashed;
        public readonly SimulationPoint[] Body;
        public readonly Scarf Scarf;
        public bool SledBroken;


        public Rider(SimulationPoint[] body, Scarf scarf, bool dead = false, bool sledbroken = false)
        {
            Body = body;
            Scarf = scarf;
            Crashed = dead;
            SledBroken = sledbroken;
        }
        public static Rider Create(Vector2d start, Vector2d momentum)
        {
            var joints = new SimulationPoint[RiderConstants.DefaultRider.Length];
            var scarf = new SimulationPoint[RiderConstants.DefaultScarf.Length];
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
            }

            return new Rider(joints, new Scarf(joints[5].Location), false, false);
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
        public Rider Lerp(Rider rider2, float percent)
        {
            Rider ret = this;
            for (int i = 0; i < ret.Body.Length; i++)
            {
                ret.Body[i] = new SimulationPoint(Vector2d.Lerp(ret.Body[i].Location, rider2.Body[i].Location, percent), Vector2d.Zero, Vector2d.Zero, 0);
            }
            for (int i = 0; i < ret.Scarf._anchors.Length; i++)
            {
                ret.Scarf._anchors[i].Position = Vector2d.Lerp(ret.Scarf._anchors[i].Position, rider2.Scarf._anchors[i].Position, percent);
                ret.Scarf._anchors[i].Prev = Vector2d.Lerp(ret.Scarf._anchors[i].Prev, rider2.Scarf._anchors[i].Prev, percent);
            }
            return ret;
        }
        private static void ProcessLines(SimulationGrid grid, SimulationPoint[] joints, Dictionary<int, Line> collisions = null)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                var cellx = (int)Math.Floor(joints[i].Location.X / 14);
                var celly = (int)Math.Floor(joints[i].Location.Y / 14);
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var lines = grid.GetCell(cellx + x, celly + y);
                        if (lines != null)
                        {
                            foreach (var line in lines)
                            {
                                var newj = line.Interact(joints[i]);
                                if (collisions != null && newj.Location != joints[i].Location)
                                {
                                    collisions[line.ID] = line;
                                }
                                joints[i] = newj;
                            }
                        }
                    }
                }
            }
        }
        public static List<int> ProcessBones(Bone[] bones, SimulationPoint[] joints, ref bool dead, bool diagnose = false)
        {
            List<int> ret = diagnose ? new List<int>() : null;

            for (int i = 0; i < RiderConstants.Bones.Length; i++)
            {
                var bone = RiderConstants.Bones[i];
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
                        if (diagnose)
                        {
                            ret.Add(i);
                        }
                    }
                    else
                    {
						d *= scalar;
						joints[bone.joint1] = j1.CreateNewLocation(j1.Location - d);
						joints[bone.joint2] = j2.CreateNewLocation(j2.Location + d);
                    }
                }
            }
            return ret;
        }
        public Rider Simulate(Track track, Dictionary<int, Line> collisions)
        {
            SimulationPoint[] joints = Body.ToArray();
            bool dead = Crashed;
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = joints[i].StepMomentum();
            }
            for (int i = 0; i < 6; i++)
            {
                ProcessBones(track.Bones, joints, ref dead);
                ProcessLines(track.Grid, joints, collisions);
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
            var sc = Scarf.Clone();
            sc.Step(joints[5].Location);
            return new Rider(joints, sc, dead, sledbroken);
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

            for (int i = 0; i < maxiteration; i++)
            {
                var breaks = ProcessBones(track.Bones, joints, ref dead, true);
                if (dead)
                {
                    return new HashSet<int>(breaks);
                }
                ProcessLines(track.Grid, joints);
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
        public void StepScarf()
        {
            Scarf.Step(Body[5].Location);
        }
        public Line[] GetScarf()
        {
            var vecs = Scarf.GetAnchors(Body[5].Location);
            Line[] ret = new Line[vecs.Length - 1];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new Line(vecs[i], vecs[i + 1]);
            }
            return ret;
        }
    }
}