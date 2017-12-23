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
        public static bool ScaleCamera = true;
        public CameraBoundingBox framebox = new CameraBoundingBox();
        public CameraLocation Location = new CameraLocation(Vector2d.Zero, Vector2d.Zero);
        private Vector2d nextrider;
        private Vector2d lastcenter;
        private Stack<CameraLocation> camerastack = new Stack<CameraLocation>();
        private DoubleRect viewport;

        public void SetFrame(Vector2d newcenter, bool relative)
        {
            lastcenter = GetCameraCenter();
            framebox.RiderPosition = newcenter;
            if (!relative)
            {
                Location = new CameraLocation(newcenter, Vector2d.Zero);
                lastcenter = Vector2d.Zero;
            }
            else
            {
                if (Settings.SmoothCamera)
                {
                    Location = framebox.SmoothClamp(lastcenter, 0);
                }
                else
                {
                    Location = framebox.Clamp(lastcenter);
                }
            }
        }
        public void SetPrediction(Vector2d nextframe)
        {
            nextrider = nextframe;
        }

        public DoubleRect GetViewport()
        {
            return viewport;
        }

        public void BeginFrame(float blend)
        {
            var rendersize = game.RenderSize;
            var sz = new Vector2d(rendersize.Width / game.Track.Zoom, rendersize.Height / game.Track.Zoom);
            var camcenter = GetCameraCenter();
            if (blend != 1 && lastcenter != Vector2d.Zero)
            {
                camcenter = Vector2d.Lerp(lastcenter, camcenter, blend);
            }
            camcenter -= sz / 2;
            viewport = new DoubleRect(camcenter, sz);
        }

        public void Push()
        {
            camerastack.Push(Location);
        }

        public void Pop()
        {
            SetFrame(camerastack.Pop().GetPosition(),false);
        }
        public DoubleRect getclamp(float zoom, float width, float height)
        {
            var ret = GetViewport();
            var pos = ret.Vector + (ret.Size / 2);
            var b = new CameraBoundingBox() { RiderPosition = pos };
            return b.GetBox(framebox.GetSmoothCamRatio((float)game.Track.RiderState.CalculateMomentum().Length));
        }
        private Vector2d GetCameraCenter()
        {
            var camcenter = Location.GetPosition();
            if (game.Track.Animating)
            {
                if (Settings.SmoothCamera)
                {
                    CameraBoundingBox nextbox = new CameraBoundingBox() { RiderPosition = nextrider };
                    if (!nextbox.SmoothIntersects(camcenter, 0))//next frame ideal exceeds bounds of current camera, so we should tend towards it for predictive camera.
                    {
                        camcenter = nextbox.SmoothClamp(Location.GetPosition(), 0).GetPosition();
                    }
                    if (ScaleCamera)
                    {
                        var ppf = (float)game.Track.RiderState.CalculateMomentum().Length;
                        var d = nextrider - framebox.RiderPosition;
                        camcenter = framebox.SmoothClamp(camcenter, !framebox.SmoothIntersects(nextrider, 10000) ? 10000 : ppf).GetPosition();//basically, don't allow it to rubber band
                    }
                    else
                    {
                        camcenter = framebox.SmoothClamp(camcenter, 0).GetPosition();
                    }
                }
                else
                {
                    camcenter = framebox.Clamp(camcenter).GetPosition();
                }
            }
            return camcenter;
        }
    }
}
