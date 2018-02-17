//
//  HandTool.cs
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
using OpenTK.Input;

namespace linerider.Tools
{
    public class HandTool : Tool
    {
        private Vector2d CameraStart;
        private Vector2d CameraTarget;
        private Vector2d startposition;
        private Vector2d lastposition;
        private bool zoom = false;

        public override MouseCursor Cursor
        {
            get
            {
                if (Active)
                {
                    return zoom ? game.Cursors["zoom"] : game.Cursors["closed_hand"];
                }
                return game.Cursors["hand"];
            }
        }

        public HandTool() : base()
        {
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            zoom = true;
            Active = true;
            startposition = pos;
            lastposition = startposition;
            CameraStart = game.Track.Camera.GetCameraCenter();
            CameraTarget = ScreenToGameCoords(pos);
            game.Invalidate();
            game.UpdateCursor();
            base.OnMouseRightDown(pos);
        }
        public override void OnMouseDown(Vector2d pos)
        {
            zoom = false;
            Active = true;
            startposition = pos;// / game.Track.Zoom;
            CameraStart = game.Track.Camera.GetCameraCenter();
            game.Invalidate();
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (Active)
            {
                if (zoom)
                {
                    game.Zoom((float)(0.02 * (lastposition.Y - pos.Y)));
                    lastposition = pos;
                }
                else
                {
                    var newcenter = 
                        CameraStart - 
                        ((pos / game.Track.Zoom) - 
                        (startposition / game.Track.Zoom));
                    game.Track.Camera.SetFrameCenter(newcenter);
                }
                game.Invalidate();
            }
            base.OnMouseMoved(pos);
        }
        public override void OnMouseRightUp(Vector2d pos)
        {
            Active = false;
            base.OnMouseRightUp(pos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            Active = false;
            base.OnMouseUp(pos);
        }

        public override void Stop()
        {
            Active = false;
            base.Stop();
        }
    }
}