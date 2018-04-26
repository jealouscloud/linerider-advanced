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
using System.Diagnostics;
using linerider.Audio;

namespace linerider
{
    public class Track
    {
        public SimulationGrid Grid = new SimulationGrid();
        public LinkedList<int> Lines = new LinkedList<int>();
        public Dictionary<int, GameLine> LineLookup = new Dictionary<int, GameLine>();

        public string Name = Constants.DefaultTrackName;
        public string Filename = null;
        public Song Song;
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
        public int SceneryLines { get; private set; }
        public int BlueLines { get; private set; }
        public int RedLines { get; private set; }
        public bool ZeroStart = false;

        internal int _idcounter;

        private int _sceneryidcounter = -1;
        public Track()
        {
            GenerateBones();
        }
        public GameLine[] GetLines()
        {
            GameLine[] ret = new GameLine[LineLookup.Count];
            int index = ret.Length - 1;
            foreach (var id in Lines)
            {
                ret[index] = LineLookup[id];
                index--;
            }
            return ret;
        }
        public GameLine[] GetSortedLines()
        {
            GameLine[] ret = new GameLine[LineLookup.Count];
            SortedSet<int> temp = new SortedSet<int>(Lines);
            int index = 0;
            // sorted as -2 -1 0 1 2
            foreach (var line in temp)
            {
                ret[index++] = LineLookup[line];
            }
            return ret;
        }
        private void GenerateBones()
        {
            // if the start offset is different the floating point math could
            // result in a slightly different restlength and cause inconsistency.
            var joints = GetStart().Body;
            Bone[] bones = new Bone[RiderConstants.Bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = RiderConstants.Bones[i];
                var rest = (joints[bone.joint1].Location - joints[bone.joint2].Location).Length;
                if (bone.OnlyRepel)
                    rest *= 0.5;
                bones[i] = new Bone(
                    bone.joint1,
                    bone.joint2,
                    rest,
                    bone.Breakable,
                    bone.OnlyRepel);
            }
            Bones = bones;
        }
        public void AddLine(GameLine line)
        {
            if (line.Type == LineType.Scenery)
            {
                if (line.ID == GameLine.UninitializedID || line.ID >= 0)
                    line.ID = _sceneryidcounter--;
                else if (line.ID <= _sceneryidcounter)
                {
                    _sceneryidcounter = line.ID - 1;
                }
            }
            else
            {
                if (line.ID == GameLine.UninitializedID)
                    line.ID = _idcounter++;
                else if (line.ID >= _idcounter)
                {
                    _idcounter = line.ID + 1;
                }
            }
            switch (line.Type)
            {
                case LineType.Blue:
                    BlueLines++;
                    break;
                case LineType.Red:
                    RedLines++;
                    break;
                case LineType.Scenery:
                    SceneryLines++;
                    break;
            }
            Debug.Assert(
                !LineLookup.ContainsKey(line.ID),
                "Lines occupying the same ID -- really bad");
            LineLookup.Add(line.ID, line);
            // here is where using a linkedlist shines:
            // we can make the most recent change at the front so if it gets
            // looked up it's easier and faster to find
            Lines.AddFirst(line.ID);

            if (line is StandardLine stl)
                AddLineToGrid(stl);
        }
        public void RemoveLine(GameLine line)
        {
            switch (line.Type)
            {
                case LineType.Blue:
                    BlueLines--;
                    break;
                case LineType.Red:
                    RedLines--;
                    break;
                case LineType.Scenery:
                    SceneryLines--;
                    break;
            }
            LineLookup.Remove(line.ID);
            Lines.Remove(line.ID);

            if (line is StandardLine stl)
                RemoveLineFromGrid(stl);
        }
        public void MoveLine(StandardLine line, Vector2d new1, Vector2d new2)
        {
            var old = line.Position;
            var old2 = line.Position2;
            line.Position = new1;
            line.Position2 = new2;
            line.CalculateConstants();
            Grid.MoveLine(old, old2, line);
        }
        public int GetVersion()
        {
            return Grid.GridVersion;
        }

        public bool IsLineCollided(int id)
        {
            return false;
        }

        /// <summary>
        /// Adds the line to the physics grid.
        /// </summary>
        public void AddLineToGrid(StandardLine line)
        {
            Grid.AddLine(line);
        }
        /// <summary>
        /// Removes the line from the physics
        /// </summary>
        public void RemoveLineFromGrid(StandardLine line)
        {
            Grid.RemoveLine(line);
        }
        public Rider GetStart()
        {
            return Rider.Create(this.StartOffset, new Vector2d(ZeroStart ? 0 : RiderConstants.StartingMomentum, 0));
        }
        public void SetVersion(int version)
        {
            Grid.GridVersion = version;
        }
    }
}