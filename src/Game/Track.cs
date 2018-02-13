//
//  Track.cs
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

using linerider.Rendering;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using linerider.Game;
using linerider.Utils;

namespace linerider
{
    public class Track
    {
        public HashSet<int> AllCollidedLines = new HashSet<int>();

        public SimulationGrid Grid = new SimulationGrid();

        public List<ConcurrentDictionary<int, StandardLine>> Collisions =
            new List<ConcurrentDictionary<int, StandardLine>>();

        public FastGrid RenderCells = new FastGrid();

        public List<Line> Lines = new List<Line>();

        public string Name = "untitled";

        public List<Rider> RiderStates = new List<Rider>();
        //todo probably needs to be linkedlist for performance
        public List<LineTrigger> ActiveTriggers = null;
        private Vector2d _start = Vector2d.Zero;
        public Bone[] Bones = new Bone[RiderConstants.Bones.Length];
        public Vector2d StartOffset
        {
            get
            {
                return _start;
            }
            set
            {
                _start = value;
                GenerateBones();
            }
        }

        public bool ZeroStart = false;

        internal int _idcounter;

        private int _sceneryidcounter = -1;
        public FloatRect RiderRect
        {
            get
            {
                var ret = new FloatRect((Vector2)StartOffset, new Vector2(0, 0));
                ret.Width = 35;
                ret.Height = 22;
                ret.Top -= 11;
                return ret;
            }
        }
        public Track()
        {
            GenerateBones();
            Reset();
        }
        private void GenerateBones()
        {
            // if the start offset is different the floating point math could
            // result in a slightly different restlength and cause inconsistency.
            var joints = GetStart().Body;
            for (int i = 0; i < RiderConstants.Bones.Length; i++)
            {
                var bone = RiderConstants.Bones[i];
                bone.RestLength = (joints[bone.joint1].Location - joints[bone.joint2].Location).Length;
                if (bone.OnlyRepel)
                    bone.RestLength *= 0.5;
                Bones[i] = bone;
            }
        }
        public void AddLine(Line line, bool isloading = false)
        {
            Lines.Add(line);
            var scenery = line is SceneryLine;
            if (scenery)
            {
                line.ID = _sceneryidcounter--;
            }
            else if (line.ID == -1)
                line.ID = _idcounter++;
            else if (line.ID >= _idcounter)
            {
                _idcounter = line.ID + 1;
            }
            AddLineToGrid(line);

        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void AddLineToGrid(Line line)
        {
            if (line is StandardLine sl)
                Grid.AddLine(sl);
            RenderCells.AddLine(line);
        }

        public void CalculateAllCollidedLines()
        {
            //todo collision states are completely unprogrammed
        }

        public IEnumerable<Line> GetLinesInRect(FloatRect rect, bool precise)
        {
            var ret = RenderCells.LinesInRect(rect);
            if (precise)
            {
                var newret = new List<Line>(ret.Count);
                foreach(var line in ret)
                {
                    if (Line.DoesLineIntersectRect(line, rect))
                    {
                        newret.Add(line);
                    }
                }
                return newret;
            }
            return ret;
        }

        public int GetVersion()
        {
            return Grid.GridVersion;
        }

        public bool IsLineCollided(int id)
        {
            return false;
        }
        public void RemoveLine(Line l)
        {
            Lines.Remove(l);
            RemoveLineFromGrid(l);
        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void RemoveLineFromGrid(Line sl)
        {
            if (sl is StandardLine)
                Grid.RemoveLine((StandardLine)sl);
            RenderCells.RemoveLine(sl);
        }
        public Rider GetStart()
        {
            return Rider.Create(this.StartOffset, new Vector2d(ZeroStart ? 0 : RiderConstants.StartingMomentum, 0));
        }
        public void Reset()
        {
            Reset(GetStart());
        }
        public void Reset(Rider start)
        {
            RiderStates.Clear();
            RiderStates.Add(start);
        }


        public void SetVersion(int version)
        {
            Grid.GridVersion = version;
        }

        public void AddFrame()
        {
            //todo collision
            RiderStates.Add(RiderStates[RiderStates.Count - 1].Simulate(this,null));
        }
    }
}