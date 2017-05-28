using System;
using Gwen.Controls;

namespace Gwen
{
    /// <summary>
    /// Utility class for manipulating control's position according to its parent. Rarely needed, use control.Dock.
    /// </summary>
    public static class Align
    {
        /// <summary>
        /// Centers the control inside its parent.
        /// </summary>
        /// <param name="control">Control to center.</param>
        public static void Center(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (parent == null) 
                return;
            control.SetPosition(
                parent.Padding.Left + control.Margin.Left +(((parent.Width - parent.Padding.Left - parent.Padding.Right) - control.Width)/2),
                control.Margin.Top + (parent.Height - control.Height)/2);
        }

        /// <summary>
        /// Stretches the control inside it's parent
        /// </summary>
        public static void StretchHorizontally(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (parent == null)
                return;
            control.SetPosition(control.Margin.Left + parent.Padding.Left, control.Y);
            control.Width = parent.Width - parent.Padding.Right;
        }

        /// <summary>
        /// Moves the control to the left of its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void AlignLeft(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;

            control.SetPosition(parent.Padding.Left + control.Margin.Left, control.Y);
        }

        /// <summary>
        /// Centers the control horizontally inside its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void CenterHorizontally(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;


            control.SetPosition(parent.Padding.Left + control.Margin.Left + (((parent.Width - parent.Padding.Left - parent.Padding.Right) - control.Width) / 2), control.Y + control.Margin.Top);
        }

        /// <summary>
        /// Moves the control to the right of its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void AlignRight(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;


            control.SetPosition(parent.Width - control.Width - parent.Padding.Right - control.Margin.Right, control.Y);
        }

        /// <summary>
        /// Moves the control to the top of its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void AlignTop(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;

            control.SetPosition(control.X, control.Margin.Top + parent.Padding.Top);
        }

        /// <summary>
        /// Centers the control vertically inside its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void CenterVertically(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;

            control.SetPosition(control.X + control.Margin.Left, ((parent.Height - control.Height) / 2) + control.Margin.Top);
        }

        /// <summary>
        /// Moves the control to the bottom of its parent.
        /// </summary>
        /// <param name="control"></param>
        public static void AlignBottom(Controls.ControlBase control)
        {
            Controls.ControlBase parent = control.Parent;
            if (null == parent) return;
            
            control.SetPosition(control.X, (parent.Height - control.Height) - (control.Margin.Bottom + parent.Padding.Bottom));
        }

        /// <summary>
        /// Places the control below other control (left aligned), taking margins into account.
        /// </summary>
        /// <param name="control">Control to place.</param>
        /// <param name="anchor">Anchor control.</param>
        /// <param name="spacing">Optional spacing.</param>
        public static void PlaceDownLeft(Controls.ControlBase control, Controls.ControlBase anchor, int spacing = 0)
        {
            control.SetPosition(anchor.X + control.Margin.Left, anchor.Bottom + spacing + control.Margin.Top);
        }

        /// <summary>
        /// Places the control to the right of other control (bottom aligned), taking margins into account.
        /// </summary>
        /// <param name="control">Control to place.</param>
        /// <param name="anchor">Anchor control.</param>
        /// <param name="spacing">Optional spacing.</param>
        public static void PlaceRightBottom(Controls.ControlBase control, Controls.ControlBase anchor, int spacing = 0)
        {
            control.SetPosition(anchor.Right + spacing + control.Margin.Right, anchor.Y - control.Height + anchor.Height + control.Margin.Top);
        }
    }
}
