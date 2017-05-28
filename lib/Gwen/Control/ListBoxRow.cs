using System;
using System.Drawing;
using Gwen.Controls.Layout;

namespace Gwen.Controls
{
    /// <summary>
    /// List box row (selectable).
    /// </summary>
    public class ListBoxRow : TableRow
    {
        private bool m_Selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxRow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public ListBoxRow(ControlBase parent)
            : base(parent)
        {
            MouseInputEnabled = true;
            IsSelected = false;
        }

        /// <summary>
        /// Indicates whether the control is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return m_Selected; }
            set
            {
                m_Selected = value;             
                // TODO: Get these values from the skin.
                if (value)
                    SetTextColor(Color.White);
                else
                    SetTextColor(Color.Black);
            }
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawListBoxLine(this, IsSelected, EvenRow);
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
			base.OnMouseClickedLeft(x, y, down);
            if (down)
            {
                //IsSelected = true; // [omeg] ListBox manages that
                OnRowSelected();
            }
        }
    }
}
