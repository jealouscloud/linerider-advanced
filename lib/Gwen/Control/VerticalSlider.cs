using System;
using System.Drawing;

namespace Gwen.Controls
{
    /// <summary>
    /// Vertical slider.
    /// </summary>
    public class VerticalSlider : Slider
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalSlider"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public VerticalSlider(ControlBase parent)
            : base(parent)
        {
            m_SliderBar.IsHorizontal = false;
        }

        #endregion Constructors

        #region Methods

        protected override float CalculateValue()
        {
            return 1 - m_SliderBar.Y / (float)(Height - m_SliderBar.Height);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            m_SliderBar.SetSize(Width, 15);
            UpdateBarFromValue();
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
            m_SliderBar.MoveTo(m_SliderBar.X, (int)(CanvasPosToLocal(new Point(x, y)).Y - m_SliderBar.Height * 0.5));
            m_SliderBar.InputMouseClickedLeft(x, y, down);
            OnMoved(m_SliderBar, EventArgs.Empty);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawSlider(this, false, m_SnapToNotches ? m_NotchCount : 0, m_SliderBar.Height);
        }

        protected override void UpdateBarFromValue()
        {
            m_SliderBar.MoveTo(m_SliderBar.X, (int)((Height - m_SliderBar.Height) * (1 - m_Value)));
        }

        #endregion Methods
    }
}