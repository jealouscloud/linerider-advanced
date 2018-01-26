using System;
using System.Collections.Generic;
using OpenTK.Input;
namespace linerider.UI
{
    public class Keybinding
    {
        public MouseButton MouseButton = (MouseButton)(-1);
        public Key Key = (Key)(-1);
        public KeyModifiers Modifiers = (KeyModifiers)(0);
        public bool IsEmpty => (Modifiers == (KeyModifiers)(0) && Key == (Key)(-1) && MouseButton == (MouseButton)(-1));
        public int KeysDown
        {
            get
            {
                int ret = 0;
                if (Modifiers.HasFlag(KeyModifiers.Alt))
                    ret++;
                if (Modifiers.HasFlag(KeyModifiers.Shift))
                    ret++;
                if (Modifiers.HasFlag(KeyModifiers.Control))
                    ret++;
                if (this.Key != (Key) - 1)
                    ret++;
                return ret;
            }
        }

        public Keybinding()
        {
        }
        public Keybinding(MouseButton mouse, Key key, KeyModifiers modifiers)
        {
            Key = key;
            MouseButton = mouse;
            Modifiers = modifiers;
        }
        public Keybinding(Key key, KeyModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
        public Keybinding(Key key)
        {
            Key = key;
        }
        public Keybinding(MouseButton mouse)
        {
            MouseButton = mouse;
        }
        public Keybinding(MouseButton mouse, KeyModifiers modifiers)
        {
            MouseButton = mouse;
            Modifiers = modifiers;
        }
    }
}