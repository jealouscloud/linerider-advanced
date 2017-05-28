using System;
using Gwen.Controls;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Drag&drop highlight.
    /// </summary>
    public class Highlight : Controls.ControlBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Highlight"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Highlight(Controls.ControlBase parent) : base(parent)
        {
            
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawHighlight(this);
        }
    }
}
