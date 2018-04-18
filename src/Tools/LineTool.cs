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

using linerider.Rendering;
using OpenTK;
using System;
using Color = System.Drawing.Color;
using OpenTK.Input;
using linerider.Game;

namespace linerider.Tools
{
    public class LineTool : Tool
    {
        public override MouseCursor Cursor
        {
            get { return game.Cursors["line"]; }
        }
        
        public override bool ShowSwatch
        {
            get
            {
                return true;
            }
        }
        public bool Snapped = false;
        private const float MINIMUM_LINE = 0.01f;
        private bool _addflip;
        private Vector2d _end;
        private Vector2d _start;

        public LineTool()
            : base()
        {
        }

        public override void OnChangingTool()
        {
            Stop();
        }
        public override void OnMouseDown(Vector2d pos)
        {
            Active = true;
            var gamepos = ScreenToGameCoords(pos);
            if (game.EnableSnap)
            {
                using (var trk = game.Track.CreateTrackReader())
                {
                    var snap = TrySnapPoint(trk, gamepos, out bool success);
                    if (success)
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
                _start = gamepos;
                Snapped = false;
            }


            _addflip = UI.InputUtils.Check(UI.Hotkey.LineToolFlipLine);
            _end = _start;
            game.Invalidate();
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (Active)
            {
                _end = ScreenToGameCoords(pos);
                if (game.ShouldXySnap())
                {
                    _end = Utility.SnapToDegrees(_start, _end);
                }
                else if (game.EnableSnap)
                {
                    using (var trk = game.Track.CreateTrackReader())
                    {
                        var snap = TrySnapPoint(trk, _end, out bool snapped);
                        if (snapped && snap != _start)
                        {
                            _end = snap;
                        }
                    }
                }
                game.Invalidate();
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            game.Invalidate();
            if (Active)
            {
                Active = false;
                var diff = _end - _start;
                var x = diff.X;
                var y = diff.Y;
                if (Math.Abs(x) + Math.Abs(y) < MINIMUM_LINE)
                    return;
                if (game.ShouldXySnap())
                {
                    _end = Utility.SnapToDegrees(_start, _end);
                }
                else if (game.EnableSnap)
                {
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        var snap = TrySnapPoint(trk, _end, out bool snapped);
                        if (snapped && snap != _start)
                        {
                            _end = snap;
                        }
                    }
                }
                if ((_end - _start).Length >= MINIMUM_LINE)
                {
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        game.Track.UndoManager.BeginAction();
                        var added = CreateLine(trk, _start, _end, _addflip, Snapped, game.EnableSnap);
                        game.Track.UndoManager.EndAction();
                        if (added is StandardLine)
                        {
                            game.Track.NotifyTrackChanged();
                        }
                    }
                    game.Invalidate();
                }
            }
            Snapped = false;
            base.OnMouseUp(pos);
        }
        public override void Render()
        {
            base.Render();
            if (Active)
            {
                var diff = _end - _start;
                var x = diff.X;
                var y = diff.Y;
                Color c = Color.FromArgb(200, 150, 150, 150);
                if (Math.Abs(x) + Math.Abs(y) < MINIMUM_LINE)
                {
                    c = Color.Red;
                    var sz = 2f;
                    if (Swatch.Selected == LineType.Scenery)
                        sz *= Swatch.GreenMultiplier;
                    GameRenderer.RenderRoundedLine(_start, _end, c, sz);
                }
                else
                {
                    switch (Swatch.Selected)
                    {
                        case LineType.Blue:
                            StandardLine sl = new StandardLine(_start, _end, _addflip);
                            sl.CalculateConstants();
                            GameRenderer.DrawTrackLine(sl, c, Settings.Local.RenderGravityWells, true);
                            break;

                        case LineType.Red:
                            RedLine rl = new RedLine(_start, _end, _addflip);
                            rl.Multiplier = Swatch.RedMultiplier;
                            rl.CalculateConstants();
                            GameRenderer.DrawTrackLine(rl, c, Settings.Local.RenderGravityWells, true);
                            break;

                        case LineType.Scenery:
                            GameRenderer.RenderRoundedLine(_start, _end, c, 2 * Swatch.GreenMultiplier);
                            break;
                    }
                }
            }
        }

        public override void Stop()
        {
            Active = false;
        }
    }
}