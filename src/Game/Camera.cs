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
            public Vector2d Camera;
            public float Zoom;
            public Position(Vector2d pos)
            {
                Center = pos;
                Camera = pos;
                Zoom = 1;
            }
            public Position(Vector2d pos, Vector2d cam, float zoom)
            {
                Center = pos;
                Camera = cam;
                Zoom = zoom;
            }
        }
        public Position Location;
        public Position NextLocation;
        public Position LastLocation;
        private Stack<Position> camerastack = new Stack<Position>();
        private const float legacyratio = 0.125f;
        private float maxcamratio = 0.2f;

        public Vector2d GetOrigin()
        {
            return GetOrigin(Location);
        }

        public Vector2d GetOrigin(Position pos)
        {
            var viewpos = pos.Center;
            if (game.Track.Animating)
            {
                viewpos += ((pos.Camera - pos.Center) * pos.Zoom) / game.Track.Zoom;
                if (Math.Abs((viewpos - pos.Camera).Length) >= 1 / game.Track.Zoom)
                    return viewpos;
                return pos.Camera;
            }
            return viewpos;
        }

        public void SetFrame(Vector2d newcenter, bool relative)
        {
            if (!relative)
            {
                Location = new Position(newcenter);
            }
            else
            {
                var clamp = ClampAroundOrigin(newcenter, GetOrigin());
                Location = new Position(newcenter, clamp, game.Track.Zoom);
            }
        }
        private Vector2d Scale(Vector2d input, double min, double max, double scalemin, double scalemax)
        {
            max -= min;
            var xratio = (input.X - min) / max;
            var yratio = (input.Y - min) / max;
            scalemax -= scalemin;
            return new Vector2d((xratio * scalemax) + scalemin, (yratio * scalemax) + scalemin);
        }
        public void SetPrediction(Vector2d nextframe)
        {
            NextLocation = new Position(nextframe, ClampAroundOrigin(nextframe, Location.Camera), game.Track.Zoom);
            var camcenter = GetOrigin();
            var difference = NextLocation.Center - camcenter;
            var check = camcenter + difference;
            var clampedprediction = ClampAroundOrigin(camcenter, check);
            if (clampedprediction != check)//next frame ideal exceeds bounds of current camera, so we should tend towards it for predictive camera.
            {
                var nextcenter = GetOrigin(NextLocation);
                var camdifference = nextcenter - camcenter;
                var predcamera = ClampAroundOrigin(Location.Center, nextcenter, true);
                var predictiondirection = predcamera - camcenter;
                var ppf = nextframe - Location.Center;
                var log = Math.Log(predictiondirection.Length,10);
                predictiondirection = predictiondirection * log;
                NextLocation.Camera = camcenter + predictiondirection; ;
            }
        }

        public DoubleRect GetRenderArea(float blend = 1)
        {
            var rendersize = game.RenderSize;
            var sz = new Vector2d(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom);
            var camcenter = GetOrigin();
            if (Settings.Default.SmoothCamera && game.Track.Animating)
            {
                var clampedprediction = ClampAroundOrigin(camcenter, NextLocation.Center);
                if (clampedprediction != NextLocation.Center)//next frame ideal exceeds bounds of current camera, so we should tend towards it for predictive camera.
                {
                    var nextcenter = GetOrigin(NextLocation);
                    var predcamera = ClampAroundOrigin(Location.Center, camcenter, true);
                    camcenter = Vector2d.Lerp(camcenter, predcamera, 1);
                }
            }
            camcenter -= sz / 2;
            return new DoubleRect(camcenter, sz);
        }

        public void Push()
        {
            camerastack.Push(Location);
        }

        public void Pop()
        {
            Location = camerastack.Pop();
        }

        public DoubleRect getclamp(float zoom, float width, float height)
        {
            var rendersize = game.RenderSize;
            float camratio = GetCameraRatio();
            var sz = new Vector2d(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom) * camratio * 2;
            var ret = GetRenderArea();
            var pos = new Vector2d(ret.Left + (ret.Width / 2), ret.Top + (ret.Height / 2));
            pos -= sz / 2;
            return new DoubleRect(pos, sz);
        }

        private Vector2d ClampAroundOrigin(Vector2d camorigin, Vector2d nextcenter, bool allowshrink = false)
        {
            var rendersize = game.RenderSize;
            var sz = new Vector2d(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom);
            var camratio = Settings.Default.SmoothCamera ? maxcamratio : legacyratio;
            if (allowshrink)
                camratio = GetCameraRatio();
            var clampbounds = new DoubleRect(camorigin - (sz * camratio), sz * camratio * 2);
            Vector2d clamped;
            if (Settings.Default.SmoothCamera)
            {
                var ppf = nextcenter - camorigin;
                var oval = clampbounds.EllipseClamp(nextcenter);
                var square = clampbounds.Clamp(nextcenter);
                clamped = Vector2d.Lerp(square, oval, LiveTestFile.GetValue(3));//apply 75% roundness
            }
            else
            {
                clamped = clampbounds.Clamp(nextcenter);
            }
            return clamped;
        }

        private float GetCameraRatio()
        {
            if (Settings.Default.SmoothCamera)
            {
                int floor = LiveTestFile.GetValueInt(0);
                int ceil = LiveTestFile.GetValueInt(1);
                var ppf = Math.Max(0, game.Track.RiderState.CalculateMomentum().Length - floor);
                var scale1 = (Math.Min(ceil, ppf) / ceil);
                var cam = maxcamratio - (scale1 * (maxcamratio * 0.4));
                return (float)cam;
            }
            else
            {
                return legacyratio;
            }
        }
    }
}
