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

using OpenTK;
using OpenTK.Input;

namespace linerider
{
    public class HandTool : Tool
    {
        private Vector2d Start;
        private bool started;
        private Vector2d startposition;
        public override MouseCursor Cursor => Mouse.GetState().IsButtonDown(MouseButton.Left)
            ? game.Cursors["closed_hand"]
            : game.Cursors["hand"];

        public HandTool() : base()
        {
        }

        public override void OnMouseDown(Vector2d pos)
        {
            started = true;
            startposition = pos / game.Track.Zoom;
            Start = game.Track.Camera.GetCenter(); 
            game.Invalidate();
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (started)
            {
                game.Track.Camera.SetFrame(Start - ((pos / game.Track.Zoom) - startposition),false);
                game.Invalidate();
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            started = false;
            base.OnMouseUp(pos);
        }

        public override void Stop()
        {
            base.Stop();
            started = false;
        }
    }
}