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
        #region Constructors

        public Track()
        {
            UndoManager = new UndoManager(this);
        }

        #endregion Constructors

        #region Fields

        public HashSet<int> AllCollidedLines = new HashSet<int>();

        public ChunkCollection Chunks = new ChunkCollection();

        public List<ConcurrentDictionary<int, StandardLine>> Collisions =
            new List<ConcurrentDictionary<int, StandardLine>>();

        public FastGrid FastChunks = new FastGrid();

        public int Frame;

        public List<Line> Lines = new List<Line>();

        public string Name = "untitled";

        public Rider RiderState = new Rider();

        public List<Rider> RiderStates = new List<Rider>();

        public Vector2d Start = new Vector2d(0, 0);

        public bool ZeroStart = false;

        private readonly ConcurrentQueue<Line> Changes = new ConcurrentQueue<Line>();

        internal int _idcounter;

        private int _sceneryidcounter = -1;

        #endregion Fields

        #region Properties

        public FloatRect RiderRect
        {
            get
            {
                var ret = new FloatRect((Vector2)Start, new Vector2(0, 0));
                ret.Width = 35;
                ret.Height = 22;
                ret.Top -= 11;
                return ret;
            }
        }

        public UndoManager UndoManager { get; private set; }

        #endregion Properties

        #region Methods

        public void AddLines(params Line[] lines)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                Lines.Add(lines[i]);
                var scenery = lines[i] is SceneryLine;
                if (scenery)
                {
                    lines[i].ID = _sceneryidcounter--;
                }
                else if (lines[i].ID == -1)
                    lines[i].ID = _idcounter++;
                else if (lines[i].ID >= _idcounter)
                {
                    _idcounter = lines[i].ID + 1;
                }
                AddLineToGrid(lines[i]);
                UndoManager.AddLine(lines[i]);
                if (!scenery)
                    ChangeMade(lines[i].Position, lines[i].Position2);
            }
        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void AddLineToGrid(Line sl)
        {
            if (!(sl is SceneryLine))
                Chunks.AddLine(sl);
            FastChunks.AddLine(sl);
        }

        private int _collisioncalculations;
        private readonly object _collisioncalculationlock = new object();

        public void CalculateAllCollidedLines()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                ConcurrentDictionary<int, StandardLine>[] collisions = null;
                try
                {
                    lock (_collisioncalculationlock)
                    {
                        Interlocked.Increment(ref _collisioncalculations);
                    }
                    while (_collisioncalculations != 1)
                    {
                        Thread.Sleep(1);
                    }
                    lock (Collisions)
                    {
                        collisions = Collisions.ToArray();
                    }
                    lock (AllCollidedLines)
                    {
                        AllCollidedLines.Clear();
                        foreach (var v in collisions)
                        {
                            if (_collisioncalculations > 1)
                                return;
                            foreach (var l in v)
                            {
                                if (_collisioncalculations > 1)
                                    return;
                                AllCollidedLines.Add(l.Key);
                            }
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _collisioncalculations);
                }
            }));
        }

        public int CalculateUpdateStart()
        {
            //todo function is O(n*y)
            //    return RiderStates.Count - 10;
            var updateStart = RiderStates.Count;
            var riderpositions = new List<HashSet<Point>>();
            lock (Changes)
            {
                while (Changes.Count > 0)
                {
                    Line l = null;
                    if (!Changes.TryDequeue(out l))
                        break;
                    var positions = Chunks.GetGridPositions(l);
                    var endi = Math.Min(updateStart, RiderStates.Count);
                    for (var i = 0; i < endi; i++)
                    {
                        var state = RiderStates[i];
                        var statepositions = new HashSet<Point>();
                        if (riderpositions.Count > i)
                        {
                            statepositions = riderpositions[i];
                        }
                        else
                        {
                            var anchorindex = 0;
                            foreach (var anchor in state.ModelAnchors)
                            {
                                var info = Chunks.ChunkInfo(anchor.Position.X, anchor.Position.Y);
                                for (var x = -2; x <= 2; x++)
                                {
                                    for (var y = -2; y <= 2; y++)
                                    {
                                        statepositions.Add(new Point(info.X + x, info.Y + y));
                                    }
                                }
                                var off = state.GetAnchorOffset(anchorindex);
                                if (off.X != 0 || off.Y != 0)
                                {
                                    info = Chunks.ChunkInfo(anchor.Position.X + off.X, anchor.Position.Y + off.Y);
                                    for (var x = -2; x <= 2; x++)
                                    {
                                        for (var y = -2; y <= 2; y++)
                                        {
                                            statepositions.Add(new Point(info.X + x, info.Y + y));
                                        }
                                    }
                                }
                                anchorindex++;
                            }
                            riderpositions.Add(statepositions);
                        }
                        var end = false;
                        foreach (var position in positions)
                        {
                            if (statepositions.Contains(new Point(position.X, position.Y)))
                            {
                                end = true;
                                if (i < updateStart)
                                    updateStart = i;
                                break;
                            }
                        }
                        if (end)
                            break;
                    }
                }
            }
            return updateStart;
        }

        /// <remarks>Does not care about inv lines, result is same</remarks>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void ChangeMade(Vector2d start, Vector2d end)
        {
            var l = new Line(start, end);
            foreach (var v in Changes)
            {
                if (v.Position == start && v.Position2 == end)
                    return;
            }
            l.diff = end - start;
            Changes.Enqueue(l);
        }

        public void ClearTrack()
        {
            _idcounter = 0;
            _sceneryidcounter = -1;
            Lines.Clear();
            Chunks = new ChunkCollection();
            FastChunks = new FastGrid();
            ResetUndo();
            ResetChanges();
            GC.Collect();
        }

        public bool DbgGridCheck(int gx, int gy)
        {
            var r = Chunks.PointToChunk(new Vector2d(gx * 14, gy * 14));
            return r != null && r.Count > 0;
        }

        public HashSet<int> Diagnose(Rider state)
        {
            var ret = new HashSet<int>();
            if (state.Crashed)
                return ret;
            var s = state.Clone();

            s.TickMomentum();

            for (var iteration = 0; iteration < 6; iteration++)
            {
                for (var i = 0; i < s.ModelLines.Length; i++)
                {
                    s.ModelLines[i].satisfyDistance(s);
                    if (s.Crashed)
                    {
                        ret.Add(i);
                        s.Crashed = false;
                    }
                }
                s.SatisfyBoundaries(this, null);
            }
            var sleddiff = s.ModelAnchors[3].Position - s.ModelAnchors[0].Position;
            if (sleddiff.X * (s.ModelAnchors[1].Position.Y - s.ModelAnchors[0].Position.Y) -
                sleddiff.Y * (s.ModelAnchors[1].Position.X - s.ModelAnchors[0].Position.X) < 0)
            {
                s.Crashed = true;
                s.SledBroken = true;
                ret.Add(-1);
            }
            if (sleddiff.X * (s.ModelAnchors[5].Position.Y - s.ModelAnchors[4].Position.Y) -
                sleddiff.Y * (s.ModelAnchors[5].Position.X - s.ModelAnchors[4].Position.X) > 0)
            {
                s.Crashed = true;
                s.SledBroken = true;
                ret.Add(-2);
            }
            return ret;
        }

        public HashSet<int> DiagnoseIteration(Rider state, int it)
        {
            var ret = new HashSet<int>();
            if (state.iterations[it].Crashed)
                return ret;
            var s = state.iterations[0].Clone();
            it -= 1; //skip momentum tick
            for (var iteration = 0; iteration < 6; iteration++)
            {
                for (var i = 0; i < s.ModelLines.Length; i++)
                {
                    s.ModelLines[i].satisfyDistance(s);
                    if (s.Crashed)
                    {
                        if (iteration != it + 1)
                            return ret;
                        ret.Add(i);
                        s.Crashed = false;
                    }
                }
                if (ret.Count != 0)
                    break;
                s.SatisfyBoundaries(this, null);
            }
            return ret;
        }
        public List<Line> Erase(Vector2d pos, LineType t, float zoom)
        {
            List<Line> ret = new List<Line>();
            var eraser = new Vector2d(5 / zoom, 5 / zoom);
            var searchrect = new FloatRect((Vector2)(pos - eraser), (Vector2)(eraser * 2));
            searchrect = searchrect.Inflate(24, 24);
            var fr = new FloatRect((Vector2)(pos - eraser), (Vector2)(eraser * 2));
            var lines = FastChunks.LinesInChunks(FastChunks.UsedChunksInRect(fr));

            foreach (var line in lines)
            {
                var scenery = line as SceneryLine;
                if (scenery != null)
                {
                    var sceneryeraser = new Vector2d((5 / zoom) * scenery.Width, (5 / zoom) * scenery.Width);
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
                if (pl1 != null)
                {
                    UndoManager.AddExtensionChange(pl1, pl2, false);
                }
                if (nl1 != null)
                {
                    UndoManager.AddExtensionChange(nl1, nl2, false);
                }
            }
            return ret;
        }

        public SortedList<int, Line> GetChunkAtPosition(Vector2d pos)
        {
            var chk = Chunks.PointToChunk(pos);
            if (chk == null)
                return new SortedList<int, Line>();
            return chk;
        }

        public List<Line> GetLinesInRect(FloatRect rect, bool precise, bool standardlinesonly = false)
        {
            List<FastGrid.Chunk> chunks;
            List<Line> ret;
            if (standardlinesonly)
            {
                chunks = FastChunks.UsedSolidChunksInRect(rect);
                ret = FastChunks.SortedLinesInChunks(chunks);
            }
            else
            {
                chunks = FastChunks.UsedChunksInRect(rect);
                ret = FastChunks.LinesInChunks(chunks);
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

        public decimal GetVersion()
        {
            return Chunks.GridVersion;
        }

        public void GwellAddLine(Line line)
        {
            if (line.ID == -1)
                line.ID = _idcounter;
            AddLineToGrid(line);
        }

        public void GwellRemoveLine(Line line)
        {
            RemoveLineFromGrid(line);
        }

        public bool IsLineCollided(int id)
        {
            ConcurrentDictionary<int, StandardLine>[] collisions = null;
            lock (Collisions)
            {
                collisions = Collisions.ToArray();
            }
            foreach (var v in collisions)
            {
                if (v.ContainsKey(id))
                    return true;
            }
            return false;
        }

        public void NextFrame()
        {
            SetFrame(++Frame);
        }

        public bool Redo()
        {
            return UndoManager.Redo();
        }

        public void RemoveLine(Line l)
        {
            ChangeMade(l.Position, l.Position2);
            Lines.Remove(l);
            RemoveLineFromGrid(l);
            UndoManager.RemoveLine(l);
        }

        /// <summary>
        ///     For moving lines
        /// </summary>
        public void RemoveLineFromGrid(Line sl)
        {
            if (!(sl is SceneryLine))
                Chunks.RemoveLine(sl);
            FastChunks.RemoveLine(sl);
        }

        public void Reset()
        {
            Frame = 0;
            RiderStates.Clear();
            RiderStates.Add(RiderState.Clone());
        }

        public void ResetChanges()
        {
            Line l = null;
            while (Changes.TryDequeue(out l)) ;
        }

        public void ResetUndo()
        {
            UndoManager = new UndoManager(this);
        }

        public void SetFrame(int f)
        {
            if (f > RiderStates.Count)
            {
                // f = RiderStates.Count;
                throw new Exception("unsupported frameskip to " + (f - RiderStates.Count));
            }
            if (f == RiderStates.Count)
            {
                var c = Tick();
                lock (Collisions)
                {
                    Collisions.Add(c);
                }
                lock (AllCollidedLines)
                {
                    foreach (var V in c)
                    {
                        AllCollidedLines.Add(V.Key);
                    }
                }
                RiderStates.Add(RiderState.Clone());
                Frame = f;
            }
            else
            {
                Frame = f;
                RiderState = RiderStates[f].Clone();
            }
        }

        public void SetVersion(decimal version)
        {
            Chunks.GridVersion = version;
        }

        public ConcurrentDictionary<int, StandardLine> Tick()
        {
            return Tick(RiderState);
        }

        public ConcurrentDictionary<int, StandardLine> Tick(Rider state)
        {
            var collisions = new ConcurrentDictionary<int, StandardLine>();
            state.TickMomentum();
            //state.iterations.Clear();
            for (var iteration = 0; iteration < 6; iteration++)
            {
                //state.iterations.Add(state.Clone());
                state.SatisfyDistance();
                state.SatisfyBoundaries(this, collisions);
            }
            var points = state.ModelAnchors;
            var sleddiff = points[3].Position - points[0].Position;
            if (sleddiff.X * (points[1].Position.Y - points[0].Position.Y) -
                sleddiff.Y * (points[1].Position.X - points[0].Position.X) < 0)
            {
                state.Crashed = true;
                state.SledBroken = true;
            }
            if (sleddiff.X * (points[5].Position.Y - points[4].Position.Y) -
                sleddiff.Y * (points[5].Position.X - points[4].Position.X) > 0)
            {
                state.Crashed = true;
                state.SledBroken = true;
            }
            state.StepScarf();
            return collisions;
        }

        public void TickWithIterations(Rider state)
        {
            var collisions = new ConcurrentDictionary<int, StandardLine>();
            state.TickMomentum();
            if (state.iterations == null)
                state.iterations = new List<Rider>();
            state.iterations.Clear();
            for (var iteration = 0; iteration < 6; iteration++)
            {
                var it = state.Clone();
                it.StepScarf();
                state.iterations.Add(it);
                state.SatisfyDistance();
                state.SatisfyBoundaries(this, collisions);
            }
            var points = state.ModelAnchors;
            var sleddiff = points[3].Position - points[0].Position;
            if (sleddiff.X * (points[1].Position.Y - points[0].Position.Y) -
                sleddiff.Y * (points[1].Position.X - points[0].Position.X) < 0)
            {
                state.Crashed = true;
                state.SledBroken = true;
            }
            if (sleddiff.X * (points[5].Position.Y - points[4].Position.Y) -
                sleddiff.Y * (points[5].Position.X - points[4].Position.X) > 0)
            {
                state.Crashed = true;
                state.SledBroken = true;
            }
            state.StepScarf();
        }

        public void TrackChanged()
        {
            var start = Frame + 1;
            if (start < RiderStates.Count)
                RiderStates.RemoveRange(start, RiderStates.Count - start);
        }

        public bool Undo()
        {
            return UndoManager.Undo();
        }

        #endregion Methods

        #region Classes

        public class Chunk : SortedList<int, Line>
        {
            #region Classes

            private class descintcomparer : IComparer<int>
            {
                #region Methods

                public int Compare(int x, int y)
                {
                    return y - x;
                }

                #endregion Methods
            }

            #endregion Classes

            #region Properties

            public SortedList<int, Line> Lines
            {
                get { return this; }
            }

            #endregion Properties

            #region Constructors

            public Chunk()
                : base(new descintcomparer())
            {
            }

            #endregion Constructors
        }

        public class ChunkCollection
        {
            #region Fields

            public const int squaresize = 14;
            public decimal GridVersion = 6.2m;

            private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, Chunk>> Chunks =
                new ConcurrentDictionary<int, ConcurrentDictionary<int, Chunk>>();

            #endregion Fields

            #region Methods

            public void AddLine(Line line)
            {
                var positions = GetGridPositions(line);
                foreach (var pos in positions)
                {
                    Register(line, pos.X, pos.Y);
                }
            }

            //[-5] [-4] ... [-1] [0] [1]
            public gridpos ChunkInfo(double posx, double posy)
            {
                var x = (int)Math.Floor(posx / squaresize);
                var y = (int)Math.Floor(posy / squaresize);

                return new gridpos { Gx = (posx - (squaresize * x)), Gy = (posy - (squaresize * y)), X = x, Y = y };
            }

            public Chunk GetChunk(int x, int y)
            {
                ConcurrentDictionary<int, Chunk> row;
                if (Chunks.TryGetValue(x, out row))
                {
                    Chunk chunk;
                    if (row.TryGetValue(y, out chunk))
                    {
                        return chunk;
                    }
                }
                return null;
            }

            public List<gridpos> GetGridPositions(Line line)
            {
                var ret = new List<gridpos>();
                var gridstart = ChunkInfo(line.Position.X, line.Position.Y);
                var gridend = ChunkInfo(line.Position2.X, line.Position2.Y);

                ret.Add(gridstart.Clone());
                if ((line.diff.X == 0 && line.diff.Y == 0) || (gridstart.X == gridend.X && gridstart.Y == gridend.Y))
                    return ret;

                int p1X = Math.Min(gridstart.X, gridend.X),
                    p2X = Math.Max(gridstart.X, gridend.X),
                    p1Y = Math.Min(gridstart.Y, gridend.Y),
                    p2Y = Math.Max(gridstart.Y, gridend.Y);
                var box = Rectangle.FromLTRB(p1X, p1Y, p2X, p2Y);
                var current = line.Position;
                if (GridVersion == 6.2m)
                {
                    while (true)
                    {
                        double difY, difX;
                        if (gridstart.X < 0)
                            difX = line.diff.X > 0 ? (squaresize + gridstart.Gx) : (-squaresize - gridstart.Gx);
                        else
                            difX = line.diff.X > 0 ? (squaresize - gridstart.Gx) : (-(gridstart.Gx + 1));

                        if (gridstart.Y < 0)
                            difY = line.diff.Y > 0 ? (squaresize + gridstart.Gy) : (-squaresize - gridstart.Gy);
                        else
                            difY = line.diff.Y > 0 ? (squaresize - gridstart.Gy) : (-(gridstart.Gy + 1));
                        var ratiox = line.diff.X * difY * (1 / line.diff.Y);
                        var ratioy = line.diff.Y * difX * (1 / line.diff.X);
                        current.X += (Math.Abs(ratiox) < Math.Abs(difX)) ? ratiox : difX;
                        current.Y += (Math.Abs(ratioy) < Math.Abs(difY)) ? ratioy : difY;
                        gridstart = ChunkInfo(current.X, current.Y);
                        if (!CheckBounds(box, gridstart.X, gridstart.Y))
                            return ret;
                        ret.Add(gridstart.Clone());
                    }
                }
                if (GridVersion == 6.1m) //TODO DO NOT RELEASE WITHOUT FIXING FREEZE ISSUE
                {
                    double slope = 0;
                    double _loc13 = 0;
                    double isbelowactualY = 0;
                    if (line.diff.X != 0 && line.diff.Y != 0)
                    {
                        slope = line.diff.Y / line.diff.X;
                        _loc13 = 1.0 / slope;
                        isbelowactualY = line.Position.Y - slope * line.Position.X;
                    } // end if
                    while (true)
                    {
                        double difY, difX;
                        difX = -gridstart.Gx + (line.diff.X > 0 ? squaresize : -1);
                        difY = -gridstart.Gy + (line.diff.Y > 0 ? squaresize : -1);
                        if (line.diff.X == 0)
                        {
                            current.Y = current.Y + difY;
                        }
                        else if (line.diff.Y == 0)
                        {
                            current.X = current.X + difX;
                        }
                        else
                        {
                            var whyyy = Math.Round(slope * (current.X + difX) + isbelowactualY);
                            if (Math.Abs(whyyy - current.Y) < Math.Abs(difY))
                            {
                                current.X = current.X + difX;
                                current.Y = whyyy;
                            }
                            else if (Math.Abs(whyyy - current.Y) == Math.Abs(difY))
                            {
                                current.X = current.X + difX;
                                current.Y = current.Y + difY;
                            }
                            else
                            {
                                current.X = Math.Round((current.Y + difY - isbelowactualY) * _loc13);
                                current.Y = current.Y + difY;
                            }
                        }
                        gridstart = ChunkInfo(current.X, current.Y);
                        if (CheckBounds(box, gridstart.X, gridstart.Y))
                        {
                            ret.Add(gridstart.Clone());
                            continue;
                        }
                        return ret;
                    }
                }
                return ret;
            }

            public Chunk PointToChunk(Vector2d pos)
            {
                return GetChunk((int)Math.Floor(pos.X / squaresize), (int)Math.Floor(pos.Y / squaresize));
            }

            public void RemoveLine(Line line)
            {
                var positions = GetGridPositions(line);
                foreach (var pos in positions)
                {
                    Unregister(line, pos.X, pos.Y);
                }
            }

            private bool CheckBounds(Rectangle r, int x, int y)
            {
                return x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom;
            }

            private void Register(Line l, int x, int y)
            {
                ConcurrentDictionary<int, Chunk> xrow;
                if (!Chunks.ContainsKey(x))
                {
                    xrow = new ConcurrentDictionary<int, Chunk>();
                    Chunks[x] = xrow;
                }
                else
                {
                    xrow = Chunks[x];
                }
                Chunk yrow;
                if (!xrow.ContainsKey(y))
                {
                    yrow = new Chunk();
                    xrow[y] = yrow;
                }
                else
                {
                    yrow = xrow[y];
                }
                yrow[l.ID] = l;
            }

            private void Unregister(Line l, int x, int y)
            {
                ConcurrentDictionary<int, Chunk> xrow;
                if (!Chunks.ContainsKey(x))
                {
                    return;
                }
                xrow = Chunks[x];
                if (xrow.ContainsKey(y))
                {
                    xrow[y].Remove(l.ID);
                }
            }

            #endregion Methods
        }

        public class gridpos
        {
            #region Methods

            public gridpos Clone()
            {
                return new gridpos { Gx = Gx, Gy = Gy, X = X, Y = Y };
            }

            #endregion Methods

            #region Fields

            public double Gx;

            public double Gy;

            public int X;

            public int Y;

            #endregion Fields
        }

        public class Linecomparer : IComparer<Line>, IEqualityComparer<Line>
        {
            #region Methods

            public int Compare(Line x, Line y)
            {
                return y.ID - x.ID;
            }

            public bool Equals(Line x, Line y)
            {
                return x.ID == y.ID;
            }

            public int GetHashCode(Line x)
            {
                return x.ID;
            }

            #endregion Methods
        }

        #endregion Classes
    }
}