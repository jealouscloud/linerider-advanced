//
//  EraserTool.cs
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

namespace linerider
{
    public class EraserTool : Tool
    {
        private bool started;

        public override MouseCursor Cursor
        {
            get { return game.Cursors["eraser"]; }
        }

        public EraserTool() : base()
        {
        }

        public override void OnMouseDown(Vector2d pos)
        {
            started = true;
            var p = MouseCoordsToGame(pos);
            Erase(p);
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (started)
            {
                var p = MouseCoordsToGame(pos);
                Erase(p);
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            if (started)
            {
                started = false;
                var p = MouseCoordsToGame(pos);
                Erase(p);
            }
            base.OnMouseUp(pos);
        }

        private void Erase(Vector2d pos)
        {
            //todo implement eraser
        //    game.Track.Erase(pos, game.Canvas.ColorControls.Selected);
        }

        public override void Stop()
        {
            started = false;
        }
    }
}