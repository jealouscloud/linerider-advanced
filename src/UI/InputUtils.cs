using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using linerider.Utils;

namespace linerider.UI
{
    static class InputUtils
    {
        /// how hotkeys ive observed in applications work
        /// 
        /// if a modifier is pressed and a key is pressed and the modifier does 
        /// not hit a special case, the normal case not play
        /// 
        /// if a key is pressed then a modifier is pressed, regardless of if 
        /// the previous key hit, it switches over to the modifier version
        /// 
        /// if that modifier is released, the key will resume without modifiers
        /// 
        /// if another non mod key is pressed, it's like the previous 
        /// non-modifier key was never pressed.

        private class HotkeyHandler
        {
            public Hotkey hotkey = Hotkey.None;
            public bool repeat = false;
            public Func<bool> condition = null;
            public Action keydownhandler = null;
            public Action keyuphandler = null;
        }
        private static GameWindow _window;
        private static KeyboardState _kbstate;
        private static KeyboardState _prev_kbstate;
        private static List<MouseButton> _mousebuttonsdown = new List<MouseButton>();
        private static MouseState _mousestate;
        private static MouseState _prev_mousestate;
        private static bool _hasmoved = false;
        private static ResourceSync _lock = new ResourceSync();
        private static KeyModifiers _modifiersdown;
        private static Dictionary<Hotkey, HotkeyHandler> Handlers = new Dictionary<Hotkey, HotkeyHandler>();
        private static HotkeyHandler _current_hotkey = null;
        // macos has to handle ctrl+ combos differently.
        private static bool _macOS = false;
        /// "If a non modifier key was pressed, it's like the previous key 
        /// stopped being pressed"
        /// We implement that using RepeatKey which we update on non-repeat
        /// keystokes
        private static Key RepeatKey = Key.Unknown;
        public static List<KeyModifiers> SplitModifiers(KeyModifiers modifiers)
        {
            List<KeyModifiers> ret = new List<KeyModifiers>();
            if (modifiers.HasFlag(KeyModifiers.Control))
            {
                ret.Add(KeyModifiers.Control);
            }
            if (modifiers.HasFlag(KeyModifiers.Shift))
            {
                ret.Add(KeyModifiers.Shift);
            }
            if (modifiers.HasFlag(KeyModifiers.Alt))
            {
                ret.Add(KeyModifiers.Alt);
            }
            return ret;
        }
        public static Keybinding ReadHotkey()
        {
            var key = RepeatKey;
            if (!_kbstate[key])
                key = (Key)(-1);
            if (_mousebuttonsdown.Count == 1 && key == (Key)(-1))
            {
                var button = _mousebuttonsdown[0];
                if (button == MouseButton.Left || button == MouseButton.Right)
                    return new Keybinding();//ignore
                return new Keybinding(button, _modifiersdown);
            }
            return new Keybinding(key, _modifiersdown);
        }
        public static void KeyDown(Key key)
        {
            if (!IsModifier(key))
                RepeatKey = key;
        }
        public static void UpdateKeysDown(KeyboardState ks, KeyModifiers modifiers)
        {
            using (_lock.AcquireWrite())
            {
                _prev_kbstate = _kbstate;
                _kbstate = ks;
                _modifiersdown = modifiers;
            }
        }
        public static void RegisterHotkey(
            Hotkey hotkey,
            Func<bool> condition,
            Action onkeydown,
            Action onkeyup = null,
            bool repeat = false)
        {
            Handlers.Add(hotkey, new HotkeyHandler()
            {
                hotkey = hotkey,
                condition = condition,
                keydownhandler = onkeydown,
                keyuphandler = onkeyup,
                repeat = repeat
            });
        }
        /// <summary>
        /// Checks if the currently pressed hotkey is still 'pressed' after a
        /// state change.
        /// </summary>
        public static bool CheckCurrentHotkey()
        {
            if (_current_hotkey != null)
            {
                if (Check(_current_hotkey.hotkey) &&
                    _current_hotkey.condition())
                {
                    return true;
                }
                _current_hotkey.keyuphandler?.Invoke();
                _current_hotkey = null;
            }
            return false;
        }
        public static void ProcessMouseHotkeys()
        {
            CheckCurrentHotkey();
            foreach (var pair in Handlers)
            {
                var bind = CheckInternal(pair.Key, true);
                if (bind != null && bind.UsesMouse)
                {
                    var handler = pair.Value;
                    if (handler.condition())
                    {
                        bool waspressed = CheckPressed(
                            bind, 
                            ref _prev_kbstate, 
                            ref _prev_mousestate);
                        if (waspressed)
                        {
                            continue;
                        }
                        _current_hotkey?.keyuphandler?.Invoke();
                        _current_hotkey = handler;
                        handler.keydownhandler();
                        break;
                    }
                }
            }
        }
        public static void ProcessKeyboardHotkeys()
        {
            if (CheckCurrentHotkey())
            {
                var kb = CheckInternal(_current_hotkey.hotkey, true);
                if (!kb.UsesMouse && _current_hotkey.repeat)
                {
                    _current_hotkey.keydownhandler();
                }
                return;
            }
            _current_hotkey = null;
            foreach (var pair in Handlers)
            {
                var bind = CheckInternal(pair.Key, false);
                if (bind != null)
                {
                    var handler = pair.Value;
                    if (handler.condition())
                    {
                        bool waspressed = CheckPressed(
                            bind, 
                            ref _prev_kbstate, 
                            ref _prev_mousestate);
                        if (waspressed && !handler.repeat)
                        {
                            continue;
                        }
                        handler.keydownhandler();
                        _current_hotkey = handler;
                        break;
                    }
                }
            }
        }
        public static bool HandleMouseMove(out int x, out int y)
        {
            using (_lock.AcquireWrite())
            {
                x = _mousestate.X;
                y = _mousestate.Y;
                if (_hasmoved)
                {
                    _hasmoved = false;
                    return true;
                }
                return false;
            }
        }
        public static void UpdateMouse(MouseState ms)
        {
            using (_lock.AcquireWrite())
            {
                if (_mousestate.X != ms.X || _mousestate.Y != ms.Y)
                {
                    _hasmoved = true;
                }
                _prev_mousestate = _mousestate;
                _mousestate = ms;
                _mousebuttonsdown.Clear();
                for (MouseButton btn = 0; btn < MouseButton.LastButton; btn++)
                {
                    if (_mousestate[btn])
                        _mousebuttonsdown.Add(btn);
                }
            }
        }
        public static Vector2d GetMouse()
        {
            return new Vector2d(_mousestate.X, _mousestate.Y);
        }
        public static bool Check(Hotkey hotkey)
        {
            return CheckInternal(hotkey, true) != null;
        }
        public static bool CheckPressed(Hotkey hotkey)
        {
            List<Keybinding> keybindings;
            if (Settings.Keybinds.TryGetValue(hotkey, out keybindings))
            {
                using (_lock.AcquireRead())
                {
                    foreach (var bind in keybindings)
                    {
                        if (bind.IsEmpty)
                        {
                            continue;
                        }
                        if (CheckPressed(bind, ref _kbstate, ref _mousestate))
                            return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Returns true if no key has been pressed that changes the definition
        /// of the "currently pressed" keybind.
        /// </summary>
        private static bool IsKeybindExclusive(Keybinding bind)
        {
            if (bind.UsesModifiers && !bind.UsesKeys)
                return true;
            if (bind.UsesKeys)
            {
                if (_modifiersdown != bind.Modifiers ||
                bind.Key != RepeatKey)//someone overrode us
                    return false;
            }
            if (bind.UsesMouse)
            {
                //we can conflict with left/right, not others
                int buttonsdown = _mousebuttonsdown.Count;
                if (_mousestate[MouseButton.Left])
                    buttonsdown--;
                if (_mousestate[MouseButton.Right])
                    buttonsdown--;
                if (buttonsdown > 1)
                    return false;
            }
            return true;
        }
        private static Keybinding CheckInternal(Hotkey hotkey, bool checkmouse)
        {
            List<Keybinding> keybindings;
            if (Settings.Keybinds.TryGetValue(hotkey, out keybindings))
            {
                using (_lock.AcquireRead())
                {
                    foreach (var bind in keybindings)
                    {
                        if (bind.IsEmpty ||
                            (bind.UsesMouse && !checkmouse) ||
                            !IsKeybindExclusive(bind))
                        {
                            continue;
                        }
                        if (CheckPressed(bind, ref _kbstate, ref _mousestate))
                            return bind;
                    }
                }
            }
            return null;
        }
        private static bool CheckPressed(Keybinding bind, ref KeyboardState state, ref MouseState mousestate)
        {
            if (_window != null && !_window.Focused)
                return false;
            if (bind.Key != (Key)(-1))
            {
                if (!state.IsKeyDown(bind.Key))
                {
                    if (_macOS)
                    {
                        // We remap command to control here.
                        // Ctrl+ keys aren't working properly on osx
                        // I don't know of a better way to handle this platform
                        // issue.
                        switch (bind.Key)
                        {
                            case Key.ControlLeft:
                                if (!state.IsKeyDown(Key.WinLeft))
                                    return false;
                                break;
                            case Key.ControlRight:
                                if (!state.IsKeyDown(Key.WinRight))
                                    return false;
                                break;
                            default:
                                return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (bind.UsesMouse)
            {
                if (!mousestate.IsButtonDown(bind.MouseButton))
                    return false;
            }
            if (bind.Modifiers != (KeyModifiers)(0))
            {
                var alt =
                state.IsKeyDown(Key.AltLeft) ||
                state.IsKeyDown(Key.AltRight);
                var ctrl =
                state.IsKeyDown(Key.ControlLeft) ||
                state.IsKeyDown(Key.ControlRight);
                if (_macOS)
                {
                    // Remap the command key to ctrl.
                    ctrl |=
                    state.IsKeyDown(Key.WinLeft) ||
                    state.IsKeyDown(Key.WinRight);
                }
                var shift =
                state.IsKeyDown(Key.ShiftLeft) ||
                state.IsKeyDown(Key.ShiftRight);

                if ((bind.Modifiers.HasFlag(KeyModifiers.Alt) && !alt) ||
                (bind.Modifiers.HasFlag(KeyModifiers.Shift) && !shift) ||
                (bind.Modifiers.HasFlag(KeyModifiers.Control) && !ctrl))
                    return false;
            }
            return !bind.IsEmpty;
        }
        private static bool IsModifier(Key key)
        {
            switch (key)
            {
                case Key.AltLeft:
                case Key.AltRight:
                case Key.ShiftLeft:
                case Key.ShiftRight:
                case Key.ControlLeft:
                case Key.ControlRight:
                case Key.WinLeft:
                case Key.WinRight:
                    return true;
            }
            return false;
        }
        public static void SetWindow(GameWindow window)
        {
            if (window == null)
                throw new NullReferenceException("InputUtils SetWindow cannot be null");
            _window = window;
            _macOS = OpenTK.Configuration.RunningOnMacOS;
        }
    }
}