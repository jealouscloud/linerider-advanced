//
//  LineTool.cs
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

using linerider.Drawing;
using OpenTK;
using System;
using Color = System.Drawing.Color;
using OpenTK.Input;

namespace linerider
{
    public class LineTool : Tool
    {
        #region Properties

        public override MouseCursor Cursor
        {
            get { return game.Cursors["line"]; }
        }

        #endregion Properties

        #region Constructors

        public LineTool()
            : base()
        {
        }

        #endregion Constructors

        #region Methods

        public override void OnMouseDown(Vector2d pos)
        {
            _started = true;
            var gamepos = MouseCoordsToGame(pos);
            if (game.EnableSnap)
            {
                var ssnap = Snap(gamepos);
                var snap = ssnap as StandardLine;
                if (snap != null)
                {
                    _start = (snap.CompliantPosition - gamepos).Length < (snap.CompliantPosition2 - gamepos).Length ? snap.CompliantPosition : snap.CompliantPosition2;
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
                _start = gamepos;
            }

            var state = OpenTK.Input.Keyboard.GetState();
            _addflip = state[OpenTK.Input.Key.LShift] || state[OpenTK.Input.Key.RShift];
            _end = _start;
            game.Invalidate();
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                _end = MouseCoordsToGame(pos);
                if (game.ShouldXySnap())
                {
                    _end = SnapXY(_start, _end);
                }
                game.Invalidate();
            }
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
                if (Math.Abs(x) + Math.Abs(y) < MINIMUM_LINE)
                    return;
                StandardLine next = null;
                if (game.ShouldXySnap())
                {
                    _end = SnapXY(_start, _end);
                }
                else if (game.EnableSnap)
                {
                    var ssnap = Snap(_end);
                    var snap = ssnap as StandardLine;
                    if (snap != null)
                    {
                        var old = _end;

                        _end = (snap.CompliantPosition - _end).Length < (snap.CompliantPosition2 - _end).Length ? snap.CompliantPosition : snap.CompliantPosition2;
                        if (_end == _start)
                            _end = old;
                        else
                        {
                            next = snap;
                        }
                    }
                    else if (ssnap != null)
                    {
                        var old = _end;
                        _end = (ssnap.Position - _end).Length < (ssnap.Position2 - _end).Length
    ? ssnap.Position
    : ssnap.Position2;
                        if (_end == _start)
                            _end = old;
                    }
                }
                if ((_end - _start).Length >= MINIMUM_LINE)
                {
                    Line added = null;
                    switch (game.Canvas.ColorControls.Selected)
                    {
                        case LineType.Blue:
                            added = new StandardLine(_start, _end, _addflip);
                            break;

                        case LineType.Red:
                            added = new RedLine(_start, _end, _addflip);
                            (added as RedLine).Multiplier = game.Canvas.ColorControls.RedMultiplier;
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
                        game.Track.TryConnectLines(stl, next);
                    }
                    if (game.Canvas.ColorControls.Selected != LineType.Scenery)
                    {
                        game.Track.TrackUpdated();
                    }
                    game.Invalidate();
                }
            }
            base.OnMouseUp(pos);
        }
        public override void Render()
        {
            base.Render();
            if (_started)
            {
                var diff = _end - _start;
                var x = diff.X;
                var y = diff.Y;
                Color c = Color.FromArgb(150, 150, 150);
                if (Math.Abs(x) + Math.Abs(y) < MINIMUM_LINE)
                    c = Color.Red;
                switch (game.Canvas.ColorControls.Selected)
                {
                    case LineType.Blue:
                        StandardLine sl = new StandardLine(_start, _end, _addflip);
                        sl.CalculateConstants();
                        GameRenderer.DrawTrackLine(sl, c, game.SettingRenderGravityWells, true, false, false);
                        break;

                    case LineType.Red:
                        RedLine rl = new RedLine(_start, _end, _addflip);
                        rl.Multiplier = game.Canvas.ColorControls.RedMultiplier;
                        rl.CalculateConstants();
                        GameRenderer.DrawTrackLine(rl, c, game.SettingRenderGravityWells, true, false, false);
                        break;

                    case LineType.Scenery:
                        GameRenderer.RenderRoundedLine(_start, _end, c, 1);
                        break;
                }
            }
        }
        public override bool OnKeyDown(Key k)
        {
            switch (k)
            {
                case Key.Left:
                    return false;
                case Key.Right:
                    return false;
            }
            return base.OnKeyDown(k);
        }

        public override void Stop()
        {
            _started = false;
        }

        #endregion Methods

        #region Fields

        private const float MINIMUM_LINE = 0.01f;
        private bool _addflip;
        private Vector2d _end;
        private StandardLine _last;
        private Vector2d _start;
        private bool _started = false;

        #endregion Fields
    }
}