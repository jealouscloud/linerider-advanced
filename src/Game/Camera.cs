//
//  Camera.cs
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
using System.Text;
using linerider.Game;
using linerider.Drawing;
using OpenTK;
namespace linerider.Game
{
    public class Camera : GameService
    {
        public struct Position
        {
            public Vector2d Center;
            public Vector2d AnimationOffset;
            public Position(Vector2d pos)
            {
                Center = pos;
                AnimationOffset = Vector2d.Zero;
            }
            public Position(Vector2d pos, Vector2d offset)
            {
                Center = pos;
                AnimationOffset = offset;
            }
        }
        public Position Location;
        public Position NextLocation;
        private Stack<Position> camerastack = new Stack<Position>();
        public static Vector2d EllipseClamp(FloatRect rect, Vector2d position)
        {
            var center = (Vector2d)(rect.Vector + (rect.Size / 2));
            var a = rect.Width / 2;
            var b = rect.Height / 2;
            var p = position - center;
            var d = p.X * p.X / (a * a) + p.Y * p.Y / (b * b);

            if (d > 1)
            {
                Tools.Angle angle = Tools.Angle.FromLine(center, position);
                double t = Math.Atan((rect.Width / 2) * Math.Tan(angle.Radians)
                                     / (rect.Height / 2));
                if (angle.Degrees <= 270 && angle.Degrees >= 90)
                {
                    t += Math.PI;
                }
                Vector2d ptfPoint =
                   new Vector2d(center.X + (rect.Width / 2) * Math.Cos(t),
                               center.Y + (rect.Height / 2) * Math.Sin(t));

                position = ptfPoint;
            }
            return position;
        }
        public Vector2d UpdateClamp(Vector2d ridercenter, Vector2d targetcenter)
        {
            var rendersize = game.RenderSize;
            var sz = new Vector2(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom);
            float szcam = Settings.Default.SmoothCamera ? 0.2f : 0.125f;
            var clampbounds = new FloatRect(((Vector2)ridercenter - (sz * szcam)), sz * szcam * 2);
            Vector2d clamped;
            if (Settings.Default.SmoothCamera)
            {
                clamped = EllipseClamp(clampbounds, targetcenter);
            }
            else
            {
                clamped = (Vector2d)clampbounds.Clamp((Vector2)targetcenter);
            }
            return clamped;
        }
        public void SetFrame(Vector2d newcenter, bool relative)
        {
            if (!relative)
            {
                Location = new Position(newcenter);
            }
            else
            {
                var clamp = UpdateClamp(newcenter, GetCenter());
                Location = new Position(newcenter, (clamp - newcenter) * game.Track.Zoom);
            }
        }
        public void SetSmoothFrame(Vector2d currentframe, Vector2d nextframe)
        {
            var pos1 = UpdateClamp(currentframe, GetCenter());
            var pos2 = UpdateClamp(nextframe, pos1);
            Location = new Position(currentframe, (pos1 - currentframe) * game.Track.Zoom);
            NextLocation = new Position(nextframe, (pos2 - nextframe) * game.Track.Zoom);
        }
        public FloatRect GetCamera(float blend)
        {
            var rendersize = game.RenderSize;
            var sz = new Vector2d(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom);
            var camcenter = GetCenter();
            if (Settings.Default.SmoothCamera && game.Track.Animating)
            {
                var nextcenter = camcenter + ((NextLocation.Center - camcenter) * 2);
                var clampedprediction = UpdateClamp(camcenter,nextcenter);
                if (clampedprediction != NextLocation.Center)//next frame ideal exceeds bounds of current camera, so we should tend towards it for predictive camera.
                {
                    var nextridercenter = NextLocation.Center + (NextLocation.AnimationOffset / game.Track.Zoom);
                    var predcamera = UpdateClamp(Location.Center, nextridercenter);
                    camcenter += (predcamera - camcenter) * blend;
                }
            }
            camcenter -= sz / 2;
            return new FloatRect((Vector2)camcenter, (Vector2)sz);
        }
        public FloatRect GetCamera()
        {
            return GetCamera(1);
        }
        public void Push()
        {
            camerastack.Push(Location);
        }
        public void Pop()
        {
            Location = camerastack.Pop();
        }

        public FloatRect getclamp(float zoom, float width, float height)
        {
            var rendersize = game.RenderSize;
            float szcam = Settings.Default.SmoothCamera ? 0.2f : 0.125f;
            var sz = new Vector2(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom) * szcam * 2;
            var viewpos = GetCenter();
            var pos = (Vector2)viewpos;
            pos -= sz / 2;
            return new FloatRect(pos, sz);
        }
        public Vector2d GetCenter()
        {
            var viewpos = Location.Center;
            if (game.Track.Animating)
            {
                viewpos += Location.AnimationOffset / game.Track.Zoom;
            }
            return viewpos;
        }
    }
}
