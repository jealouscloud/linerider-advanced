using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;

namespace linerider.UI
{
    public class RebindHotkeyWindow : DialogBase
    {
        private Keybinding _binding = null;
        private Keybinding _lasthit = null;
        private Label _kblabel;
        public bool ModifierOnly = false;
        public event EventHandler<Keybinding> KeybindChanged;
        public RebindHotkeyWindow(GameCanvas parent, string label) : base(parent, null)

        {
            _binding = new Keybinding();
            _lasthit = _binding;
            Title = $"Rebind \"{label}\" (ESC to cancel)";
            SetSize(350, 110);
            DisableResizing();
            MakeModal(true);
            Setup();
            KeyboardInputEnabled = true;
            Gwen.Input.InputHandler.KeyboardFocus = this;
        }

        protected override bool OnKeyEscape(bool down)
        {
            Close();
            return true;
        }
        protected override bool OnKeyReturn(bool down)
        {
            if (down)
            {
                Close();
                if (!_binding.IsEmpty && KeybindChanged != null)
                {
                    KeybindChanged.Invoke(this, _binding);
                }
            }
            return true;
        }
        private void Setup()
        {
            var labeldesc = new Label(this)
            {
                Dock = Dock.Top,
                AutoSizeToContents = true,
                Alignment = Pos.CenterH | Pos.Top,
                Text = "Enter your new key combination and press enter",
            };
            Panel container = new Panel(this)
            {
                Dock = Dock.Bottom,
                Padding = new Padding(0, 10, 0, 10),
                MouseInputEnabled = false,
                KeyboardInputEnabled = false,
                AutoSizeToContents = true,
            };
            _kblabel = new Label(container)
            {
                Dock = Dock.Fill,
                Alignment = Pos.Center,
                Text = _binding.ToString(),
            };
        }
        public override void Think()
        {
            base.Think();
            if (IsOnTop)
            {
                var hk = InputUtils.ReadHotkey();
                bool changemade = false;
                if (ModifierOnly)
                {
                    hk.Key = (OpenTK.Input.Key)(-1);
                }
                if (!hk.IsEmpty)
                {
                    if (_lasthit.IsEmpty)
                    {
                        _binding = hk;
                        changemade = true;
                    }
                    else
                    {
                        if (hk.UsesModifiers)
                        {
                            var mods = InputUtils.SplitModifiers(hk.Modifiers);
                            var oldmods = InputUtils.SplitModifiers(_binding.Modifiers);
                            foreach (var v in mods)
                            {
                                if (!oldmods.Contains(v))
                                    changemade = true;
                            }
                        }
                        if (hk.UsesKeys && hk.Key != _binding.Key ||
                            hk.UsesMouse && hk.MouseButton != _binding.MouseButton)
                        {
                            changemade = true;
                        }
                    }
                    if (changemade)
                    {
                        _binding = hk;
                        _kblabel.Text = _binding.ToString();
                    }
                }
                if (!hk.IsBindingEqual(_lasthit))
                {
                    _lasthit = hk;
                }
            }
        }
    }
}
