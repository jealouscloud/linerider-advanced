using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;
using linerider.Game;
using OpenTK;
using System.Linq;

namespace linerider.UI
{
    public class LineWindow : DialogBase
    {
        private PropertyTree _proptree;
        private GameLine _ownerline;
        private GameLine _linecopy;
        private bool _linechangemade = false;
        private const string DefaultTitle = "Line Properties";
        private bool closing = false;
        public LineWindow(GameCanvas parent, Editor editor, GameLine line) : base(parent, editor)
        {
            _ownerline = line;
            _linecopy = _ownerline.Clone();
            Title = "Line Properties";
            Padding = new Padding(0, 0, 0, 0);
            AutoSizeToContents = true;
            _proptree = new PropertyTree(this)
            {
                Width = 220,
                Height = 200
            };
            _proptree.Dock = Dock.Top;
            MakeModal(true);
            Setup();
            _proptree.ExpandAll();
        }
        protected override void CloseButtonPressed(ControlBase control, EventArgs args)
        {
            if (closing || !_linechangemade)
            {
                closing = true;
                base.CloseButtonPressed(control, args);
            }
            else
            {
                WarnClose();
            }
        }
        public override bool Close()
        {
            if (closing || !_linechangemade)
            {
                closing = true;
                return base.Close();
            }
            else
            {
                WarnClose();
                return false;
            }
        }
        private void WarnClose()
        {
            var mbox = MessageBox.Show(
                _canvas,
                "The line has been modified. Do you want to save your changes?",
                "Save Changes?",
                MessageBox.ButtonType.YesNoCancel);
            mbox.RenameButtonsYN("Save", "Discard", "Cancel");
            mbox.MakeModal(false);
            mbox.Dismissed += (o, e) =>
            {
                switch (e)
                {
                    case DialogResult.Yes:
                        FinishChange();
                        closing = true;
                        base.Close();
                        break;
                    case DialogResult.No:
                        CancelChange();
                        closing = true;
                        base.Close();
                        break;
                }
            };
        }
        private void Setup()
        {
            SetupRedOptions(_proptree);
            Panel bottom = new Panel(this)
            {
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
                ShouldDrawBackground = false,
            };
            Button cancel = new Button(bottom)
            {
                Text = "Cancel",
                Dock = Dock.Right,
            };
            cancel.Clicked += (o, e) =>
            {
                CancelChange();
                closing = true;
                Close();
            };
            Button ok = new Button(bottom)
            {
                Text = "Okay",
                Dock = Dock.Right,
                Margin = new Margin(0, 0, 5, 0)
            };
            ok.Clicked += (o, e) =>
            {
                FinishChange();
                closing = true;
                Close();
            };
        }
        private void SetupRedOptions(PropertyTree tree)
        {
            if (_ownerline is RedLine red)
            {
                var table = tree.Add("Acceleration", 120);
                var multiplier = new NumberProperty(table)
                {
                    Min = 1,
                    Max = 255,
                    NumberValue = red.Multiplier,
                    OnlyWholeNumbers = true,
                };
                multiplier.ValueChanged += (o, e) =>
                {
                    ChangeMultiplier((int)multiplier.NumberValue);
                };
                table.Add("Multiplier", multiplier);
                var multilines = new NumberProperty(table)
                {
                    Min = 1,
                    Max = 9999,
                    OnlyWholeNumbers = true,
                };
                multilines.NumberValue = GetMultiLines(true).Count;
                multilines.ValueChanged += (o, e) =>
                {
                    Multiline((int)multilines.NumberValue);
                };
                table.Add("Multilines", multilines);

                var accelinverse = GwenHelper.AddPropertyCheckbox(
                    table,
                    "Inverse",
                    red.inv
                    );
                accelinverse.ValueChanged += (o, e) =>
                {
                    using (var trk = _editor.CreateTrackWriter())
                    {
                        var multi = GetMultiLines(false);
                        foreach (var l in multi)
                        {
                            var cpy = (StandardLine)l.Clone();
                            cpy.Position = l.Position2;
                            cpy.Position2 = l.Position;
                            cpy.inv = accelinverse.IsChecked;
                            UpdateLine(trk, l, cpy);
                        }

                        var owner = (StandardLine)_ownerline.Clone();
                        owner.Position = _ownerline.Position2;
                        owner.Position2 = _ownerline.Position;
                        owner.inv = accelinverse.IsChecked;
                        UpdateOwnerLine(trk, owner);
                    }
                };
            }
        }

        private void UpdateOwnerLine(TrackWriter trk, GameLine replacement)
        {
            UpdateLine(trk, _ownerline, replacement);
            _ownerline = replacement;
        }
        private void UpdateLine(TrackWriter trk, GameLine current, GameLine replacement)
        {
            MakingChange();

            if (replacement is StandardLine stl)
            {
                stl.CalculateConstants();
            }
            trk.ReplaceLine(current, replacement);
            _editor.NotifyTrackChanged();
            _editor.Invalidate();
        }
        private void ChangeMultiplier(int mul)
        {
            var lines = GetMultiLines(false);
            using (var trk = _editor.CreateTrackWriter())
            {
                var cpy = (RedLine)_ownerline.Clone();
                cpy.Multiplier = mul;
                UpdateOwnerLine(trk, cpy);
                foreach (var line in lines)
                {
                    var copy = (RedLine)line.Clone();
                    copy.Multiplier = mul;
                    UpdateLine(trk, line, copy);
                }
            }
        }
        private SimulationCell GetMultiLines(bool includeowner)
        {
            SimulationCell redlines = new SimulationCell();
            using (var trk = _editor.CreateTrackReader())
            {
                var owner = (StandardLine)_ownerline;
                var lines = trk.GetLinesInRect(new Utils.DoubleRect(owner.Position, new Vector2d(1, 1)), false);
                foreach (var red in lines)
                {
                    if (
                        red is RedLine stl &&
                        red.Position == owner.Position &&
                        red.Position2 == owner.Position2 &&
                        (includeowner || red.ID != owner.ID))
                    {
                        redlines.AddLine(stl);
                    }
                }
            }
            return redlines;
        }
        private void Multiline(int count)
        {
            SimulationCell redlines = GetMultiLines(false);
            using (var trk = _editor.CreateTrackWriter())
            {
                var owner = (StandardLine)_ownerline;
                MakingChange();
                // owner line doesn't count, but our min bounds is 1
                var diff = (count - 1) - redlines.Count;
                if (diff < 0)
                {
                    for (int i = 0; i > diff; i--)
                    {
                        trk.RemoveLine(redlines.First());
                        redlines.RemoveLine(redlines.First().ID);
                    }
                }
                else if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        var red = new RedLine(owner.Position, owner.Position2, owner.inv) { Multiplier = ((RedLine)owner).Multiplier };
                        red.CalculateConstants();
                        trk.AddLine(red);
                    }
                }
            }
            _editor.NotifyTrackChanged();
        }
        private void MakingChange()
        {
            if (!_linechangemade)
            {
                _editor.UndoManager.BeginAction();
                _linechangemade = true;
                Title = DefaultTitle + " *";
            }
        }
        private void CancelChange()
        {
            if (_linechangemade)
            {
                _editor.UndoManager.CancelAction();
                _linechangemade = false;
            }
        }
        private void FinishChange()
        {
            if (_linechangemade)
            {
                _editor.UndoManager.EndAction();
                _linechangemade = false;
            }
        }
    }
}
