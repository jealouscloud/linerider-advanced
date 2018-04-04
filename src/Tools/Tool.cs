﻿//
//  Tool.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using linerider.Utils;
using linerider.Game;

namespace linerider.Tools
{
    public abstract class Tool : GameService
    {
        protected virtual double SnapRadius
        {
            get
            {
                return 2 / game.Track.Zoom;
            }
        }
        public bool Active { get; protected set; }
        public virtual bool NeedsRender { get { return false; } }
        public abstract MouseCursor Cursor { get; }
        /// <summary>
        /// Determines whether to receive mouse movement events when they happen
        /// or only the last one before frame update.
        /// Leaving this false can be very good for performance.
        /// </summary>
        public virtual bool RequestsMousePrecision { get { return false; } }
        public Tool()
        {
        }

        protected Vector2d ScreenToGameCoords(Vector2d mouse)
        {
            return game.ScreenPosition + (mouse / game.Track.Zoom);
        }
        public virtual void OnMouseMoved(Vector2d pos)
        {
        }

        public virtual void OnMouseDown(Vector2d pos)
        {
        }

        public virtual void OnMouseRightDown(Vector2d pos)
        {
        }

        public virtual void OnMouseUp(Vector2d pos)
        {
        }

        public virtual void OnMouseRightUp(Vector2d pos)
        {
        }

        public virtual bool OnKeyDown(OpenTK.Input.Key k)
        {
            return false;
        }

        public virtual bool OnKeyUp(OpenTK.Input.Key k)
        {
            return false;
        }

        public virtual void Render()
        {
        }

        public virtual void Stop()
        {
        }

