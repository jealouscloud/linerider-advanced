using System;
using Gwen.Controls;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Submenu indicator.
    /// </summary>
    public class RightArrow : Controls.ControlBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RightArrow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public RightArrow(Controls.ControlBase parent)
            : base(parent)
        {
            MouseInputEnabled = false;
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawMenuRightArrow(this);
        }
    }
}
