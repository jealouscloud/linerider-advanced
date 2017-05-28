using System;
using System.Drawing;
using Gwen.Controls;

namespace Gwen.DragDrop
{
    public class Package
    {
        public string Name;
        public object UserData;
        public bool IsDraggable;
        public Controls.ControlBase DrawControl;
        public Point HoldOffset;
    }
}