        public virtual void OnChangingTool()
        {
        }
        protected GameLine SelectLine(TrackWriter trk, Vector2d position, out bool knob)
        {
            knob = false;
            var zoom = game.Track.Zoom;
            var ends = LineEndsInRadius(trk, position, SnapRadius);
            if (ends.Length > 0)
            {
                knob = true;
                return ends[0];
            }
            var lines =
                trk.GetLinesInRect(
                    new DoubleRect((Vector2d)position - new Vector2d(24, 24),
                    new Vector2d(24 * 2, 24 * 2)),
                    false);
            foreach (var line in lines)
            {
                double lnradius = line.Width;
                var angle = Angle.FromLine(line.Position, line.Position2);
                var rect = Utility.GetThickLine(
                    line.Position,
                    line.Position2,
                    angle,
                    lnradius * 2);
                if (Utility.PointInRectangle(rect,
                position))
                {
                    return line;
                }
            }
            return null;
        }
        protected GameLine CreateLine(
            TrackWriter trk,
            Vector2d start,
            Vector2d end,
            bool inv,
            bool snapstart,
            bool snapend)
        {
            GameLine added = null;
            switch (game.Canvas.ColorControls.Selected)
            {
                case LineType.Blue:
                    added = new StandardLine(start, end, inv);
                    break;

                case LineType.Red:
                    var red = new RedLine(start, end, inv)
                    { Multiplier = game.Canvas.ColorControls.RedMultiplier };
                    red.CalculateConstants();//multiplier needs to be recalculated
                    added = red;
                    break;

                case LineType.Scenery:
                    added = new SceneryLine(start, end)
                    { Width = game.Canvas.ColorControls.GreenMultiplier };
                    break;
            }
            trk.AddLine(added);
            if (game.Canvas.ColorControls.Selected != LineType.Scenery)
            {
                if (snapstart)
                    SnapLineEnd(trk, added, added.Position);
                if (snapend)
                    SnapLineEnd(trk, added, added.Position2);
            }
            game.Track.Invalidate();
            return added;
        }
        /// <summary>
        /// Gets lines near the point by radius.
        /// does not support large distances as it only gets a small number of grid cells
        /// </summary>
        /// <returns>a sorted array of lines where 0 is the closest point</returns>
        public GameLine[] LinesInRadius(TrackWriter trk, Vector2d position, double rad)
        {
            SortedList<int, GameLine> lines = new SortedList<int, GameLine>();
            var inrect =
                trk.GetLinesInRect(new DoubleRect(position - new Vector2d(24, 24), new Vector2d(24 * 2, 24 * 2)),
                    false);
            var circle = Rendering.StaticRenderer.GenerateCircle(position.X, position.Y, rad, 8);

            var ends = LineEndsInRadius(trk, position, rad);
            foreach (var line in ends)
            {
                lines[line.ID] = line;
            }
            foreach (var line in inrect)
            {
                if (lines.ContainsKey(line.ID))
                    continue;
                var angle = Angle.FromLine(line.Position, line.Position2);
                var rect = Utility.GetThickLine(
                    line.Position,
                    line.Position2,
                    angle,
                    line.Width * 2);
                if (Utility.PointInRectangle(rect, position))
                {
                    lines.Add(line.ID, line);
                    continue;
                }
                else
                {
                    for (int i = 0; i < circle.Length; i++)
                    {
                        if (Utility.PointInRectangle(rect, circle[i]))
                        {
                            lines.Add(line.ID, line);
                            break;
                        }
                    }
                }
            }
            GameLine[] ret = new GameLine[lines.Count];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = lines.Values[(lines.Count - 1) - i];
            }
            return lines.Values.ToArray();
        }
        /// <summary>
        /// Gets line ends near the point by radius.
        /// does not support large distances as it only gets a small number of grid cells
        /// </summary>
        /// <returns>a sorted array of lines where 0 is the closest point within the radius</returns>
        protected GameLine[] LineEndsInRadius(TrackReader trk, Vector2d point, double rad)
        {
            var lines =
                trk.GetLinesInRect(new DoubleRect(point - new Vector2d(24, 24), new Vector2d(24 * 2, 24 * 2)),
                    false);
            SortedList<double, List<GameLine>> ret = new SortedList<double, List<GameLine>>();
            foreach (var line in lines)
            {
                var p1 = (point - line.Position).Length;
                var p2 = (point - line.Position2).Length;
                var closer = Math.Min(p1, p2);
                if (closer - line.Width < rad)
                {
                    if (ret.ContainsKey(closer))
                    {
                        ret[closer].Add(line);
                    }
                    else
                    {
                        var l = new List<GameLine>();
                        l.Add(line);
                        ret[closer] = l;
                    }
                }
            }
            List<GameLine> retn = new List<GameLine>();
            for (int i = 0; i < ret.Values.Count; i++)
            {
                retn.AddRange(ret.Values[i]);
            }
            return retn.ToArray();
        }
        protected bool LifeLock(TrackReader track, Timeline timeline, StandardLine line)
        {
            int offset = game.Track.Offset;
            int iteration = game.Track.IterationsOffset;
            if (offset == 0)
                return false;
            var frame = timeline.GetFrame(offset, iteration);
            if (!frame.Crashed)
            {
                if (Settings.PinkLifelock)
                {
                    var diagnosis = timeline.DiagnoseFrame(offset, iteration);
                    foreach (var v in diagnosis)
                    {
                        //the next frame dies on something that isnt a fakie, so we cant stop here
                        if (v >= 0)
                            return false;
                    }
                }
                if (Settings.IllegalFakieLifelock)
                {
                    var diagnosis = timeline.DiagnoseFrame(offset, iteration);
                    foreach (var v in diagnosis)
                    {
                        //the next frame dies from a fakie, so we can't stop here
                        if (v < 0)
                            return false;
                    }
                }
                if (timeline.IsLineHit(line.ID, game.Track.Offset))
                    return true;
            }
            return false;
        }
        protected Vector2d TrySnapPoint(TrackReader track, Vector2d point, out bool snapped)
        {
            var lines = this.LineEndsInRadius(track, point, SnapRadius);
            if (lines.Length == 0)
            {
                snapped = false;
                return point;
            }
            var snap = lines[0];
            snapped = true;
            return Utility.CloserPoint(point, snap.Position, snap.Position2);
        }
        /// <summary>
        /// Snaps the point specified in endpoint of line to another line if within snapradius
        /// </summary>
        protected void SnapLineEnd(TrackWriter trk, GameLine line, Vector2d endpoint)
        {
            var lines = LineEndsInRadius(trk, endpoint, SnapRadius);
            for (int i = 0; i < lines.Length; i++)
            {
                var curr = lines[i];
                if (curr != line)
                {
                    if (line is StandardLine && curr is SceneryLine)
                        continue;//phys lines dont wanna snap to scenery

                    var snap = Utility.CloserPoint(endpoint, curr.Position, curr.Position2);
                    if (line.Position == endpoint)
                    {
                        // don't snap to the same point.
                        if (line.Position2 != snap)
                            trk.MoveLine(line, snap, line.Position2);
                    }
                    else if (line.Position2 == endpoint)
                    {
                        // don't snap to the same point.
                        if (line.Position != snap)
                            trk.MoveLine(line, line.Position, snap);
                    }
                    else
                    {
                        throw new Exception("Endpoint does not match line position in snap. It's not one of the ends of the line.");
                    }
                    break;
                }
            }
        }
        protected void ShowTooltip(string message)
        {
            game.Canvas.SetTooltip(null, message);
        }
        protected void HideTooltip()
        {
            game.Canvas.RemoveTooltip(null);
        }
    }
}