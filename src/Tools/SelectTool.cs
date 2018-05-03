using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using linerider.Utils;
using linerider.Game;
using linerider.Rendering;
using linerider.Drawing;
using System.Drawing;

namespace linerider.Tools
{
    public class SelectTool : Tool
    {
        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }
        private Vector2d _boxstart;
        private DoubleRect _selectionbox = DoubleRect.Empty;
        private Vector2d _clickstart;
        private bool _movingselection = false;
        private List<LineSelection> _selection = new List<LineSelection>();
        private bool _drawingbox = false;
        private bool _movemade = false;
        private List<GameLine> _copybuffer = new List<GameLine>();
        private GameLine _snapline = null;
        private GameLine _hoverline = null;
        private Vector2d _copyorigin;
        public void Copy()
        {
            _copybuffer.Clear();
            if (Active && !_drawingbox && _selection.Count > 0)
            {
                foreach (var selected in _selection)
                {
                    _copybuffer.Add(selected.line.Clone());
                }
                _copyorigin = GetCopyOrigin();
            }
        }
        public void Paste()
        {
            if (_copybuffer.Count != 0)
            {
                Stop();
                var pasteorigin = GetCopyOrigin();
                var diff = pasteorigin - _copyorigin;
                _selection.Clear();
                Active = true;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    game.Track.UndoManager.BeginAction();
                    foreach (var line in _copybuffer)
                    {
                        var add = line.Clone();
                        add.ID = GameLine.UninitializedID;
                        add.Position += diff;
                        add.Position2 += diff;
                        if (add is StandardLine stl)
                            stl.CalculateConstants();
                        trk.AddLine(add);
                        var selectinfo = new LineSelection()
                        {
                            clone = add.Clone(),
                            line = add,
                            snapped = null,
                            joint1 = true,
                            joint2 = true,
                        };
                        _selection.Add(selectinfo);
                    }
                    game.Track.UndoManager.EndAction();
                }
                _selectionbox = GetBoxFromSelected(_selection);
            }
        }
        public override void OnMouseDown(Vector2d pos)
        {
            var gamepos = ScreenToGameCoords(pos);
            if (Active && _selection.Count != 0)
            {
                if (_selectionbox.Contains(gamepos.X, gamepos.Y))
                {
                    using (var trk = game.Track.CreateTrackReader())
                    {
                        var line = SelectLine(trk, gamepos, out bool knob);
                        if (line != null)
                        {
                            foreach (var s in _selection)
                            {
                                if (s.line.ID == line.ID)
                                {
                                    _snapline = s.clone;
                                    _clickstart = gamepos;
                                    _boxstart = _selectionbox.Vector;
                                    _movingselection = true;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            _selection.Clear();
            _selectionbox = DoubleRect.Empty;
            Active = true;

            _drawingbox = true;
            _selectionbox.Vector = gamepos;
            _selectionbox.Size = Vector2d.Zero;
            base.OnMouseDown(pos);
        }
        public override void OnMouseMoved(Vector2d pos)
        {
            var gamepos = ScreenToGameCoords(pos);
            if (Active && _drawingbox)
            {
                var size = gamepos - _selectionbox.Vector;
                _selectionbox.Size = size;
                _selection.Clear();
                using (var trk = game.Track.CreateTrackReader())
                {
                    var lines = trk.GetLinesInRect(_selectionbox.MakeLRTB(), true);
                    foreach (var line in lines)
                    {
                        var selection = new LineSelection()
                        {
                            snapped = null, // ignored
                            line = line,
                            clone = line.Clone(),
                            joint1 = true,
                            joint2 = true,
                        };
                        _selection.Add(selection);
                    }
                }
            }
            else if (Active && _movingselection)
            {
                MoveSelection(gamepos);
            }
            else if (Active)
            {
                _hoverline = null;
                using (var trk = game.Track.CreateTrackReader())
                {
                    var line = SelectLine(trk, gamepos, out bool knob);
                    if (line != null)
                    {
                        foreach (var s in _selection)
                        {
                            if (s.line.ID == line.ID)
                            {
                                _hoverline = s.line;
                                break;
                            }
                        }
                    }
                }
            }
            base.OnMouseMoved(pos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            if (_drawingbox)
            {
                _drawingbox = false;
                if (_selection.Count == 0)
                {
                    Stop();
                }
                else
                {
                    _selectionbox = GetBoxFromSelected(_selection);
                }
            }
            else
            {
                _snapline = null;
                _movingselection = false;
                SaveMovedSelection();
                foreach (var selected in _selection)
                {
                    selected.clone = selected.line.Clone();
                }
            }
            base.OnMouseUp(pos);
        }
        public override void OnChangingTool()
        {
            Stop();
        }
        public override void Stop()
        {
            if (Active)
            {
                SaveMovedSelection();
            }
            Active = false;
            _selection.Clear();
            _selectionbox = DoubleRect.Empty;
            _movingselection = false;
            _drawingbox = false;
            _movemade = false;
            _snapline = null;
            _hoverline = null;
        }
        public override void Render()
        {
            if (Active)
            {
                var color = Color.FromArgb(255, 192, 192, 192);
                GameRenderer.RenderRoundedRectangle(_selectionbox, color, 2f / game.Track.Zoom);
                if (_hoverline != null)
                {
                    GameRenderer.RenderRoundedLine(_hoverline.Position, _hoverline.Position2, Color.FromArgb(127, Constants.DefaultKnobColor), (_hoverline.Width * 2 * 0.8f));
                }
                if (_selection.Count != 0)
                {
                    GameRenderer.RenderSelection(_selection, Color.FromArgb(64, Constants.DefaultKnobColor));
                }
            }
            base.Render();
        }

        private DoubleRect GetBoxFromSelected(List<LineSelection> selected)
        {
            if (selected == null || selected.Count == 0)
                return DoubleRect.Empty;
            var ret = DoubleRect.Empty;
            Vector2d topleft = selected[0].line.Position;
            Vector2d bottomright = selected[0].line.Position;
            for (int i = 0; i < selected.Count; i++)
            {
                var sel = selected[i].line;
                var p1 = sel.Position;
                var p2 = sel.Position2;
                topleft.X = Math.Min(
                    topleft.X,
                    Math.Min(p1.X, p2.X) - sel.Width);
                topleft.Y = Math.Min(
                    topleft.Y,
                    Math.Min(p1.Y, p2.Y) - sel.Width);

                bottomright.X = Math.Max(
                    bottomright.X,
                    Math.Max(p1.X, p2.X) + (sel.Width));
                bottomright.Y = Math.Max(
                    bottomright.Y,
                    Math.Max(p1.Y, p2.Y) + (sel.Width));
            }
            return new DoubleRect(topleft, bottomright - topleft);
        }
        private Vector2d GetSnapOffset(Vector2d movediff, TrackReader trk)
        {
            var snap1 = _snapline.Position + movediff;
            var snap2 = _snapline.Position2 + movediff;
            var lines1 = LineEndsInRadius(trk, snap1, SnapRadius);
            var lines2 = (LineEndsInRadius(trk, snap2, SnapRadius));
            Vector2d snapoffset = Vector2d.Zero;
            double distance = -1;
            foreach (var line in lines1)
            {
                if (_selection.FirstOrDefault(x => x.line.ID == line.ID) == null)
                {
                    var closer = Utility.CloserPoint(snap1, line.Position, line.Position2);
                    var diff = closer - snap1;
                    var dist = diff.Length;
                    if (distance == -1 || dist < distance)
                        snapoffset = diff;
                }
            }
            foreach (var line in lines2)
            {
                if (_selection.FirstOrDefault(x => x.line.ID == line.ID) == null)
                {
                    var closer = Utility.CloserPoint(snap2, line.Position, line.Position2);
                    var diff = closer - snap2;
                    var dist = diff.Length;
                    if (distance == -1 || dist < distance)
                        snapoffset = diff;
                }
            }

            return snapoffset;
        }
        private void MoveSelection(Vector2d pos)
        {
            if (_selection.Count > 0)
            {
                _movemade = true;
                var movediff = (pos - _clickstart);
                using (var trk = game.Track.CreateTrackWriter())
                {
                    if (_snapline != null)
                    {
                        movediff += GetSnapOffset(movediff, trk);
                    }
                    _selectionbox.Vector = _boxstart + movediff;
                    foreach (var _selected in _selection)
                    {
                        var line = _selected.line;
                        trk.DisableUndo();
                        var joint1 = line.Position;
                        var joint2 = line.Position2;
                        joint1 =
                            _selected.clone.Position + movediff;
                        joint2 =
                            _selected.clone.Position2 + movediff;
                        trk.MoveLine(
                            line,
                            joint1,
                            joint2);
                    }
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }
        private void SaveMovedSelection()
        {
            if (Active)
            {
                if (_selection.Count != 0 && _movemade)
                {
                    game.Track.UndoManager.BeginAction();
                    foreach (var selected in _selection)
                    {
                        game.Track.UndoManager.AddChange(selected.clone, selected.line);
                    }
                    game.Track.UndoManager.EndAction();
                    _movemade = false;
                }
                game.Invalidate();
            }
        }
        private Vector2d GetCopyOrigin()
        {
            var zoom = game.Track.Zoom;
            var viewport = game.Track.Camera.GetViewport(
                game.Track.Zoom,
                (int)(Math.Round(game.RenderSize.Width / zoom)),
                (int)((game.RenderSize.Height / zoom)));
            return viewport.Vector + (viewport.Size / 2);
        }
    }
}