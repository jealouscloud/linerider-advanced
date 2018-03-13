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
using System;
using linerider.Utils;

namespace linerider.Tools
{
    public class EraserTool : Tool
    {
        public override bool RequestsMousePrecision
        {
            get
            {
                return false;
            }
        }
        private Vector2d _last_erased = Vector2d.Zero;
        // todo, this + the circle function dont work at ultra zoomed out.
        private float radius => 8 / game.Track.Zoom;
        private bool _actionmade;
        public override MouseCursor Cursor
        {
            get { return game.Cursors["eraser"]; }
        }

        public EraserTool() : base()
        {
        }

        public override void OnMouseDown(Vector2d pos)
        {
            Stop();
            Active = true;
            var p = ScreenToGameCoords(pos);
            game.Track.UndoManager.BeginAction();
            Erase(p);
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (Active)
            {
                var p = ScreenToGameCoords(pos);
                var diff = (Vector2)(p - _last_erased);
                var len = diff.LengthFast;
                double steplen = radius * 2;
                if (len >= steplen)
                {
                    // calculate intermediary lines we might have missed
                    var v = Angle.FromLine(_last_erased, p);
                    var current = _last_erased;
                    int count = (int)(len / steplen);
                    for (int i = 0; i < count; i++)
                    {
                        Erase(current);
                        current += new Vector2d(v.Cos * steplen, v.Sin * steplen);
                    }
                }
                Erase(p);
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            if (Active)
            {
                var p = ScreenToGameCoords(pos);
                Erase(p);
                Stop();
            }
            base.OnMouseUp(pos);
        }

        private void Erase(Vector2d pos)
        {
            using (var trk = game.Track.CreateTrackWriter())
            {
                var lines = LinesInRadius(trk, pos, radius);
                if (lines.Length != 0)
                {
                    var linefilter = game.Canvas.ColorControls.Selected;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (linefilter == LineType.All || lines[i].Type == linefilter)
                        {
                            _actionmade = true;
                            trk.RemoveLine(lines[i]);
                        }
                    }
                    game.Track.NotifyTrackChanged();
                }
                _last_erased = pos;
            }
        }

        public override void Stop()
        {
            if (Active)
            {
                if (_actionmade)
                {
                    game.Track.UndoManager.EndAction();
                }
                else
                {
                    game.Track.UndoManager.CancelAction();
                }
                _actionmade = false;
            }
            Active = false;
        }
    }
}