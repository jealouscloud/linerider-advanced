using System;
using System.Collections.Generic;
using OpenTK.Input;
using linerider.Utils;
namespace linerider.UI
{
    static class InputUtils
    {
        public static List<Key> KeysDown { get; private set; } = new List<Key>();
        private static KeyboardState _last_kb_state;
        public static List<MouseButton> MouseButtonsDown { get; private set; } = new List<MouseButton>();
        private static MouseState _last_mouse_state;
        private static ResourceSync _lock = new ResourceSync();
        private static int _modifiersdown = 0;
        public static void UpdateKeysDown(KeyboardState ks)
        {
            var ret = new List<Key>();
            if (ks == _last_kb_state)// no thanks, we already did this one
                return;
            using (_lock.AcquireWrite())
            {
                _last_kb_state = ks;
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
                KeysDown = ret;
            }
        }
        public static void UpdateMouseDown(MouseState ms)
        {
            if (ms == _last_mouse_state)// no thanks, we already did this one
                return;
            using (_lock.AcquireWrite())
            {
                _last_mouse_state = ms;
                var ret = new List<MouseButton>();
                for (MouseButton btn = 0; btn < MouseButton.LastButton; btn++)
                {
                    if (ms.IsButtonDown(btn))
                        ret.Add(btn);
                }
                MouseButtonsDown = ret;
            }
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
        private static bool RegularExclusiveCHeck(Keybinding bind)
        {
            int allowedkeys = bind.KeysDown;
            var keysdown = KeysDown.Count;
            if (allowedkeys > 0)
            {
                if ((bind.UsesModifiers && keysdown != allowedkeys) || 
                (!bind.UsesModifiers && _modifiersdown!=0))
                return false;
            }
            if (bind.UsesMouse)
            {
                if (MouseButtonsDown.Count > 1)
                    return false;
            }
            return true;
        }
        public static bool Check(Hotkey hotkey, bool trulyexclusive = false)
        {
            List<Keybinding> keybindings;
            if (Settings.Keybinds.TryGetValue(hotkey, out keybindings))
            {
                foreach (var bind in keybindings)
                {
                    using (_lock.AcquireRead())
                    {
                        if (trulyexclusive)
                        {
                            int allowedkeys = bind.KeysDown;
                            var keysdown = KeysDown.Count;
                            if (allowedkeys > 0 && keysdown != allowedkeys)
                                continue;
                        }
                        if (!RegularExclusiveCHeck(bind) || bind.IsEmpty)
                        {
                            continue;
                        }
                        if (bind.Key != (Key)(-1))
                        {
                            if (!_last_kb_state.IsKeyDown(bind.Key))
                                continue;
                        }
                        if (bind.MouseButton != (MouseButton)(-1))
                        {
                            if (!_last_mouse_state.IsButtonDown(bind.MouseButton))
                                continue;
                        }
                        if (bind.Modifiers != (KeyModifiers)(0))
                        {
                            if ((bind.Modifiers.HasFlag(KeyModifiers.Alt) && !_last_kb_state.IsKeyDown(Key.AltLeft) && !_last_kb_state.IsKeyDown(Key.AltRight)) ||
                            (bind.Modifiers.HasFlag(KeyModifiers.Shift) && !_last_kb_state.IsKeyDown(Key.ShiftRight) && !_last_kb_state.IsKeyDown(Key.ShiftLeft)) ||
                            (bind.Modifiers.HasFlag(KeyModifiers.Control) && !_last_kb_state.IsKeyDown(Key.ControlLeft) && !_last_kb_state.IsKeyDown(Key.ControlRight)))
                                continue;
                        }
                        return true;

                    }
                }
            }
            return false;
        }
    }
}