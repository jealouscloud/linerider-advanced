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
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Color = System.Drawing.Color;
using linerider.Drawing;

namespace linerider
{

    public class PencilTool : Tool
    {
        public override bool NeedsRender
        {
            get
            {
                return true;
            }
        }
        private Vector2d _start;
        private Vector2d _end;
        private bool _started = false;
        const float MINIMUM_LINE = 0.1f;
        private StandardLine _last = null;
        private bool inv = false;
        private Vector2d _mouseshadow;
        private Vector2d _lastrendered;
        public override MouseCursor Cursor
        {
            get { return game.Cursors["pencil"]; }
        }
        public PencilTool()
            : base()
        {
        }
        public override void OnMouseDown(Vector2d pos)
        {
            _started = true;
            _last = null;

            if (game.EnableSnap)
            {
                var gamepos = MouseCoordsToGame(pos);
                var ssnap = Snap(gamepos);
                var snap = ssnap as StandardLine;

                if (snap != null)
                {
                    _start = (snap.Start - gamepos).Length < (snap.End - gamepos).Length ? snap.Start : snap.End;
                    _last = snap;
                }
                else if (ssnap != null)
                {
                    _start = (ssnap.Position - gamepos).Length < (ssnap.Position2 - gamepos).Length
                        ? ssnap.Position
                        : ssnap.Position2;
                }
                else
                {
                    _start = gamepos;
                }
            }
            else
            {
                _start = MouseCoordsToGame(pos);
            }
            var state = OpenTK.Input.Keyboard.GetState();
            inv = state[OpenTK.Input.Key.ShiftLeft] || state[OpenTK.Input.Key.ShiftRight];
            _end = _start;
            game.Invalidate();
            base.OnMouseDown(pos);
        }
        private void AddLine()
        {
            Line added = null;
            switch (game.Canvas.ColorControls.Selected)
            {
                case LineType.Blue:
                    added = new StandardLine(_start, _end, false) { inv = inv };
                    added.CalculateConstants();
                    break;

                case LineType.Red:
                    added = new RedLine(_start, _end, false) { inv = inv };
                    (added as RedLine).Multiplier = game.Canvas.ColorControls.RedMultiplier;
                    added.CalculateConstants();
                    break;

                case LineType.Scenery:
                    added = new SceneryLine(_start, _end) { Width = game.Canvas.ColorControls.GreenMultiplier };
                    break;
            }
            game.Track.AddLine(added);
            if (added is StandardLine)
            {
                var stl = added as StandardLine;
                game.Track.TryConnectLines(stl, _last);
                _last = stl;
            }
            if (game.Canvas.ColorControls.Selected != LineType.Scenery)
            {
                game.Track.ChangeMade(_start, _end);
                game.Track.TrackUpdated();
            }
            game.Invalidate();
        }
        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                _end = MouseCoordsToGame(pos);
                var diff = _end - _start;
                var x = diff.X;
                var y = diff.Y;
                if (Math.Abs(x) + Math.Abs(y) >= MINIMUM_LINE / game.Track.Zoom)
                {
                    AddLine();
                    _start = _end;
                }
                game.Invalidate();
            }

            _mouseshadow = MouseCoordsToGame(pos);
            base.OnMouseMoved(pos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            game.Invalidate();
            if (_started)
            {
                _started = false;
                var diff = _end - _start;
                var x = diff.X;
                var y = diff.Y;
                if (Math.Abs(x) + Math.Abs(y) < MINIMUM_LINE / game.Track.Zoom && (_end != _start || game.Canvas.ColorControls.Selected != LineType.Scenery))
                    return;
                AddLine();
                _last = null;
            }
            base.OnMouseUp(pos);
        }
        public override void Render()
        {
            base.Render();
            if (game.Canvas.ColorControls.Selected == LineType.Scenery)
            {
                GameRenderer.RenderRoundedLine(_mouseshadow, _mouseshadow, Color.FromArgb(100,0x00, 0xCC, 0x00), 2f * game.Canvas.ColorControls.GreenMultiplier, false, false);
            }
            _lastrendered = _mouseshadow;
        }
        public override void Stop()
        {
            _started = false;
        }
    }
}
