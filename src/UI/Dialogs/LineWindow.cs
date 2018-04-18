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
        private const string DefaultTitle = "Line Propeties";
        public LineWindow(GameCanvas parent, Editor editor, GameLine line) : base(parent, editor)
        {
            _ownerline = line;
            _linecopy = _ownerline.Clone();
            Title = "Line Properties";
            Padding = new Padding(0, 0, 0, 0);
            AutoSizeToContents = true;
            _proptree = new PropertyTree(this)
            {
                Width = 200,
                Height = 150
            };
            _proptree.Dock = Dock.Top;
            MakeModal(true);
            Setup();
            _proptree.ExpandAll();
            this.IsHiddenChanged += (o, e) =>
            {
                if (IsHidden)
                {
                    FinishChange();
                }
            };
        }
        private void Setup()
        {
            SetupRedOptions(_proptree);
            SetupTriggers(_proptree);
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
                if (_linechangemade)
                {
                    _editor.UndoManager.CancelAction();
                    _linechangemade = false;
                }
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
                    Max = 3,
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
            }
        }
        private void SetupTriggers(PropertyTree tree)
        {
            if (_ownerline is StandardLine physline)
            {
                var table = tree.Add("Triggers", 120);
                var trigger = physline.Trigger;
                var triggerenabled = AddPropertyCheckbox(table, "Enabled", trigger != null);
                var zoom = new NumberProperty(table)
                {
                    Min = Constants.MinimumZoom,
                    Max = Constants.MaxZoom,
                    NumberValue = 4
                };
                if (trigger != null)
                {
                    zoom.NumberValue = trigger.ZoomTarget;
                }
                table.Add("Target Zoom", zoom);
                var frames = new NumberProperty(table)
                {
                    Min = 0,
                    Max = 40 * 60 * 2,//2 minutes is enough for a zoom trigger, ok.
                    NumberValue = 40,
                    OnlyWholeNumbers = true,
                };
                if (trigger != null)
                {
                    frames.NumberValue = trigger.ZoomFrames;
                }
                table.Add("Frames", frames);
                triggerenabled.ValueChanged += (o, e) =>
                {
                    using (var trk = _editor.CreateTrackWriter())
                    {
                        if (triggerenabled.IsChecked)
                        {
                            var cpy = (StandardLine)physline.Clone();
                            cpy.Trigger = new LineTrigger()
                            {
                                ZoomFrames = (int)frames.NumberValue,
                                ZoomTarget = (float)zoom.NumberValue
                            };
                            UpdateLine(trk, _ownerline, cpy);
                        }
                        else
                        {
                            var cpy = (StandardLine)physline.Clone();
                            cpy.Trigger = null;
                            UpdateLine(trk, _ownerline, cpy);
                        }
                    }
                };
            }
        }
        private CheckProperty AddPropertyCheckbox(PropertyTable prop, string label, bool value)
        {
            var check = new CheckProperty(null);
            prop.Add(label, check);
            check.IsChecked = value;
            return check;
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
                UpdateLine(trk, _ownerline, cpy);
                _ownerline = cpy;
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
