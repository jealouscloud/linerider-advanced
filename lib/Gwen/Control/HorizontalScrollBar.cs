using System;
using System.Drawing;
using Gwen.Input;

namespace Gwen.Controls
{
    /// <summary>
    /// Horizontal scrollbar.
    /// </summary>
    public class HorizontalScrollBar : ScrollBar
    {
        /// <summary>
        /// Bar size (in pixels).
        /// </summary>
        public override int BarSize
        {
            get { return m_Bar.Width; }
            set { m_Bar.Width = value; }
        }

        /// <summary>
        /// Bar position (in pixels).
        /// </summary>
        public override int BarPos
        {
            get { return m_Bar.X - Height; }
        }

        /// <summary>
        /// Indicates whether the bar is horizontal.
        /// </summary>
        public override bool IsHorizontal
        {
            get { return true; }
        }

        /// <summary>
        /// Button size (in pixels).
        /// </summary>
        public override int ButtonSize
        {
            get { return Height; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalScrollBar"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public HorizontalScrollBar(ControlBase parent)
            : base(parent)
        {
            m_Bar.IsHorizontal = true;

            m_ScrollButton[0].SetDirectionLeft();
            m_ScrollButton[0].Clicked += NudgeLeft;

            m_ScrollButton[1].SetDirectionRight();
            m_ScrollButton[1].Clicked += NudgeRight;

            m_Bar.Dragged += OnBarMoved;
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            base.Layout(skin);

            m_ScrollButton[0].Width = Height;
            m_ScrollButton[0].Dock = Pos.Left;

            m_ScrollButton[1].Width = Height;
            m_ScrollButton[1].Dock = Pos.Right;

            m_Bar.Height = ButtonSize;
            m_Bar.Padding = new Padding(ButtonSize, 0, ButtonSize, 0);

            float barWidth = (m_ViewableContentSize / m_ContentSize) * (Width - (ButtonSize * 2));

            if (barWidth < ButtonSize * 0.5f)
                barWidth = (int)(ButtonSize * 0.5f);

            m_Bar.Width = (int)(barWidth);
            m_Bar.IsHidden = Width - (ButtonSize * 2) <= barWidth;

            //Based on our last scroll amount, produce a position for the bar
            if (!m_Bar.IsHeld)
            {
                SetScrollAmount(ScrollAmount, true);
            }
        }

		public void NudgeLeft(ControlBase control, EventArgs args)
        {
            if (!IsDisabled)
                SetScrollAmount(ScrollAmount - NudgeAmount, true);
        }

		public void NudgeRight(ControlBase control, EventArgs args)
        {
            if (!IsDisabled)
                SetScrollAmount(ScrollAmount + NudgeAmount, true);
        }

        public override void ScrollToLeft()
        {
            SetScrollAmount(0, true);
        }

        public override void ScrollToRight()
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
                if (clickPos.X < m_Bar.X)
					NudgeLeft(this, EventArgs.Empty);
                else
                    if (clickPos.X > m_Bar.X + m_Bar.Width)
						NudgeRight(this, EventArgs.Empty);

                m_Depressed = false;
                InputHandler.MouseFocus = null;
            }
        }

        protected override float CalculateScrolledAmount()
        {
            return (float)(m_Bar.X - ButtonSize) / (Width - m_Bar.Width - (ButtonSize * 2));
        }

        /// <summary>
        /// Sets the scroll amount (0-1).
        /// </summary>
        /// <param name="value">Scroll amount.</param>
        /// <param name="forceUpdate">Determines whether the control should be updated.</param>
        /// <returns>
        /// True if control state changed.
        /// </returns>
        public override bool SetScrollAmount(float value, bool forceUpdate = false)
        {
            value = Util.Clamp(value, 0, 1);

            if (!base.SetScrollAmount(value, forceUpdate))
                return false;

            if (forceUpdate)
            {
                int newX = (int)(ButtonSize + (value * ((Width - m_Bar.Width) - (ButtonSize * 2))));
                m_Bar.MoveTo(newX, m_Bar.Y);
            }

            return true;
        }

        /// <summary>
        /// Handler for the BarMoved event.
        /// </summary>
        /// <param name="control">Event source.</param>
		protected override void OnBarMoved(ControlBase control, EventArgs args)
        {
            if (m_Bar.IsHeld)
            {
                SetScrollAmount(CalculateScrolledAmount(), false);
                base.OnBarMoved(control, args);
            }
            else
                InvalidateParent();
        }
    }
}
