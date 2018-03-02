using System;
using System.Collections.Generic;
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
        private static List<Key> _keysdown = new List<Key>();
        private static KeyboardState _kbstate;
        private static KeyboardState _prev_kbstate;
        private static List<MouseButton> _mousebuttonsdown = new List<MouseButton>();
        private static MouseState _last_mouse_state;
        private static bool _hasmoved = false;
        private static ResourceSync _lock = new ResourceSync();
        private static int _modifiersdown = 0;
        private static Dictionary<Hotkey, HotkeyHandler> Handlers = new Dictionary<Hotkey, HotkeyHandler>();
        private static HotkeyHandler _current_hotkey = null;

        /// "If a non modifier key was pressed, it's like the previous key 
        /// stopped being pressed"
        /// We implement that using LastPressedKey which we update on non-repeat
        /// keystokes
        private static Key LastPressedKey = Key.Unknown;
        public static void KeyDown(Key key)
        {
            LastPressedKey = key;
        }
        public static void UpdateKeysDown(KeyboardState ks)
        {
            var ret = new List<Key>();
            if (ks == _kbstate)// no thanks, we already did this one
                return;
            using (_lock.AcquireWrite())
            {
                _prev_kbstate = _kbstate;
                _kbstate = ks;
                _modifiersdown = 0;
                if (ks.IsAnyKeyDown)
                {
                    //skip key.unknown
                    //opentk.nativewindow.processevents has a similar loop
                    for (Key key = 0; key < Key.LastKey; key++)
                    {
                        if (ks.IsKeyDown(key))
                        {
                            ret.Add(key);
                            if (IsModifier(key))
                                _modifiersdown++;
                        }
                    }
                }
                _keysdown = ret;
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
        public static void ProcessKeyup()
        {
            if (_current_hotkey != null)
            {
                if (!Check(_current_hotkey.hotkey))
                {
                    _current_hotkey.keyuphandler?.Invoke();
                    _current_hotkey = null;
                }
            }
        }
        public static void ProcessHotkeys()
        {
            if (_current_hotkey != null)
            {
                if (Check(_current_hotkey.hotkey) && _current_hotkey.condition())
                {
                    if (_current_hotkey.repeat)
                    {
                        _current_hotkey.keydownhandler();
                    }
                    return;
                }
                else
                {
                    _current_hotkey.keyuphandler?.Invoke();
                }
            }
            _current_hotkey = null;
            foreach (var pair in Handlers)
            {
                var bind = CheckInternal(pair.Key);
                if (bind != null)
                {
                    var handler = pair.Value;
                    if (handler.condition())
                    {
                        bool waspressed = CheckPressed(bind, ref _prev_kbstate);
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
                x = _last_mouse_state.X;
                y = _last_mouse_state.Y;
                return _hasmoved;
            }
        }
        public static void UpdateMouse(MouseState ms)
        {
            if (ms == _last_mouse_state)// no thanks, we already did this one
                return;
            using (_lock.AcquireWrite())
            {
                if (_last_mouse_state.X != ms.X || _last_mouse_state.Y != ms.Y)
                {
                    _hasmoved = true;
                }
                _last_mouse_state = ms;
                var ret = new List<MouseButton>();
                for (MouseButton btn = 0; btn < MouseButton.LastButton; btn++)
                {
                    if (ms.IsButtonDown(btn))
                        ret.Add(btn);
                }
                _mousebuttonsdown = ret;
            }
        }
        public static bool Check(Hotkey hotkey)
        {
            return CheckInternal(hotkey) != null;
        }
        /// <summary>
        /// Returns true if no key has been pressed that changes the definition
        /// of the "currently pressed" keybind.
        /// </summary>
        private static bool IsKeybindExclusive(Keybinding bind)
        {
            int allowedkeys = bind.KeysDown;
            var keysdown = _keysdown.Count;
            if (allowedkeys > 0 && !IsModifier(bind.Key))
            {
                if ((bind.UsesModifiers && keysdown > allowedkeys) ||
                (!bind.UsesModifiers && _modifiersdown != 0) ||
                bind.Key != LastPressedKey)//someone overrode us
                    return false;
            }
            if (bind.UsesMouse)
            {
                if (_mousebuttonsdown.Count > 1)
                    return false;
            }
            return true;
        }
        private static Keybinding CheckInternal(Hotkey hotkey)
        {
            List<Keybinding> keybindings;
            if (Settings.Keybinds.TryGetValue(hotkey, out keybindings))
            {
                using (_lock.AcquireRead())
                {
                    foreach (var bind in keybindings)
                    {
                        if (!IsKeybindExclusive(bind) || bind.IsEmpty)
                        {
                            continue;
                        }
                        if (CheckPressed(bind, ref _kbstate))
                            return bind;
                    }
                }
            }
            return null;
        }
        private static bool CheckPressed(Keybinding bind, ref KeyboardState state)
        {
            if (bind.Key != (Key)(-1))
            {
                if (!state.IsKeyDown(bind.Key))
                    return false;
            }
            if (bind.MouseButton != (MouseButton)(-1))
            {
                if (!_last_mouse_state.IsButtonDown(bind.MouseButton))
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
                    return true;
            }
            return false;
        }
    }
}