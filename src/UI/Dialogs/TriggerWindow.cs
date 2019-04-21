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
using System.Diagnostics;

namespace linerider.UI
{
    public class TriggerWindow : DialogBase
    {
        private const int FramePadding = 0;
        private HorizontalSlider _slider;
        private ListBox _lbtriggers;
        private ControlBase _zoomoptions;

        private Spinner _spinnerStart;
        private Spinner _spinnerDuration;
        private ComboBox _triggertype;
        private Spinner _zoomtarget;
        private int SliderFrames
        {
            get
            {
                return TriggerDuration + (FramePadding * 2);
            }
        }
        private int TriggerDuration
        {
            get
            {
                return (int)(_spinnerDuration.Value);
            }
        }
        private int SelectedTrigger
        {
            get
            {
                return _lbtriggers.SelectedRowIndex;
            }
        }
        private List<GameTrigger> _triggers_copy = new List<GameTrigger>();
        private GameTrigger _trigger_copy = null;
        private bool _changemade = false;
        private bool _closing = false;
        public TriggerWindow(GameCanvas parent, Editor editor)
        : base(parent, editor)
        {
            Title = "Timeline Triggers";
            Size = new Size(400, 340);
            Padding = new Padding(0, 0, 0, 0);
            MakeModal(true);
            using (var trk = editor.CreateTrackWriter())
            {
                foreach (var trigger in trk.Triggers)
                {
                    _triggers_copy.Add(trigger.Clone());
                }
            }
            Setup();
            ToggleDisable(true);
            MinimumSize = new Size(250, MinimumSize.Height);
            DisableResizing();

        }
        protected override void CloseButtonPressed(
            ControlBase control,
            EventArgs args)
        {
            if (_closing || !_changemade)
            {
                _closing = true;
                base.CloseButtonPressed(control, args);
            }
            else
            {
                WarnClose();
            }
        }
        public override bool Close()
        {
            if (_closing || !_changemade)
            {
                _closing = true;
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
                        _closing = true;
                        base.Close();
                        break;
                    case DialogResult.No:
                        CancelChange();
                        _closing = true;
                        base.Close();
                        break;
                }
            };
        }
        private GameTrigger BeginModifyTrigger(TrackWriter trk)
        {
            var selected = SelectedTrigger;
            if (selected == -1)
                return null;
            var trigger = trk.Triggers[selected];
            _trigger_copy = trigger.Clone();
            return trigger;
        }
        private void EndModifyTrigger(GameTrigger trigger, TrackWriter trk)
        {
            var selected = SelectedTrigger;
            if (selected == -1)
                throw new Exception(
                    "SelectedTrigger was removed during ModifyTrigger");

            _changemade = true;
            trigger.Start = (int)(_spinnerStart.Value);
            trigger.End = (int)(_spinnerStart.Value + _spinnerDuration.Value);
            trk.Triggers[SelectedTrigger] = trigger;
            _editor.Timeline.TriggerChanged(_trigger_copy, trigger);
            UpdateFrame();
        }
        private void SetupZoom()
        {
            _zoomoptions = new ControlBase(null)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Fill
            };
            _zoomtarget = new Spinner(null)
            {
                Min = Constants.MinimumZoom,
                Max = Constants.MaxZoom,
                Value = _editor.Zoom,
            };
            _zoomtarget.ValueChanged += (o, e) =>
            {
                if (_selecting_trigger)
                    return;
                using (var trk = _editor.CreateTrackWriter())
                {
                    var trigger = BeginModifyTrigger(trk);
                    if (trigger != null)
                    {
                        trigger.ZoomTarget = (float)_zoomtarget.Value;
                        EndModifyTrigger(trigger, trk);
                    }
                }
            };
            GwenHelper.CreateLabeledControl(
                _zoomoptions, "Zoom Target:", _zoomtarget).Dock = Dock.Bottom;
        }
        private void SetupRight()
        {
            SetupZoom();
            ControlBase rightcontainer = new ControlBase(this)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Right,
                AutoSizeToContents = true
            };
            ControlBase buttoncontainer = new ControlBase(rightcontainer)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
                Children =
                {
                    new Button(null)
                    {
                        Text = "Cancel",
                        Name = "btncancel",
                        Dock = Dock.Right,
                        Margin = new Margin(5,0,0,0),
                        AutoSizeToContents = false,
                        Width = 60,
                    },
                    new Button(null)
                    {
                        Text = "Save",
                        Name = "btnsave",
                        Dock = Dock.Right,
                        AutoSizeToContents = false,
                        Width = 120,
                    }
                }
            };
            var save = buttoncontainer.FindChildByName("btnsave");
            var cancel = buttoncontainer.FindChildByName("btncancel");
            save.Clicked += (o, e) =>
            {
                FinishChange();
                Close();
            };
            cancel.Clicked += (o, e) =>
            {
                CancelChange();
                Close();
            };
            Panel triggeroptions = new Panel(rightcontainer)
            {
                Dock = Dock.Fill,
                Padding = Padding.Four,
                Margin = new Margin(0, 0, 0, 5)
            };

            _zoomoptions.Parent = triggeroptions;
            _triggertype = GwenHelper.CreateLabeledCombobox(
                rightcontainer, "Trigger Type:");
            _triggertype.Dock = Dock.Bottom;

            var zoom = _triggertype.AddItem("Zoom", "", TriggerType.Zoom);
            zoom.Selected += (o, e) =>
            {
                triggeroptions.Children.Clear();
                _zoomoptions.Parent = triggeroptions;
            };
            _triggertype.SelectedItem = zoom;
        }
        private void SetupLeft()
        {
            ControlBase leftcontainer = new ControlBase(this)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Left,
                AutoSizeToContents = true,
            };
            var panel = new ControlBase(leftcontainer);
            panel.Width = 150;
            panel.Height = 200;
            ControlBase topcontainer = new ControlBase(panel)
            {
                Margin = new Margin(0, 3, 0, 3),
                Padding = Padding.Five,
                Dock = Dock.Top,

                Children =
                {
                    new Button(null)
                    {
                        Text = "-",
                        Name = "btndelete",
                        Dock = Dock.Right,
                        Margin = new Margin(2,0,2,0),
                        Height = 16,
                        Width = 16,
                        AutoSizeToContents = false
                    },
                    new Button(null)
                    {
                        Text = "+",
                        Name = "btnadd",
                        Dock = Dock.Right,
                        Margin = new Margin(2,0,2,0),
                        Width = 16,
                        Height = 16,
                        AutoSizeToContents = false
                    }
                },
                AutoSizeToContents = true,

            };
            var add = topcontainer.FindChildByName("btnadd");
            var delete = topcontainer.FindChildByName("btndelete");
            add.Clicked += (o, e) =>
            {
                var trigger = new GameTrigger()
                {
                    TriggerType = TriggerType.Zoom,
                    Start = _editor.Offset,
                    End = _editor.Offset + 40,
                    ZoomTarget = 4,
                };

                _changemade = true;
                using (var trk = _editor.CreateTrackWriter())
                {
                    trk.Triggers.Add(trigger);
                    _editor.Timeline.TriggerChanged(trigger, trigger);
                    UpdateFrame();
                }
                ToggleDisable(false);
                _lbtriggers.AddRow(GetTriggerLabel(trigger), "", trigger);
                _lbtriggers.SelectByUserData(trigger);
            };
            delete.Clicked += (o, e) =>
            {
                var row = _lbtriggers.SelectedRow;
                if (row != null)
                {
                    var trigger = (GameTrigger)(row.UserData);
                    _changemade = true;
                    using (var trk = _editor.CreateTrackWriter())
                    {
                        trk.Triggers.RemoveAt(SelectedTrigger);
                        _editor.Timeline.TriggerChanged(trigger, trigger);
                        UpdateFrame();
                    }
                    _lbtriggers.Children.Remove(row);
                    ToggleDisable(true);
                }
            };
            _lbtriggers = new ListBox(panel)
            {
                Dock = Dock.Fill,
                Margin = new Margin(0, 0, 0, 5)
            };
            ControlBase spinnerContainer = new ControlBase(leftcontainer)
            {
                Margin = new Margin(0, 0, 0, 0),
                Padding = new Padding(0, 0, 50, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true
            };
            _spinnerStart = new Spinner(null)
            {
                OnlyWholeNumbers = true,
                Min = 1,
                Max = Constants.MaxFrames,
                Value = _editor.Offset
                //TODO set values
            };
            _spinnerDuration = new Spinner(null)
            {
                OnlyWholeNumbers = true,
                Min = 0,
                Max = 40 * 60 * 2,//2 minutes is enough for a zoom trigger, you crazy nuts.
                Value = 0
                //TODO set values
            };
            _spinnerStart.ValueChanged += OnTriggerTimeChanged;
            _spinnerDuration.ValueChanged += OnTriggerTimeChanged;
            GwenHelper.CreateLabeledControl(
                spinnerContainer,
                "Duration:  ",
                _spinnerDuration).Dock = Dock.Bottom;
            GwenHelper.CreateLabeledControl(
                spinnerContainer,
                "Start:",
                _spinnerStart).Dock = Dock.Bottom;


            _lbtriggers.RowSelected += OnTriggerSelected;
        }
        private void OnTriggerTimeChanged(object o, EventArgs e)
        {
            if (_selecting_trigger)
                return;
            using (var trk = _editor.CreateTrackWriter())
            {
                var trigger = BeginModifyTrigger(trk);
                if (trigger != null)
                {
                    EndModifyTrigger(trigger, trk);
                    var selected = _lbtriggers.SelectedRow;
                    selected.Text = GetTriggerLabel(trigger);
                    selected.UserData = trigger;
                }
                UpdateFrame();
            }
        }
        private void ToggleDisable(bool disabled)
        {
            _spinnerStart.IsDisabled = disabled;
            _spinnerDuration.IsDisabled = disabled;
            _slider.IsDisabled = disabled;
        }
        private bool _selecting_trigger = false;
        private void OnTriggerSelected(object o, ItemSelectedEventArgs e)
        {
            if (e.SelectedItem == null)
            {
                ToggleDisable(true);
            }
            else
            {
                ToggleDisable(false);
            }
            var trigger = (GameTrigger)e.SelectedItem.UserData;
            try
            {
                _selecting_trigger = true;
                _spinnerStart.Value = trigger.Start;
                _spinnerDuration.Value = trigger.End - trigger.Start;
                _triggertype.SelectByUserData(trigger.TriggerType);
                if (trigger.TriggerType == TriggerType.Zoom)
                {
                    _zoomtarget.Value = trigger.ZoomTarget;
                }
            }
            finally
            {
                _selecting_trigger = false;
            }
            UpdateFrame();
        }
        private void OnSliderValueChanged(object o, EventArgs e)
        {
            UpdateFrame();
        }
        private void Setup()
        {
            ControlBase bottomcontainer = new ControlBase(this)
            {
                Margin = new Margin(0, 0, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true
            };
            _slider = new HorizontalSlider(bottomcontainer)
            {
                Dock = Dock.Bottom,
                Max = 1,
                Value = 0
            };
            _slider.ValueChanged += OnSliderValueChanged;
            SetupLeft();
            SetupRight();
            Populate();
        }
        private void Populate()
        {
            using (var trk = _editor.CreateTrackWriter())
            {
                foreach (var trigger in trk.Triggers)
                {
                    _lbtriggers.AddRow(
                        GetTriggerLabel(trigger), string.Empty, trigger);
                }
            }
        }
        private string GetTriggerLabel(GameTrigger trigger)
        {
            string typelabel = "";
            switch (trigger.TriggerType)
            {
                case TriggerType.Zoom:
                    typelabel = "[Zoom]";
                    break;
            }
            return $"{typelabel} {trigger.Start} - {trigger.End}";
        }

        private void UpdateFrame()
        {
            var start = Math.Max(_spinnerStart.Value - FramePadding, 0);
            var end = (_spinnerStart.Value + _spinnerDuration.Value + FramePadding);
            var frame = (int)(start + (end - start) * _slider.Value);
            _editor.UseUserZoom = false;
            _editor.SetFrame(frame);
            _editor.UpdateCamera();
        }
        private void CancelChange()
        {
            if (_changemade)
            {
                using (var trk = _editor.CreateTrackWriter())
                {
                    var triggers = trk.Triggers;

                    foreach (var oldtrigger in _triggers_copy)
                    {
                        bool match = false;
                        foreach (var newtrigger in triggers)
                        {
                            if (oldtrigger.CompareTo(newtrigger))
                            {
                                match = true;
                            }
                        }
                        if (!match)
                        {
                            _editor.Timeline.TriggerChanged(oldtrigger, oldtrigger);
                        }
                    }
                    trk.Triggers = _triggers_copy;
                }
                _changemade = false;
            }
        }
        private void FinishChange()
        {
            using (var trk = _editor.CreateTrackWriter())
            {
                var triggers = trk.Triggers;
                if (_changemade)
                {
                    _changemade = false;
                }
            }
        }
    }
}
