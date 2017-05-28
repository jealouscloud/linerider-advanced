using System;
using Gwen.Controls;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Numeric up arrow.
    /// </summary>
    public class UpDownButton_Up : Button
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpDownButton_Up"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public UpDownButton_Up(Controls.ControlBase parent)
            : base(parent)
        {
            SetSize(7, 7);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawNumericUpDownButton(this, IsDepressed, true);
        }
    }
}
