using System;
using Gwen.Controls;

namespace Gwen.Input
{
    /// <summary>
    /// Keyboard state.
    /// </summary>
    public class KeyData
    {
        public readonly bool[] KeyState;
        public readonly float [] NextRepeat;
        public Controls.ControlBase Target;
        public bool LeftMouseDown;
        public bool RightMouseDown;

        public KeyData()
        {
            KeyState = new bool[(int)Key.Count];
            NextRepeat = new float[(int)Key.Count];
            // everything is initialized to 0 by default
        }
    }
}
