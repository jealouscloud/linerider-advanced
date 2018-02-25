using System;
using System.Collections.Generic;
using OpenTK.Input;
using linerider.Utils;
namespace linerider.UI
{
    public enum InputState
    {
        ///Regular state where the user can do anything
        Editor,
        ///State where input events are forwarded to the tool
        Tool,
        ///Input events are forwarded to the UI
        UI
    }
}