//
//  LineAdjustTool.cs
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

using Gwen.Controls;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace linerider
{
    public class LineAdjustTool : Tool
    {
        #region Fields

        public bool LifeLock = false;
        public bool CanLifelock = false;

     //   private Joint _joint = 0;
        private bool _started = false;

        #endregion Fields

        #region Properties

        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }

        public bool Started
        {
            get
            {
                return _started;
            }
        }

        #endregion Properties

        #region Constructors

        public LineAdjustTool()
        {
        }

        #endregion Constructors

        #region Methods

        public void Deselect()
        {
        }

        public void MoveLine(Vector2d pos)
        {
        }

        public override void OnMouseDown(Vector2d pos)
        {
            using (var trk = game.Track.CreateTrackWriter())
            {
                this.SelectLine(trk,pos);
            }
            base.OnMouseDown(pos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                MoveLine(pos);
                if (_started)//moveline can call stop, so check again
                {
                    game.Canvas.RemoveTooltip(null);
               //     UpdateTooltip();
                }
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            base.OnMouseRightDown(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            base.OnMouseUp(pos);
        }

        public override void Stop()
        {
        }

        private Vector2d AngleLock(Vector2d pt, Vector2d pos, Line l)
        {
            var diff = l.Position2 - l.Position;
            var angle = Math.Atan2(diff.Y, diff.X);
            var delta = pt - pos;
            var ret = new Vector2d(Math.Cos(angle), Math.Sin(angle));
            return (new Vector2d(ret.X, ret.Y) * Vector2d.Dot(delta, ret)) + pos;
        }

        private Vector2d LengthLock(Vector2d p1, Vector2d p2, Line l)
		{
			var diff = l.Position2 - l.Position;
            var diff2 = p2 - p1;
            if (diff.Length != diff2.Length)
            {
                var angle = Math.Atan2(diff2.Y, diff2.X);
                Drawing.Turtle turtle = new Drawing.Turtle(p1);
                turtle.Move(Tools.Angle.FromRadians(angle).Degrees, diff.Length);
                return turtle.Point;
            }
            return p2;
        }

        #endregion Methods

        #region Classes

        private class NoDecimalNUD : NumericUpDown
        {
            #region Constructors

            public NoDecimalNUD(ControlBase b) : base(b)
            {
            }

            #endregion Constructors

            #region Methods

            protected override bool IsTextAllowed(string str)
            {
                return base.IsTextAllowed(str) && !str.Contains(".");
            }

            #endregion Methods
        }

        #endregion Classes

        #region Enums

        private enum Joint
        {
            Left = 1,
            Right = 2,
            Both = 3
        }

        #endregion Enums
    }
}