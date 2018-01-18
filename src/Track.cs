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

using linerider.Drawing;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using linerider.Game;
namespace linerider
{
    public class Track
    {
        #region Fields

        public HashSet<int> AllCollidedLines = new HashSet<int>();

        public SimulationGrid Grid = new SimulationGrid();

        public List<ConcurrentDictionary<int, StandardLine>> Collisions =
            new List<ConcurrentDictionary<int, StandardLine>>();

        public FastGrid RenderCells = new FastGrid();

        public List<Line> Lines = new List<Line>();

        public string Name = "untitled";

        public List<Rider> RiderStates = new List<Rider>();
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

        #endregion Fields

        #region Properties

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

        #endregion Properties

        #region Methods

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
            if (!scenery && !isloading)
                ChangeMade(line.Position, line.Position2);

        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void AddLineToGrid(Line sl)
        {
            if (!(sl is SceneryLine))
                Grid.AddLine(sl);
            RenderCells.AddLine(sl);
        }

        public void CalculateAllCollidedLines()
        {
            //todo collision states are completely unprogrammed
        }

        public int CalculateUpdateStart()
        {
            return 0;
        }

        /// <remarks>Does not care about inv lines, result is same</remarks>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void ChangeMade(Vector2d start, Vector2d end)
        {
            return;
        }

        public void ClearTrack()
        {
            _idcounter = 0;
            _sceneryidcounter = -1;
            Lines.Clear();
            Grid = new SimulationGrid();
            RenderCells = new FastGrid();
            ResetChanges();
            GC.Collect();
        }

        public HashSet<int> Diagnose(Rider state, int maxiteration = 6)
        {
            return state.Diagnose(this, maxiteration);
        }

        public List<Line> Erase(Vector2d pos, LineType t, float zoom)
        {
            //todo circular eraser
            List<Line> ret = new List<Line>();
            var eraser = new Vector2d(10 / zoom, 10 / zoom);
            var searchrect = new FloatRect((Vector2)(pos - eraser), (Vector2)(eraser * 2));
            searchrect = searchrect.Inflate(24, 24);
            var fr = new FloatRect((Vector2)(pos - eraser), (Vector2)(eraser * 2));
            var lines = RenderCells.LinesInChunks(RenderCells.UsedChunksInRect(fr));

            foreach (var line in lines)
            {
                var scenery = line as SceneryLine;
                if (scenery != null)
                {
                    var sceneryeraser = new Vector2d((10 / zoom) * scenery.Width, (10 / zoom) * scenery.Width);
                    var sceneryfr = new FloatRect((Vector2)(pos - sceneryeraser), (Vector2)(sceneryeraser * 2));
                    if (!Line.DoesLineIntersectRect(line, sceneryfr))
                        continue;
                }
                else
                {
                    if (!Line.DoesLineIntersectRect(line, fr))
                        continue;
                }
                if (!(t == LineType.All || ((t == LineType.Red && line.GetLineType() == LineType.Red) ||
                                            (t == LineType.Blue && line.GetLineType() == LineType.Blue) ||
                                            (t == LineType.Scenery && line.GetLineType() == LineType.Scenery))))
                    continue;
                var sl = line as StandardLine;
                StandardLine pl1 = null;
                StandardLine pl2 = null;
                StandardLine nl1 = null;
                StandardLine nl2 = null;
                if (sl != null)
                {
                    if (sl.Prev != null)
                    {
                        pl1 = sl.Prev;
                        pl2 = sl;
                        sl.Prev.Next = null;
                        sl.Prev.RemoveExtension(StandardLine.ExtensionDirection.Right);
                        sl.RemoveExtension(StandardLine.ExtensionDirection.Left);
                        sl.Prev = null;
                    }
                    if (sl.Next != null)
                    {
                        nl1 = sl;
                        nl2 = sl.Next;
                        sl.Next.Prev = null;
                        sl.Next.RemoveExtension(StandardLine.ExtensionDirection.Left);
                        sl.RemoveExtension(StandardLine.ExtensionDirection.Right);
                        sl.Next = null;
                    }
                }
                RemoveLine(line);
                ret.Add(line);
            }
            return ret;
        }

        public List<Line> GetLinesInRect(FloatRect rect, bool precise, bool standardlinesonly = false)
        {
            List<FastGrid.Chunk> chunks;
            List<Line> ret;
            if (standardlinesonly)
            {
                chunks = RenderCells.UsedSolidChunksInRect(rect);
                ret = RenderCells.SortedLinesInChunks(chunks);
            }
            else
            {
                chunks = RenderCells.UsedChunksInRect(rect);
                ret = RenderCells.LinesInChunks(chunks);
            }
            if (precise)
            {
                var newret = new List<Line>(ret.Count);
                for (var i = 0; i < ret.Count; i++)
                {
                    var line = ret[i];
                    if (Line.DoesLineIntersectRect(line, rect))
                    {
                        newret.Add(line);
                    }
                }
                ret = newret;
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
            ChangeMade(l.Position, l.Position2);
            Lines.Remove(l);
            RemoveLineFromGrid(l);
        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void RemoveLineFromGrid(Line sl)
        {
            if (!(sl is SceneryLine))
                Grid.RemoveLine(sl);
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

        public void ResetChanges()
        {
            //todo for live update rework
        }


        public void SetVersion(int version)
        {
            Grid.GridVersion = version;
        }

        public void AddFrame()
        {
            RiderStates.Add(Tick(RiderStates[RiderStates.Count - 1]));
        }

        public Rider Tick(Rider state, int maxiterations = 6, Dictionary<int, Line> collisions = null)
        {
            return state.Simulate(this, collisions);
        }

        #endregion Methods
    }
}