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

namespace linerider
{
    public abstract class Tool : GameService
    {
        public virtual bool NeedsRender { get { return false; } }
        public abstract MouseCursor Cursor { get; }
        public Tool()
        {
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
        private static double isLeft(Vector2d P0, Vector2d P1, Vector2d P2)
		{
			return ((P1.X - P0.Y) * (P2.Y - P0.Y) - (P2.X - P0.X) * (P1.Y - P0.Y));
		}
		//https://gamedev.stackexchange.com/questions/110229/how-do-i-efficiently-check-if-a-point-is-inside-a-rotated-rectangle
		private static bool PointInRectangle(Vector2d X, Vector2d Y, Vector2d Z, Vector2d W, Vector2d P)
		{
			return (isLeft(X, Y, P) > 0 && isLeft(Y, Z, P) > 0 && isLeft(Z, W, P) > 0 && isLeft(W, X, P) > 0);
		}
        public Line SelectLine(TrackWriter trk, Vector2d position)
        {
            var ends = LineEndsInRadius(trk, position, 2);
            if (ends.Length > 0)
                return ends[0];
			var lines =
				trk.GetLinesInRect(new FloatRect((Vector2)position - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
					false);
            for (int i = 0; i < lines.Count; i++)
            {
                
                var rect = Drawing.StaticRenderer.GenerateThickLine(lines[i].Position, lines[i].Position2, 2);
                if (PointInRectangle(rect[0], rect[3], rect[2], rect[1], position))
                {
                    return lines[i];
                }
            }
            return null;
        }
        // heavy lifting function for creating lines
        public Line CreateLine(TrackWriter trk, Vector2d start, Vector2d end, bool inv)
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
                trk.Track.ChangeMade(start, end);
            }
            game.Invalidate();
            return added;
        }
        /// Heavy lifting function for moving an existing line.
        protected void MoveLine(TrackWriter trk, Line line, Vector2d pos1, Vector2d pos2)
        {
            if (line is SceneryLine)
            {
                trk.Track.RemoveLineFromGrid(line);
                line.Position = pos1;
                line.Position2 = pos2;
                trk.Track.AddLineToGrid(line);
            }
            else
            {
                var phys = (StandardLine)line;
                trk.Track.RemoveLineFromGrid(phys);
                trk.Track.ChangeMade(line.Position, line.Position2);
                trk.Track.ChangeMade(pos1, pos2);
                line.Position = pos1;
                line.Position2 = pos2;
                game.Track.RedrawLine(line);
                trk.Track.AddLineToGrid(phys);
                game.Invalidate();
            }
        }
        /// Gets lines near the point by radius.
        // does not support large distances as it only gets a small number of grid cells
        protected Line[] LineEndsInRadius(TrackWriter trk, Vector2d point, int rad)
        {
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)point - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    false);
            SortedList<double, Line> ret = new SortedList<double, Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                var p1 = (point - lines[i].Position).Length;
                var p2 = (point - lines[i].Position2).Length;
                var closer = Math.Min(p1, p2);
                if (closer < rad)
                {
                    ret.Add(closer, lines[i]);
                }
            }
            return ret.Values.ToArray();
        }
        /// Snaps the point specified in endpoint of line to the
        // line.
        protected void SnapLineEnd(TrackWriter trk, Line line, Vector2d endpoint)
        {
            var lines = LineEndsInRadius(trk, endpoint, 4);
            for (int i = 0; i < lines.Length; i++)
            {
                var curr = lines[i];
                if (curr != line)
                {
                    if (line is StandardLine && curr is SceneryLine)
                        continue;//phys lines dont wanna snap to scenery

                    if (line.Position == endpoint)
                    {
                        if ((line.Position - curr.Position).Length < (line.Position - curr.Position2).Length)
                        {
                            line.Position = curr.Position;
                        }
                        else
                        {
                            line.Position = curr.Position2;
                        }
                    }
                    else if (line.Position == endpoint)
                    {
                        if ((line.Position2 - curr.Position).Length < (line.Position2 - curr.Position2).Length)
                        {
                            line.Position2 = curr.Position;
                        }
                        else
                        {
                            line.Position2 = curr.Position;
                        }
                    }
                    else
                    {
                        throw new Exception("Endpoint does not match line position in snap");
                    }
                    if (line is StandardLine)
                        trk.TryConnectLines((StandardLine)line, (StandardLine)curr);

                    break;
                }
            }
        }
        public Line SnapLine(TrackWriter trk, Vector2d pos)
        {
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)pos - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    true);

            var eraser = new Vector2d(0.5, 0.5);
            var fr = new FloatRect((Vector2)(pos - eraser), (Vector2)(eraser * 2));
            for (var i = 0; i < lines.Count; i++)
            {
                if (Line.DoesLineIntersectRect(lines[i], fr))
                {
                    return lines[i];
                }
            }
            return null;
        }
        public Line Snap(TrackReader trk, Vector2d pos, Line ignore = null)
        {
            Line closest = null;
            double dist = 8 / Math.Min(10, game.Track.Zoom);
            float sq = (float)(dist * 2);
            var lines =
                trk.GetLinesInRect(new FloatRect((Vector2)pos - new Vector2(sq, sq), new Vector2(sq * 2, sq * 2)),
                    true);

            for (var i = 0; i < lines.Count; i++)
            {
                var sl = lines[i];
                if (sl != null)
                {
                    if (ignore == sl)
                        continue;
                    var cmpdist1 = (sl.Position - pos).Length;
                    var cmpdist2 = (sl.Position2 - pos).Length;
                    if (cmpdist1 < dist)
                    {
                        dist = cmpdist1;
                        closest = sl;
                    }
                    if (cmpdist2 < dist)
                    {
                        dist = cmpdist2;
                        closest = sl;
                    }
                }
            }
            if (ignore != null && dist != 0)
            {
                closest = null;
            }
            return closest;
        }
        public static Vector2d SnapXY(Vector2d start, Vector2d end)
        {
            var diff = end - start;
            var angle = MathHelper.RadiansToDegrees(Math.Atan2(diff.Y, diff.X)) + 90;
            if (angle < 0)
                angle += 360;
            const double deg = 45;
            if (angle >= 360 - (deg / 2) || angle <= (deg / 2))
            {
                end.X = start.X;
            }
            else if (angle >= 45 - (deg / 2) && angle <= 45 + (deg / 2))
            {
                end.X = start.X - (end.Y - start.Y);
            }
            else if (angle >= 90 - (deg / 2) && angle <= 90 + (deg / 2))
            {
                end.Y = start.Y;
            }
            else if (angle >= 135 - (deg / 2) && angle <= 135 + (deg / 2))
            {
                end.Y = start.Y + (end.X - start.X);
            }
            else if (angle >= 180 - (deg / 2) && angle <= 180 + (deg / 2))
            {
                end.X = start.X;
            }
            else if (angle >= 225 - (deg / 2) && angle <= 225 + (deg / 2))
            {
                end.X = start.X - (end.Y - start.Y);
            }
            else if (angle >= 270 - (deg / 2) && angle <= 270 + (deg / 2))
            {
                end.Y = start.Y;
            }
            else if (angle >= 315 - (deg / 2) && angle <= 315 + (deg / 2))
            {
                end.Y = start.Y + (end.X - start.X);
            }
            return end;
        }

        protected Vector2d MouseCoordsToGame(Vector2d mouse)
        {
            var p = game.ScreenPosition + (mouse / game.Track.Zoom);
            return p;
        }
    }
}