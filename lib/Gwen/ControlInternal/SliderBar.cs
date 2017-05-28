using Gwen.Controls;
using System;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Slider bar.
    /// </summary>
    public class SliderBar : Dragger
    {
        #region Properties

        /// <summary>
        /// Indicates whether the bar is horizontal.
        /// </summary>
        public bool IsHorizontal { get { return m_bHorizontal; } set { m_bHorizontal = value; } }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SliderBar"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public SliderBar(Controls.ControlBase parent)
            : base(parent)
        {
            Target = this;
            RestrictToParent = true;
        }

        #endregion Constructors

        #region Methods

        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
            base.OnMouseClickedLeft(x, y, down);
            if (!down && Tooltip != null && !Bounds.Contains(x, y))
            {
                ToolTip.Disable(Parent);
                ToolTip.Disable(this);
            }
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawSliderButton(this, IsHeld, IsHorizontal);
        }

        #endregion Methods

        #region Fields

        private bool m_bHorizontal;

        #endregion Fields
    }
}