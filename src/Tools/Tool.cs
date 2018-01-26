//
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

namespace linerider.Tools
{
    public abstract class Tool : GameService
    {
        public bool Snapped = false;
        protected virtual double SnapRadius
        {
            get
            {
                return 2 / game.Track.Zoom;
            }
        }
        public virtual bool NeedsRender { get { return false; } }
        public abstract MouseCursor Cursor { get; }
        public Tool()
        {
        }

        protected Vector2d MouseCoordsToGame(Vector2d mouse)
        {
            var p = game.ScreenPosition + (mouse / game.Track.Zoom);
            return p;
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
        public Line SelectLine(TrackWriter trk, Vector2d position)
        {
            var ends = LineEndsInRadius(trk, position, 0);
            if (ends.Length > 0)
                return ends[0];
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)position - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    false);
            for (int i = 0; i < lines.Count; i++)
            {
                double lnradius = Line.GetLineRadius(lines[i]);
                var rect = Drawing.StaticRenderer.GenerateThickLine(lines[i].Position, lines[i].Position2, lnradius * 2);
                if (Utility.PointInRectangle(rect[3], rect[2], rect[1], rect[0], position))
                {
                    return lines[i];
                }
            }
            return null;
        }
        public Line CreateLine(TrackWriter trk, Vector2d start, Vector2d end, bool inv, bool snapstart, bool snapend)
        {
            Line added = null;
            switch (game.Canvas.ColorControls.Selected)
            {
                case LineType.Blue:
                    added = new StandardLine(start, end, false) { inv = inv };
                    added.CalculateConstants();
                    break;

                case LineType.Red:
                    added = new RedLine(start, end, false) { inv = inv };
                    (added as RedLine).Multiplier = game.Canvas.ColorControls.RedMultiplier;
                    added.CalculateConstants();
                    break;

                case LineType.Scenery:
                    added = new SceneryLine(start, end) { Width = game.Canvas.ColorControls.GreenMultiplier };
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
            game.Track.RequiresUpdate = true;
            return added;
        }
        /// <summary>
        /// Gets lines near the point by radius.
        /// does not support large distances as it only gets a small number of grid cells
        /// </summary>
        /// <returns>a sorted array of lines where 0 is the closest point within the radius</returns>
        public List<Line> LinesInRadius(TrackWriter trk, Vector2d position, double rad)
        {
            HashSet<Line> ret = new HashSet<Line>();
            var ends = LineEndsInRadius(trk, position, rad);
            foreach (var line in ends)
            {
                ret.Add(line);
            }
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)position - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    false);
            var evilcirc = Drawing.StaticRenderer.GenerateCircle(position.X, position.Y, rad, 10);
            for (int i = 0; i < lines.Count; i++)
            {
                double lnradius = Line.GetLineRadius(lines[i]);
                var rect = Drawing.StaticRenderer.GenerateThickLine(lines[i].Position, lines[i].Position2, lnradius * 2);
                for (int j = 0; j < evilcirc.Length; j++)
                {
                    if (Utility.PointInRectangle(rect[3], rect[2], rect[1], rect[0], evilcirc[j]))
                    {
                        ret.Add(lines[i]);
                        break;
                    }
                }
            }
            return ret.ToList();
        }
        /// <summary>
        /// Gets line ends near the point by radius.
        /// does not support large distances as it only gets a small number of grid cells
        /// </summary>
        /// <returns>a sorted array of lines where 0 is the closest point within the radius</returns>
        protected Line[] LineEndsInRadius(TrackReader trk, Vector2d point, double rad)
        {
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)point - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    false);
            SortedList<double, List<Line>> ret = new SortedList<double, List<Line>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var p1 = (point - lines[i].Position).Length;
                var p2 = (point - lines[i].Position2).Length;
                double lnradius = Line.GetLineRadius(lines[i]);
                var closer = Math.Min(p1, p2);
                if (closer - lnradius < rad)
                {
                    if (ret.ContainsKey(closer))
                    {
                        ret[closer].Add(lines[i]);
                    }
                    var l = new List<Line>();
                    l.Add(lines[i]);
                    ret[closer] = l;
                }
            }
            List<Line> retn = new List<Line>();
            for (int i = 0; i < ret.Values.Count; i++)
            {
                retn.AddRange(ret.Values[i]);
            }
            return retn.ToArray();
        }
        protected Vector2d TrySnapPoint(TrackReader track, Vector2d point)
        {
            var lines = this.LineEndsInRadius(track, point, SnapRadius);
            if (lines.Length == 0)
                return point;

            var snap = lines[0];
            return Utility.CloserPoint(point, snap.Position, snap.Position2);
        }
        /// <summary>
        /// Snaps the point specified in endpoint of line to another line if within snapradius
        /// </summary>
        protected void SnapLineEnd(TrackWriter trk, Line line, Vector2d endpoint)
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
                        trk.MoveLine(line, snap, line.Position2);
                    }
                    else if (line.Position2 == endpoint)
                    {
                        trk.MoveLine(line, line.Position, snap);
                    }
                    else
                    {
                        throw new Exception("Endpoint does not match line position in snap. It's not one of the ends of the line.");
                    }
                    if (line is StandardLine)
                        trk.TryConnectLines((StandardLine)line, (StandardLine)curr);

                    break;
                }
            }
        }
    }
}