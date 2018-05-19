using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using Gwen;
using Gwen.Controls;
namespace linerider.UI
{
    public class HotkeyWidget : ControlBase
    {
        private PropertyTree _kbtree;
        private Button _btnreset;
        private GameCanvas _canvas;
        public HotkeyWidget(ControlBase parent) : base(parent)
        {
            _canvas = (GameCanvas)parent.GetCanvas();
            _kbtree = new PropertyTree(this)
            {
                Dock = Dock.Fill,
            };
            ControlBase container = new ControlBase(this)
            {
                Margin = new Margin(0, 5, 0, 0),
                Dock = Dock.Bottom,
                AutoSizeToContents = true,
            };
            _btnreset = new Button(container)
            {
                Dock = Dock.Right,
                Text = "Default Keybindings"
            };
            _btnreset.Clicked += (o, e) =>
            {
                var box = MessageBox.Show(
                    _canvas,
                    "Are you sure you want to reset your keybindings to default settings?",
                    "Reset keybindings?",
                    MessageBox.ButtonType.OkCancel);
                box.RenameButtons("Reset");
                box.Dismissed += (_o, result) =>
                {
                    if (result == DialogResult.OK)
                    {
                        Settings.ResetKeybindings();
                        Settings.Save();
                        foreach (var kb in Settings.Keybinds)
                        {
                            var prop = GetLabel(kb.Key);
                            if (prop != null)
                            {
                                prop.Value = CreateBindingText(kb.Key);
                            }
                        }
                    }
                };
            };
            Dock = Dock.Fill;
            Setup();
        }
        private void Setup()
        {
            var editortable = _kbtree.Add("Editor", 150);
            AddBinding(editortable, "Pencil Tool", Hotkey.EditorPencilTool);
            AddBinding(editortable, "Line Tool", Hotkey.EditorLineTool);
            AddBinding(editortable, "Eraser", Hotkey.EditorEraserTool);
            AddBinding(editortable, "Select Tool", Hotkey.EditorSelectTool);
            AddBinding(editortable, "Hand Tool", Hotkey.EditorPanTool);
            AddBinding(editortable, "Quick Pan", Hotkey.EditorQuickPan);
            AddBinding(editortable, "Drag Canvas", Hotkey.EditorDragCanvas);
            AddBinding(editortable, "Move Start Point", Hotkey.EditorMoveStart,
                "Hold and click the rider to move him");
            AddBinding(editortable, "Swatch Color Blue", Hotkey.EditorToolColor1);
            AddBinding(editortable, "Swatch Color Red", Hotkey.EditorToolColor2);
            AddBinding(editortable, "Swatch Color Green", Hotkey.EditorToolColor3);
            AddBinding(editortable, "Cycle Tool Setting", Hotkey.EditorCycleToolSetting);
            AddBinding(editortable, "Toggle Onion Skinning", Hotkey.PreferenceOnionSkinning);
            AddBinding(editortable, "Focus on Rider", Hotkey.EditorFocusRider);
            AddBinding(editortable, "Focus on Flag", Hotkey.EditorFocusFlag);
            AddBinding(editortable, "Focus First Line", Hotkey.EditorFocusStart);
            AddBinding(editortable, "Focus Last Line", Hotkey.EditorFocusLastLine);
            AddBinding(editortable, "Remove Newest Line", Hotkey.EditorRemoveLatestLine);

            var tool = _kbtree.Add("Tool", 150);
            AddBinding(tool, "15° Line Snap", Hotkey.ToolXYSnap);
            AddBinding(tool, "Toggle Line Snap", Hotkey.ToolToggleSnap);
            AddBinding(tool, "Flip Line", Hotkey.LineToolFlipLine,
                "Hold before drawing a new line");

            var selecttool = _kbtree.Add("Select Tool", 150);
            AddBinding(selecttool, "Lock Angle", Hotkey.ToolAngleLock);
            AddBinding(selecttool, "Move Whole Line", Hotkey.ToolSelectBothJoints);
            AddBinding(selecttool, "Life Lock", Hotkey.ToolLifeLock,
                "While pressed moving the line will stop if the rider survives");
            AddBinding(selecttool, "Move Along Axis", Hotkey.ToolAxisLock,
                "If you're moving a whole line,\nuse this to keep it on the same plane");
            AddBinding(selecttool, "Move Along Right angle", Hotkey.ToolPerpendicularAxisLock,
            "If you're moving a whole line,\nuse this to keep perpendicular to its plane");
            AddBinding(selecttool, "Lock Length", Hotkey.ToolLengthLock);
            AddBinding(selecttool, "Copy Selection", Hotkey.ToolCopy);
            AddBinding(selecttool, "Cut", Hotkey.ToolCut);
            AddBinding(selecttool, "Paste", Hotkey.ToolPaste);
            AddBinding(selecttool, "Delete Selection", Hotkey.ToolDelete);

            var pbtable = _kbtree.Add("Playback", 150);
            AddBinding(pbtable, "Toggle Flag", Hotkey.PlaybackFlag);
            AddBinding(pbtable, "Reset Camera", Hotkey.PlaybackResetCamera);
            AddBinding(pbtable, "Start Track", Hotkey.PlaybackStart);
            AddBinding(pbtable, "Start Track before Flag", Hotkey.PlaybackStartIgnoreFlag);
            AddBinding(pbtable, "Start Track in Slowmo", Hotkey.PlaybackStartSlowmo);
            AddBinding(pbtable, "Stop Track", Hotkey.PlaybackStop);
            AddBinding(pbtable, "Toggle Pause", Hotkey.PlaybackTogglePause);
            AddBinding(pbtable, "Frame Next", Hotkey.PlaybackFrameNext);
            AddBinding(pbtable, "Frame Previous", Hotkey.PlaybackFramePrev);
            AddBinding(pbtable, "Iteration Next", Hotkey.PlaybackIterationNext);
            AddBinding(pbtable, "Iteration Previous", Hotkey.PlaybackIterationPrev);
            AddBinding(pbtable, "Hold -- Forward", Hotkey.PlaybackForward);
            AddBinding(pbtable, "Hold -- Rewind", Hotkey.PlaybackBackward);
            AddBinding(pbtable, "Increase Playback Rate", Hotkey.PlaybackSpeedUp);
            AddBinding(pbtable, "Decrease Playback Rate", Hotkey.PlaybackSpeedDown);
            AddBinding(pbtable, "Zoom In", Hotkey.PlaybackZoom);
            AddBinding(pbtable, "Zoom Out", Hotkey.PlaybackUnzoom);
            AddBinding(pbtable, "Play Button - Ignore Flag", Hotkey.PlayButtonIgnoreFlag);

            var misctable = _kbtree.Add("Misc", 150);
            AddBinding(misctable, "Quicksave", Hotkey.Quicksave);
            AddBinding(misctable, "Open Preferences", Hotkey.PreferencesWindow);
            AddBinding(misctable, "Open Game Menu", Hotkey.GameMenuWindow);
            AddBinding(misctable, "Open Track Properties", Hotkey.TrackPropertiesWindow);
            AddBinding(misctable, "Load Track", Hotkey.LoadWindow);
            _kbtree.ExpandAll();
        }
        private List<Keybinding> FetchBinding(Hotkey hotkey)
        {
            if (!Settings.Keybinds.ContainsKey(hotkey))
                Settings.Keybinds[hotkey] = new List<Keybinding>();
            var ret = Settings.Keybinds[hotkey];
            if (ret.Count == 0)
                ret.Add(new Keybinding());//empty
            return ret;
        }
        private string CreateBindingText(Hotkey hotkey)
        {
            var hk = FetchBinding(hotkey);
            string hkstring = "";
            for (int i = 0; i < hk.Count; i++)
            {
                if (hkstring.Length != 0)
                {
                    hkstring += " | ";
                }
                hkstring += hk[i].ToString();
            }
            return hkstring;
        }
        private void AddBinding(PropertyTable table, string label, Hotkey hotkey, string tooltip = null)
        {
            var hk = FetchBinding(hotkey);
            string hkstring = CreateBindingText(hotkey);
            LabelProperty prop = new LabelProperty(null)
            {
                Value = hkstring,
                Name = hotkey.ToString(),
            };
            var row = table.Add(label, prop);
            if (tooltip != null)
            {
                row.Tooltip = tooltip;
            }
            prop.Clicked += (o, e) =>
            {
                ShowHotkeyWindow(hotkey, prop, 0);
            };
            prop.RightClicked += (o, e) =>
            {
                Menu opt = new Menu(_canvas);
                opt.AddItem("Change Primary").Clicked += (_o, _e) =>
                {
                    ShowHotkeyWindow(hotkey, prop, 0);
                };
                opt.AddItem("Change Secondary").Clicked += (_o, _e) =>
                {
                    ShowHotkeyWindow(hotkey, prop, 1);
                };
                opt.AddItem("Remove Secondary").Clicked += (_o, _e) =>
                {
                    var k = Settings.Keybinds[hotkey];
                    if (k.Count > 1)
                    {
                        k.RemoveAt(1);
                        prop.Value = CreateBindingText(hotkey);
                        Settings.Save();
                    }
                };
                opt.AddItem("Restore Default").Clicked += (_o, _e) =>
                {
                    var def = Settings.GetHotkeyDefault(hotkey);
                    if (def != null && def.Count != 0)
                    {
                        int idx = 0;
                        var keys = Settings.Keybinds[hotkey];
                        keys.Clear();
                        foreach (var defaultbind in def)
                        {
                            var conflict = Settings.CheckConflicts(defaultbind, hotkey);
                            if (conflict != Hotkey.None)
                                RemoveKeybind(conflict, defaultbind);
                            ChangeKeybind(prop, hotkey, idx++, defaultbind);
                        }
                        Settings.Save();
                    }
                };
                opt.Open(Pos.Center);
            };
        }
        private LabelProperty GetLabel(Hotkey hotkey)
        {
            var ret = _kbtree.FindChildByName(hotkey.ToString(), true);
            if (ret != null)
                return (LabelProperty)ret;
            return null;
        }
        private void ShowHotkeyWindow(Hotkey hotkey, LabelProperty prop, int kbindex)
        {
            PropertyRow row = (PropertyRow)prop.Parent;
            var wnd = new RebindHotkeyWindow(_canvas, row.Label.ToString());
            wnd.KeybindChanged += (x, newbind) =>
            {
                TryNewKeybind(hotkey, newbind, kbindex);
            };
            wnd.ShowCentered();
        }
        private void RemoveKeybind(Hotkey hotkey, Keybinding binding)
        {
            var conflictkeys = Settings.Keybinds[hotkey];
            for (int i = 0; i < conflictkeys.Count; i++)
            {
                if (conflictkeys[i].IsBindingEqual(binding))
                {
                    var conflictprop = GetLabel(hotkey);
                    conflictkeys.RemoveAt(i);
                    conflictprop.Value = CreateBindingText(hotkey);
                    break;
                }
            }
        }
        private bool TryNewKeybind(Hotkey hotkey, Keybinding newbind, int kbindex)
        {
            var k = Settings.Keybinds[hotkey];
            var conflict = CheckConflicts(newbind, hotkey);
            if (conflict == hotkey)
                return true;
            var prop = GetLabel(hotkey);
            if (conflict != Hotkey.None)
            {
                var mbox = MessageBox.Show(_canvas,
                    $"Keybinding conflicts with {conflict}, If you proceed you will overwrite it.\nDo you want to continue?",
                    "Conflict detected", MessageBox.ButtonType.OkCancel);
                mbox.Dismissed += (o, e) =>
                {
                    if (e == DialogResult.OK)
                    {
                        RemoveKeybind(conflict, newbind);
                        ChangeKeybind(prop, hotkey, kbindex, newbind);
                    }
                };
                return false;
            }
            ChangeKeybind(prop, hotkey, kbindex, newbind);
            return true;
        }
        private void ChangeKeybind(LabelProperty prop, Hotkey hotkey, int kbindex, Keybinding kb)
        {
            var k = Settings.Keybinds[hotkey];
            if (kbindex >= k.Count)
            {
                k.Add(kb);
            }
            else
            {
                Settings.Keybinds[hotkey][kbindex] = kb;
            }
            prop.Value = CreateBindingText(hotkey);
            Settings.Save();
        }
        private Hotkey CheckConflicts(Keybinding keybinding, Hotkey hotkey)
        {
            if (!keybinding.IsEmpty)
            {
                var inputconflicts = Settings.KeybindConflicts[hotkey];
                foreach (var keybinds in Settings.Keybinds)
                {
                    var hk = keybinds.Key;
                    var conflicts = Settings.KeybindConflicts[hk];
                    //if the conflicts is equal to or below inputconflicts
                    //then we can compare for conflict
                    //if conflicts is above inputconflicts, ignore
                    if (inputconflicts.HasFlag(conflicts))
                    {
                        foreach (var keybind in keybinds.Value)
                        {
                            if (keybind.IsBindingEqual(keybinding))
                                return hk;
                        }
                    }
                }
            }
            return Hotkey.None;
        }
    }
}