using System;
using System.Drawing;
using Gwen.Input;

namespace Gwen.Controls
{
    /// <summary>
    /// Vertical scrollbar.
    /// </summary>
    public class VerticalScrollBar : ScrollBar
    {
        /// <summary>
        /// Bar size (in pixels).
        /// </summary>
        public override int BarSize
        {
            get { return m_Bar.Height; }
            set { m_Bar.Height = value; }
        }

        /// <summary>
        /// Bar position (in pixels).
        /// </summary>
        public override int BarPos
        {
            get { return m_Bar.Y - Width; }
        }

        /// <summary>
        /// Button size (in pixels).
        /// </summary>
        public override int ButtonSize
        {
            get { return Width; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalScrollBar"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public VerticalScrollBar(ControlBase parent)
            : base(parent)
        {
            m_Bar.IsVertical = true;

            m_ScrollButton[0].SetDirectionUp();
            m_ScrollButton[0].Clicked += NudgeUp;

            m_ScrollButton[1].SetDirectionDown();
            m_ScrollButton[1].Clicked += NudgeDown;

            m_Bar.Dragged += OnBarMoved;
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            base.Layout(skin);

            m_ScrollButton[0].Height = Width;
            m_ScrollButton[0].Dock = Pos.Top;

            m_ScrollButton[1].Height = Width;
            m_ScrollButton[1].Dock = Pos.Bottom;

            m_Bar.Width = ButtonSize;
            m_Bar.Padding = new Padding(0, ButtonSize, 0, ButtonSize);

            float barHeight = 0.0f;
            if (m_ContentSize > 0.0f) barHeight = (m_ViewableContentSize/m_ContentSize)*(Height - (ButtonSize*2));

            if (barHeight < ButtonSize*0.5f)
                barHeight = (int) (ButtonSize*0.5f);

            m_Bar.Height = (int) (barHeight);
            m_Bar.IsHidden = Height - (ButtonSize*2) <= barHeight;

            //Based on our last scroll amount, produce a position for the bar
            if (!m_Bar.IsHeld)
            {
                SetScrollAmount(ScrollAmount, true);
            }
        }

		public virtual void NudgeUp(ControlBase control, EventArgs args)
        {
            if (!IsDisabled)
                SetScrollAmount(ScrollAmount - NudgeAmount, true);
        }

		public virtual void NudgeDown(ControlBase control, EventArgs args)
        {
            if (!IsDisabled)
                SetScrollAmount(ScrollAmount + NudgeAmount, true);
        }

        public override void ScrollToTop()
        {
            SetScrollAmount(0, true);
        }

        public override void ScrollToBottom()
        {
            SetScrollAmount(1, true);
        }

        public override float NudgeAmount
        {
            get
            {
                if (m_Depressed)
                    return m_ViewableContentSize / m_ContentSize;
                else
                    return base.NudgeAmount;
            }
            set
            {
                base.NudgeAmount = value;
            }
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
                m_Depressed = true;
                InputHandler.MouseFocus = this;
            }
            else
            {
                Point clickPos = CanvasPosToLocal(new Point(x, y));
                if (clickPos.Y < m_Bar.Y)
                    NudgeUp(this, EventArgs.Empty);
                else if (clickPos.Y > m_Bar.Y + m_Bar.Height)
                    NudgeDown(this, EventArgs.Empty);

                m_Depressed = false;
                InputHandler.MouseFocus = null;
            }
        }

        protected override float CalculateScrolledAmount()
        {
            return (float)(m_Bar.Y - ButtonSize) / (Height - m_Bar.Height - (ButtonSize * 2));
        }

        /// <summary>
        /// Sets the scroll amount (0-1).
        /// </summary>
        /// <param name="value">Scroll amount.</param>
        /// <param name="forceUpdate">Determines whether the control should be updated.</param>
        /// <returns>True if control state changed.</returns>
        public override bool SetScrollAmount(float value, bool forceUpdate = false)
        {
            value = Util.Clamp(value, 0, 1);

            if (!base.SetScrollAmount(value, forceUpdate))
                return false;

            if (forceUpdate)
            {
                int newY = (int)(ButtonSize + (value * ((Height - m_Bar.Height) - (ButtonSize * 2))));
                m_Bar.MoveTo(m_Bar.X, newY);
            }

            return true;
        }

        /// <summary>
        /// Handler for the BarMoved event.
        /// </summary>
        /// <param name="control">The control.</param>
		protected override void OnBarMoved(ControlBase control, EventArgs args)
        {
            if (m_Bar.IsHeld)
            {
                SetScrollAmount(CalculateScrolledAmount(), false);
				base.OnBarMoved(control, EventArgs.Empty);
            }
            else
                InvalidateParent();
        }
    }
}
