using System;
using System.Drawing;
using Gwen.ControlInternal;
using Gwen.Input;

namespace Gwen.Controls
{
    /// <summary>
    /// Base slider.
    /// </summary>
    public class IntSlider : ControlBase
    {
        public bool Held
        {
            get { return this.m_SliderBar.IsHeld; }
        }
        protected readonly SliderBar m_SliderBar;
        protected int m_NotchCount;
        protected int m_Value;
        protected int m_Min;
        protected int m_Max;

        /// <summary>
        /// Minimum value.
        /// </summary>
        public int Min { get { return m_Min; } set { SetRange(value, m_Max); } }

        /// <summary>
        /// Maximum value.
        /// </summary>
        public int Max { get { return m_Max; } set { SetRange(m_Min, value); } }

        /// <summary>
        /// Current value.
        /// </summary>
        public int Value
        {
            get { return m_Value; }
            set
            {
                if (value < m_Min) value = m_Min;
                if (value > m_Max) value = m_Max;
                // Normalize Value

                SetValueInternal(value);
                Redraw();
            }
        }
        public override string Tooltip
        {
            get
            {
                return base.Tooltip;
            }

            set
            {
                base.Tooltip = value;
                m_SliderBar.Tooltip = value;
            }
        }

        /// <summary>
        /// Invoked when the value has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Slider"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        protected IntSlider(ControlBase parent)
            : base(parent)
        {
            SetBounds(new Rectangle(0, 0, 32, 128));

            m_SliderBar = new SliderBar(this);
            m_SliderBar.Dragged += OnMoved;
            m_Min = 0;
            m_Max = 1;
            m_Value = 0;

            KeyboardInputEnabled = true;
            IsTabable = true;
        }
        /// <summary>
        /// Handler for Right Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyRight(bool down)
        {
            if (down)
                Value = Value + 1;
            return true;
        }

        /// <summary>
        /// Handler for Up Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyUp(bool down)
        {
            if (down)
                Value = Value + 1;
            return true;
        }

        /// <summary>
        /// Handler for Left Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyLeft(bool down)
        {
            if (down)
                Value = Value - 1;
            return true;
        }

        /// <summary>
        /// Handler for Down Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyDown(bool down)
        {
            if (down)
                Value = Value - 1;
            return true;
        }

        /// <summary>
        /// Handler for Home keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyHome(bool down)
        {
            if (down)
                Value = m_Min;
            return true;
        }

        /// <summary>
        /// Handler for End keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyEnd(bool down)
        {
            if (down)
                Value = m_Max;
            return true;
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
            if (!down && Tooltip != null && !Bounds.Contains(x, y))
            {
                ToolTip.Disable(m_SliderBar);
                ToolTip.Disable(this);
            }
        }

        protected virtual void OnMoved(ControlBase control, EventArgs args)
        {
            SetValueInternal(CalculateValue());
            if (Tooltip != null)
            {
                ToolTip.Enable(this);
            }
        }

        protected virtual int CalculateValue()
        {
            return 0;
        }

        protected virtual void UpdateBarFromValue()
        {

        }

        protected virtual void SetValueInternal(int val)
        {
            if (m_Value != val)
            {
                m_Value = val;
                if (ValueChanged != null)
                    ValueChanged.Invoke(this, EventArgs.Empty);
            }

            UpdateBarFromValue();
        }

        /// <summary>
        /// Sets the value range.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public void SetRange(int min, int max)
        {
            m_Min = min;
            m_Max = max;
            if (m_Value < m_Min)
                m_Value = m_Min;
            if (m_Value > m_Max)
                m_Value = m_Max;
        }

        /// <summary>
        /// Renders the focus overlay.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void RenderFocus(Skin.SkinBase skin)
        {
            if (InputHandler.KeyboardFocus != this) return;
            if (!IsTabable) return;

            skin.DrawKeyboardHighlight(this, RenderBounds, 0);
        }
    }    /// <summary>
         /// Horizontal slider.
         /// </summary>
    public class HorizontalIntSlider : IntSlider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalSlider"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public HorizontalIntSlider(ControlBase parent)
            : base(parent)
        {
            m_SliderBar.IsHorizontal = true;
        }

        protected override int CalculateValue()
        {
            int maxx = (Width - m_SliderBar.Width);
            int normrange = m_Max - m_Min;

            //  return m_SliderBar.X / (Width - m_SliderBar.Width);
           // return (int)(((Width - m_SliderBar.Width) / (double)m_SliderBar.X) * (m_Max - m_Min)) + m_Min;
            return (int)((m_SliderBar.X / (double)maxx) *  normrange) + m_Min;
        }

        protected override void UpdateBarFromValue()
        {
            int maxx = (Width - m_SliderBar.Width);
            int normval = m_Value - m_Min;
            double valpercent = normval / (double)(m_Max - m_Min);
            m_SliderBar.MoveTo((int)(valpercent * maxx), m_SliderBar.Y);
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
            var pos = CanvasPosToLocal(new Point(x, y));
            m_SliderBar.MoveTo(pos.X - (m_SliderBar.Width/2), m_SliderBar.Y);
            m_SliderBar.InputMouseClickedLeft(x, y, down);
            OnMoved(m_SliderBar, EventArgs.Empty);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            m_SliderBar.SetSize(15, Height);
            UpdateBarFromValue();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawSlider(this, true, 0, m_SliderBar.Width);
        }
    }
}
