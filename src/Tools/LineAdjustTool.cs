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
using linerider.Lines;
using linerider.UI;
namespace linerider.Tools
{
    public class SelectTool : Tool
    {
        struct SelectInfo
        {
            public Vector2d start;
            public Line line;
            //     public Line snap;
            public bool leftjoint;
            public bool rightjoint;

        }
        public bool LifeLock = false;
        public bool CanLifelock = false;
        private SelectInfo _selection;
        private bool _started = false;
        private LineState _before;
        //   private LineState _before_snap;
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


        public SelectTool()
        {
        }


        public void Deselect()
        {
        }

        public void MoveSelection(Vector2d pos)
        {
            if (_selection.line != null)
            {
                var line = _selection.line;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    var left = _selection.leftjoint ? _before.Pos1 + (pos - _selection.start) : line.Position;
                    var right = _selection.rightjoint ? _before.Pos2 + (pos - _selection.start) : line.Position2;
                    if (_selection.leftjoint != _selection.rightjoint)
                    {
                        if (_selection.leftjoint)
                        {
                            if (UI.InputUtils.Check(Hotkey.ToolAngleLock))
                            {
                                //   movedleft = Utility.AngleLock(left,right,Angle(_selection.line.Position2 - _selection.line.Position)
                            }
                        }
                    }
                    trk.MoveLine(line,
                    left,
                    right);
                }
            }
            game.Invalidate();
        }

        public override void OnMouseDown(Vector2d mousepos)
        {
            Stop();//double check
            var gamepos = MouseCoordsToGame(mousepos);
            using (var trk = game.Track.CreateTrackWriter())
            {
                _selection = new SelectInfo();
                var line = SelectLine(trk, gamepos);
                if (line != null)
                {
                    _before = line.GetState();
                    var linerad = Line.GetLineRadius(line);
                    var point = Utility.CloserPoint(gamepos, line.Position, line.Position2);//TrySnapPoint(trk, gamepos);
                    //is it a knob?
                    if ((gamepos - point).Length <= linerad)
                    {
                        _selection.start = gamepos;
                        _selection.line = line;
                        _selection.leftjoint = line.Position == point;
                        if (!_selection.leftjoint /* todo test if we wanna drag line*/)
                        {
                            _selection.rightjoint = line.Position2 == point;
                        }
                    }
                    else
                    {
                        //select whole line
                    }
                }
                if (_selection.leftjoint || _selection.rightjoint)
                    _started = true;
            }
            base.OnMouseDown(gamepos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                MoveSelection(MouseCoordsToGame(pos));
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            base.OnMouseRightDown(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            Stop();
            base.OnMouseUp(pos);
        }

        public override void Stop()
        {
            _started = false;
            if (_selection.line != null)
            {
                game.Track.UndoManager.BeginAction();
                game.Track.UndoManager.AddChange(_before);
                game.Track.UndoManager.AddChange(_selection.line.GetState());
                game.Track.UndoManager.EndAction();
            }
            _selection.line = null;
        }
    }
}