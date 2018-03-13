//
//  PencilTool.cs
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
using OpenTK.Graphics.OpenGL;
using Color = System.Drawing.Color;
using linerider.Rendering;
using linerider.Game;

namespace linerider.Tools
{
    public class PencilTool : Tool
    {
        public override bool RequestsMousePrecision
        {
            get
            {
                return DrawingScenery;
            }
        }
        public override bool NeedsRender
        {
            get
            {
                return DrawingScenery || Active;
            }
        }
        public bool Snapped = false;
        private bool _drawn;
        private Vector2d _start;
        private Vector2d _end;
        const float MINIMUM_LINE = 0.6f;
        private bool _addflip = false;
        private Vector2d _mouseshadow;
        private bool DrawingScenery
        {
            get
            {
                return game.Canvas.ColorControls.Selected == LineType.Scenery;
            }
        }
        public override MouseCursor Cursor
        {
            get { return game.Cursors["pencil"]; }
        }
        public PencilTool() : base() { }
        public override void OnMouseDown(Vector2d pos)
        {
            Stop();
            Active = true;
            _drawn = false;

            if (game.EnableSnap)
            {
                var gamepos = ScreenToGameCoords(pos);
                using (var trk = game.Track.CreateTrackReader())
                {
                    var snap = TrySnapPoint(trk, gamepos, out bool snapped);
                    if (snapped)
                    {
                        _start = snap;
                        Snapped = true;
                    }
                    else
                    {
                        _start = gamepos;
                        Snapped = false;
                    }
                }
            }
            else
            {
                _start = ScreenToGameCoords(pos);
                Snapped = false;
            }
            _addflip = UI.InputUtils.Check(UI.Hotkey.LineToolFlipLine);
            _end = _start;
            game.Invalidate();
            game.Track.UndoManager.BeginAction();
            base.OnMouseDown(pos);
        }
        public override void OnChangingTool()
        {
            Stop();
            _mouseshadow = Vector2d.Zero;
        }
        private void AddLine()
        {
            _drawn = true;
            using (var trk = game.Track.CreateTrackWriter())
            {
                var added = CreateLine(trk, _start, _end, false, Snapped, false);
                if (added is StandardLine)
                {
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }
        public override void OnMouseMoved(Vector2d pos)
        {
            if (Active)
            {
                _end = ScreenToGameCoords(pos);
                var diff = _end - _start;
                var len = diff.Length;

                if ((DrawingScenery && len >= (MINIMUM_LINE / 2)) || len >= MINIMUM_LINE)
                {
                    AddLine();
                    _start = _end;
                    Snapped = true;//we are now connected to the newest line
                }
                game.Invalidate();
            }

            _mouseshadow = ScreenToGameCoords(pos);
            base.OnMouseMoved(pos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            game.Invalidate();
            if (Active)
            {
                _end = ScreenToGameCoords(pos);
                var diff = _end - _start;
                var len = diff.Length;

                if ((DrawingScenery && len >= (MINIMUM_LINE / 2)) || len >= MINIMUM_LINE)
                {
                    AddLine();
                }
                Stop();
            }
            base.OnMouseUp(pos);
        }
        public override void Render()
        {
            base.Render();
            if (DrawingScenery && _mouseshadow != Vector2d.Zero && !game.Track.Playing)
            {
                GameRenderer.RenderRoundedLine(_mouseshadow, _mouseshadow, Color.FromArgb(100, 0x00, 0xCC, 0x00), 2f * game.Canvas.ColorControls.GreenMultiplier, false, false);
            }
        }
        public override void Stop()
        {
            if (Active)
            {
                Active = false;

                if (_drawn)
                {
                    game.Track.UndoManager.EndAction();
                }
                else
                {
                    game.Track.UndoManager.CancelAction();
                }
            }
            _mouseshadow = Vector2d.Zero;
        }
    }
}