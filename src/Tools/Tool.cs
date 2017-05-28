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
using OpenTK;

namespace linerider
{
    public abstract class Tool : GameService
    {
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
        public Line SnapLine(Vector2d pos)
        {
            var lines =
                game.Track.GetLinesInRect(new FloatRect((Vector2)pos - new Vector2(24, 24), new Vector2(24 * 2, 24 * 2)),
                    true);

            var eraser = new Vector2d(0.5,0.5);
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
        public Line Snap(Vector2d pos, Line ignore = null)
        {
            Line closest = null;
            double dist = 8 / Math.Min(10, game.Track.Zoom);
            float sq = (float)(dist * 2);
            var lines =
                game.Track.GetLinesInRect(new FloatRect((Vector2)pos - new Vector2(sq, sq), new Vector2(sq * 2, sq * 2)),
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