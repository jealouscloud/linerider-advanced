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
            get
            {
                if (_hoverscale)
                {
                    if (_nodetop == _nodeleft)
                        return game.Cursors["size_nwse"];
                    else
                        return game.Cursors["size_nesw"];
                }
                return game.Cursors["adjustline"];
            }
        }
        private bool _hoverscale = false;
        private DoubleRect _boxstart;
        private DoubleRect _selectionbox = DoubleRect.Empty;
        private DoubleRect _startselectionedges = DoubleRect.Empty;
        private DoubleRect _drawbox = DoubleRect.Empty;
        private Vector2d _clickstart;
        private bool _movingselection = false;
        private List<LineSelection> _selection = new List<LineSelection>();
        private List<LineSelection> _boxselection = new List<LineSelection>();
        private HashSet<int> _selectedlines = new HashSet<int>();
        private bool _drawingbox = false;
        private bool _movemade = false;
        private List<GameLine> _copybuffer = new List<GameLine>();
        private GameLine _snapline = null;
        private bool _snapknob2 = false;
        private bool _snapknob1 = false;
        private GameLine _hoverline = null;
        private Vector2d _copyorigin;
        private bool _scaling = false;
        private bool _rotating = false;
        private bool _nodetop = false;
        private bool _nodeleft = false;

        public void CancelSelection()
        {
            if (Active)
            {
                if (_drawingbox)
                {
                    Cancel();
                }
                else
                {
                    Stop();
                    DeferToMoveTool();
                }
            }
        }
        public override void OnUndoRedo(bool isundo, object undohint)
        {
            if (Active && (_selection.Count != 0 || _boxselection.Count != 0) &&
                (undohint is int[] lineids))
            {
                if (lineids.Length != 0)
                {
                    Stop(false);
                    _hoverline = null;
                    Active = true;
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        foreach (var lineid in lineids)
                        {
                            var line = trk.Track.LineLookup[lineid];
                            _selectedlines.Add(line.ID);
                            var selection = new LineSelection(line, true, null);
                            _selection.Add(selection);
                            line.SelectionState = SelectionState.Selected;
                            game.Track.RedrawLine(line);
                        }
                    }
                    _selectionbox = GetBoxFromSelected(_selection);
                    return;
                }
            }
            Stop(true);
        }
        public override void OnMouseRightDown(Vector2d pos)
        {
            if (Active && _selection.Count != 0 && _movingselection == false)
            {
                var gamepos = ScreenToGameCoords(pos);
                StartTransformSelection(gamepos, true);
            }
            base.OnMouseRightDown(pos);
        }
        public override void OnMouseRightUp(Vector2d pos)
        {
            if (_movingselection & _rotating)
            {
                ReleaseSelection();
            }
            base.OnMouseRightUp(pos);
        }
        public override void OnMouseDown(Vector2d pos)
        {
            var gamepos = ScreenToGameCoords(pos);
            if (Active && _selection.Count != 0)
            {
                if (UI.InputUtils.CheckPressed(UI.Hotkey.ToolAddSelection))
                {
                    StartAddSelection(gamepos);
                    return;
                }
                if (UI.InputUtils.CheckPressed(UI.Hotkey.ToolToggleSelection))
                {
                    ToggleSelection(gamepos);
                    return;
                }
                else if (StartMoveSelection(gamepos))
                {
                    return;
                }
            }
            Unselect();
            _selectionbox = DoubleRect.Empty;
            _drawbox = new DoubleRect(gamepos, Vector2d.Zero);
            Active = true;
            _drawingbox = true;
            _movingselection = false;
            base.OnMouseDown(pos);
        }
        public override void OnMouseMoved(Vector2d pos)
        {
            var gamepos = ScreenToGameCoords(pos);
            _hoverscale = false;
            if (Active && _drawingbox)
            {
                UpdateDrawingBox(gamepos);
            }
            else if (Active && _movingselection)
            {
                if (_scaling)
                    ScaleSelection(gamepos);
                else if (_rotating)
                    RotateSelection(gamepos);
                else
                    MoveSelection(gamepos);
            }
            else if (Active)
            {
                UpdateHover(gamepos);
            }
            base.OnMouseMoved(pos);
        }
        public override void OnMouseUp(Vector2d pos)
        {
            if (_drawingbox)
            {
                _drawingbox = false;
                if (_selection.Count == 0 && _boxselection.Count == 0)
                {
                    Stop(true);
                }
                else
                {
                    foreach (var v in _boxselection)
                    {
                        _selectedlines.Add(v.line.ID);
                        _selection.Add(v);
                    }
                    _selectionbox = GetBoxFromSelected(_selection);
                    _boxselection.Clear();
                }
                _drawbox = DoubleRect.Empty;
            }
            else
            {
                ReleaseSelection();
            }
            base.OnMouseUp(pos);
        }
        private void UpdateDrawingBox(Vector2d gamepos)
        {
            var size = gamepos - _drawbox.Vector;
            _drawbox.Size = size;
            UnselectBox();
            using (var trk = game.Track.CreateTrackWriter())
            {
                var lines = trk.GetLinesInRect(_drawbox.MakeLRTB(), true);
                foreach (var line in lines)
                {
                    if (!_selectedlines.Contains(line.ID))
                    {
                        var selection = new LineSelection(line, true, null);
                        line.SelectionState = SelectionState.Selected;
                        _boxselection.Add(selection);
                        game.Track.RedrawLine(line);
                    }
                }
            }
        }
        private void UpdateHover(Vector2d gamepos)
        {
            _hoverline = null;
            using (var trk = game.Track.CreateTrackReader())
            {
                GameLine selected;
                if (UI.InputUtils.CheckPressed(UI.Hotkey.ToolToggleSelection))
                    selected = SelectLine(trk, gamepos, out bool knob);
                else
                    selected = SelectInSelection(trk, gamepos)?.line;
                if (selected != null)
                {
                    _hoverline = selected;
                }
            }
            if (CornerContains(gamepos, _nodetop = true, _nodeleft = true) ||
                CornerContains(gamepos, _nodetop = true, _nodeleft = false) ||
                CornerContains(gamepos, _nodetop = false, _nodeleft = false) ||
                CornerContains(gamepos, _nodetop = false, _nodeleft = true))
            {
                _hoverscale = true;
            }
        }
        private void ReleaseSelection()
        {
            if (_movingselection)
            {
                _snapline = null;
                _movingselection = false;
                _snapknob1 = false;
                _snapknob2 = false;
                _rotating = false;
                _scaling = false;
                SaveMovedSelection();
                foreach (var selected in _selection)
                {
                    selected.clone = selected.line.Clone();
                }
            }
        }
        public override void OnChangingTool()
        {
            Stop(false);
        }

        public override void Cancel()
        {
            _hoverscale = false;
            CancelDrawBox();
            UnselectBox();
            if (Active)
            {
                ReleaseSelection();
            }
        }
        public override void Stop()
        {
            if (Active)
            {
                Stop(true);
            }
        }
        private void Stop(bool defertomovetool)
        {
            if (Active)
            {
                SaveMovedSelection();
            }
            Active = false;
            UnselectBox();
            Unselect();
            CancelDrawBox();
            _selectionbox = DoubleRect.Empty;
            _movingselection = false;
            _movemade = false;
            _snapline = null;
            _hoverline = null;
            _snapknob1 = false;
            _snapknob2 = false;
            _scaling = false;
            _rotating = false;
            _hoverscale = false;
            if (defertomovetool)
                DeferToMoveTool();
        }
        private DoubleRect GetCorner(DoubleRect box, bool top, bool left, int boxsize = 15)
        {
            DoubleRect corner = box;
            corner.Width = boxsize / game.Track.Zoom;
            corner.Height = boxsize / game.Track.Zoom;
            if (!left)
            {
                corner.Left = box.Right;
            }
            else
            {
                corner.Left -= corner.Width;
            }
            if (!top)
            {
                corner.Top = box.Bottom;
            }
            else
            {
                corner.Top -= corner.Height;
            }
            return corner;
        }
        private void RenderCorner(DoubleRect box, bool top, bool left)
        {
            var corner = GetCorner(box, top, left);
            var rect = new DoubleRect(
                GameDrawingMatrix.ScreenCoordD(corner.Vector),
                new Vector2d(corner.Width * game.Track.Zoom,
                corner.Height * game.Track.Zoom));
            GameRenderer.RenderRoundedRectangle(rect, Color.CornflowerBlue, 2, false);
        }
        public override void Render()
        {
            if (Active)
            {
                var color = Color.FromArgb(255, 0x00, 0x77, 0xcc);
                if (_selectionbox != DoubleRect.Empty)
                {
                    GameRenderer.RenderRoundedRectangle(_selectionbox, color, 2f / game.Track.Zoom);
                    RenderCorner(_selectionbox, true, true);
                    RenderCorner(_selectionbox, false, false);
                    RenderCorner(_selectionbox, true, false);
                    RenderCorner(_selectionbox, false, true);
                }
                if (_drawbox != DoubleRect.Empty)
                    GameRenderer.RenderRoundedRectangle(_drawbox, color, 2f / game.Track.Zoom);
                if (_hoverline != null)
                {
                    GameRenderer.RenderRoundedLine(_hoverline.Position, _hoverline.Position2, Color.FromArgb(127, Constants.DefaultKnobColor), (_hoverline.Width * 2 * 0.8f));

                    GameRenderer.DrawKnob(_hoverline.Position, _snapknob1, false, _hoverline.Width, _snapknob1 && !_snapknob2 ? 1 : 0);
                    GameRenderer.DrawKnob(_hoverline.Position2, _snapknob2, false, _hoverline.Width, _snapknob2 && !_snapknob1 ? 1 : 0);
                }
            }
            base.Render();
        }
        public bool CancelDrawBox()
        {
            if (_drawingbox)
            {
                UnselectBox();
                _drawingbox = false;
                _drawbox = DoubleRect.Empty;
                return true;
            }
            return false;
        }
        public void Delete()
        {
            if (Active && !_drawingbox && _selection.Count > 0)
            {
                using (var trk = game.Track.CreateTrackWriter())
                {
                    game.Track.UndoManager.BeginAction();
                    foreach (var selected in _selectedlines)
                    {
                        var line = trk.Track.LineLookup[selected];
                        line.SelectionState = SelectionState.None;
                        trk.RemoveLine(line);
                    }
                    game.Track.UndoManager.EndAction();
                    trk.NotifyTrackChanged();
                }
                _selection.Clear();
                _selectedlines.Clear();
                Stop();
            }
        }
        public void Cut()
        {
            if (Active && !_drawingbox && _selection.Count > 0)
            {
                Copy();
                Delete();
            }
        }
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
                Stop(false);
                var pasteorigin = GetCopyOrigin();
                var diff = pasteorigin - _copyorigin;
                Unselect();
                Active = true;
                if (CurrentTools.SelectedTool != this)
                {
                    CurrentTools.SetTool(this);
                }
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
                        add.SelectionState = SelectionState.Selected;
                        trk.AddLine(add);
                        var selectinfo = new LineSelection(add, true, null);
                        _selection.Add(selectinfo);
                        _selectedlines.Add(add.ID);
                    }
                    game.Track.UndoManager.EndAction();
                }
                _selectionbox = GetBoxFromSelected(_selection);
                game.Track.NotifyTrackChanged();
            }
        }
        private void StartAddSelection(Vector2d gamepos)
        {
            _movingselection = false;
            _drawbox = new DoubleRect(gamepos, Vector2d.Zero);
            _drawingbox = true;
        }
        private bool ToggleSelection(Vector2d gamepos)
        {
            using (var trk = game.Track.CreateTrackWriter())
            {
                var line = SelectLine(trk, gamepos, out bool knob);
                if (line != null)
                {
                    if (_selectedlines.Contains(line.ID))
                    {
                        _selectedlines.Remove(line.ID);
                        _selection.RemoveAt(
                            _selection.FindIndex(
                                x => x.line.ID == line.ID));
                        line.SelectionState = SelectionState.None;
                    }
                    else
                    {
                        _selectedlines.Add(line.ID);
                        var selection = new LineSelection(line, true, null);
                        _selection.Add(selection);
                        line.SelectionState = SelectionState.Selected;
                    }
                    _selectionbox = GetBoxFromSelected(_selection);
                    game.Track.RedrawLine(line);
                    return true;
                }
            }
            return false;
        }
        /// <param name="rotate">
        /// If true rotates selection, if false scales selection
        /// </param>
        private bool StartTransformSelection(Vector2d gamepos, bool rotate)
        {
            if (CornerContains(gamepos, _nodetop = true, _nodeleft = true) ||
                CornerContains(gamepos, _nodetop = true, _nodeleft = false) ||
                CornerContains(gamepos, _nodetop = false, _nodeleft = false) ||
                CornerContains(gamepos, _nodetop = false, _nodeleft = true))
            {
                if (rotate)
                {
                    _rotating = true;
                }
                else
                {
                    _scaling = true;
                }
                _movingselection = true;
                _clickstart = gamepos;
                _boxstart = _selectionbox;
                _startselectionedges = GetBoxLineEdges(_selection);
                return true;
            }
            return false;
        }
        private bool StartMoveSelection(Vector2d gamepos)
        {
            if (_selectionbox.Contains(gamepos.X, gamepos.Y))
            {
                using (var trk = game.Track.CreateTrackReader())
                {
                    var selected = SelectInSelection(trk, gamepos);
                    if (selected != null)
                    {
                        bool snapped = IsLineSnappedByKnob(trk, gamepos, selected.clone, out bool knob1);

                        _snapknob1 = !snapped || knob1;
                        _snapknob2 = !snapped || !knob1;

                        _snapline = selected.clone;
                        _clickstart = gamepos;
                        _boxstart = _selectionbox;
                        _movingselection = true;
                        return true;
                    }
                }
            }
            else
            {
                return StartTransformSelection(gamepos, false);
            }
            return false;
        }
        private bool CornerContains(Vector2d gamepos, bool top, bool left)
        {
            var corner = GetCorner(_selectionbox, top, left);
            var ret = corner.Contains(gamepos.X, gamepos.Y);
            return ret;
        }
        /// <summary>
        /// Returns selection box without calculating line widths
        /// </summary>
        private DoubleRect GetBoxLineEdges(List<LineSelection> selected)
        {
            if (selected == null || selected.Count == 0)
                return DoubleRect.Empty;
            var ret = DoubleRect.Empty;
            Vector2d tl = selected[0].line.Position;
            Vector2d br = selected[0].line.Position;
            for (int i = 0; i < selected.Count; i++)
            {
                var sel = selected[i].line;
                var p1 = sel.Position;
                var p2 = sel.Position2;
                tl.X = Math.Min(tl.X, Math.Min(p1.X, p2.X));
                tl.Y = Math.Min(tl.Y, Math.Min(p1.Y, p2.Y));
                br.X = Math.Max(br.X, Math.Max(p1.X, p2.X));
                br.Y = Math.Max(br.Y, Math.Max(p1.Y, p2.Y));
            }
            return new DoubleRect(tl, br - tl);
        }
        private DoubleRect GetBoxFromSelected(List<LineSelection> selected)
        {
            if (selected == null || selected.Count == 0)
                return DoubleRect.Empty;
            var ret = DoubleRect.Empty;
            Vector2d tl = selected[0].line.Position;
            Vector2d br = selected[0].line.Position;
            for (int i = 0; i < selected.Count; i++)
            {
                var sel = selected[i].line;
                var p1 = sel.Position;
                var p2 = sel.Position2;
                tl.X = Math.Min(tl.X, Math.Min(p1.X, p2.X) - sel.Width);
                tl.Y = Math.Min(tl.Y, Math.Min(p1.Y, p2.Y) - sel.Width);

                br.X = Math.Max(br.X, Math.Max(p1.X, p2.X) + (sel.Width));
                br.Y = Math.Max(br.Y, Math.Max(p1.Y, p2.Y) + (sel.Width));
            }
            return new DoubleRect(tl, br - tl);
        }
        private Vector2d GetSnapOffset(Vector2d movediff, TrackReader trk)
        {
            Vector2d snapoffset = Vector2d.Zero;
            double distance = -1;
            void checklines(GameLine[] lines, Vector2d snap)
            {
                foreach (var line in lines)
                {
                    if (!_selectedlines.Contains(line.ID))
                    {
                        var closer = Utility.CloserPoint(snap, line.Position, line.Position2);
                        var diff = closer - snap;
                        var dist = diff.Length;
                        if (distance == -1 || dist < distance)
                        {
                            snapoffset = diff;
                            distance = dist;
                        }
                    }
                }
            }
            if (_snapknob1)
            {
                var snap1 = _snapline.Position + movediff;
                var lines1 = LineEndsInRadius(trk, snap1, SnapRadius);
                checklines(lines1, snap1);
            }
            if (_snapknob2)
            {
                var snap2 = _snapline.Position2 + movediff;
                var lines2 = (LineEndsInRadius(trk, snap2, SnapRadius));
                checklines(lines2, snap2);
            }

            return snapoffset;
        }
        private void ScaleSelection(Vector2d pos)
        {
            if (_selection.Count > 0)
            {
                _movemade = true;
                var movediff = (pos - _clickstart);
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();

                    foreach (var selected in _selection)
                    {
                        var scalar1 = Vector2d.Divide(
                            selected.clone.Position - _startselectionedges.Vector,
                            _startselectionedges.Size);

                        var scalar2 = Vector2d.Divide(
                            selected.clone.Position2 - _startselectionedges.Vector,
                             _startselectionedges.Size);

                        if (_nodetop)
                        {
                            scalar1.Y = 1 - scalar1.Y;
                            scalar2.Y = 1 - scalar2.Y;
                        }
                        if (_nodeleft)
                        {
                            scalar1.X = 1 - scalar1.X;
                            scalar2.X = 1 - scalar2.X;
                        }
                        var p1 = Vector2d.Multiply(scalar1, movediff);
                        var p2 = Vector2d.Multiply(scalar2, movediff);
                        trk.MoveLine(
                            selected.line,
                            selected.clone.Position + p1,
                            selected.clone.Position2 + p2);
                    }
                    _selectionbox = GetBoxFromSelected(_selection);
                    /*
                    if (_nodetop)
                    {
                        _selectionbox.Top = _boxstart.Top + movediff.Y;
                        _selectionbox.Height = _boxstart.Height - movediff.Y;
                    }
                    else
                        _selectionbox.Height = _boxstart.Height + movediff.Y;

                    if (_nodeleft)
                    {
                        _selectionbox.Left = _boxstart.Left + movediff.X;
                        _selectionbox.Width = _boxstart.Width - movediff.X;
                    }
                    else
                        _selectionbox.Width = _boxstart.Width + movediff.X;
*/
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }
        private void RotateSelection(Vector2d pos)
        {
            if (_selection.Count > 0)
            {
                _movemade = true;
                var movediff = (pos - _clickstart);
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    var center = _startselectionedges.Vector +
                        (_startselectionedges.Size / 2);
                    var edge = new Vector2d(_nodeleft
                        ? _startselectionedges.Left
                        : _startselectionedges.Right,
                        _nodetop
                        ? _startselectionedges.Top
                        : _startselectionedges.Bottom);

                    var angle = Angle.FromVector(edge - center);
                    var newangle = Angle.FromVector(pos - center);
                    var anglediff = newangle - angle;

                    foreach (var selected in _selection)
                    {
                        Vector2d p1 = Utility.Rotate(selected.clone.Position, center, anglediff);
                        Vector2d p2 = Utility.Rotate(selected.clone.Position2, center, anglediff);
                        trk.MoveLine(selected.line, p1, p2);
                    }
                    _selectionbox = GetBoxFromSelected(_selection);
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }
        private void MoveSelection(Vector2d pos)
        {
            if (_selection.Count > 0)
            {
                _movemade = true;
                var movediff = (pos - _clickstart);
                using (var trk = game.Track.CreateTrackWriter())
                {
                    if (_snapline != null && EnableSnap)
                    {
                        movediff += GetSnapOffset(movediff, trk);
                    }
                    _selectionbox.Vector = _boxstart.Vector + movediff;
                    foreach (var selected in _selection)
                    {
                        trk.DisableUndo();
                        trk.MoveLine(
                            selected.line,
                            selected.clone.Position + movediff,
                            selected.clone.Position2 + movediff);
                    }
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }
        private void UnselectBox()
        {
            if (_boxselection.Count != 0)
            {
                using (var trk = game.Track.CreateTrackWriter())
                {
                    foreach (var sel in _boxselection)
                    {
                        if (!_selectedlines.Contains(sel.line.ID))
                        {
                            if (sel.line.SelectionState != 0)
                            {
                                sel.line.SelectionState = 0;
                                game.Track.RedrawLine(sel.line);
                            }
                        }
                    }
                }
                _boxselection.Clear();
            }
        }
        private void Unselect()
        {
            if (_selection.Count != 0)
            {
                using (var trk = game.Track.CreateTrackWriter())
                {
                    var lookup = trk.Track.LineLookup;
                    foreach (var sel in _selection)
                    {
                        //prefer the 'real' line, if the track state changed
                        //our sel.line could be out of sync
                        if (lookup.TryGetValue(sel.line.ID, out var line))
                        {
                            if (line.SelectionState != SelectionState.None)
                            {
                                line.SelectionState = SelectionState.None;
                                game.Track.RedrawLine(line);
                            }
                        }
                    }
                    _selection.Clear();
                    _selectedlines.Clear();
                }
            }
            _movemade = false;
        }
        private void SaveMovedSelection()
        {
            if (Active)
            {
                if (_selection.Count != 0 && _movemade)
                {
                    game.Track.UndoManager.BeginAction();
                    game.Track.UndoManager.SetActionUserHint(_selectedlines.ToArray());
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
        private void DeferToMoveTool()
        {
            CurrentTools.SetTool(CurrentTools.MoveTool);
        }
        private Vector2d GetCopyOrigin()
        {
            return game.Track.Camera.GetCenter();
        }
        /// <summary>
        /// Return the line (if any) in the point that we've selected
        /// </summary>
        private LineSelection SelectInSelection(TrackReader trk, Vector2d gamepos)
        {
            foreach (var line in SelectLines(trk, gamepos))
            {
                if (_selectedlines.Contains(line.ID))
                {
                    foreach (var s in _selection)
                    {
                        if (s.line.ID == line.ID)
                        {
                            return s;
                        }
                    }
                }
            }
            return null;
        }
    }
}