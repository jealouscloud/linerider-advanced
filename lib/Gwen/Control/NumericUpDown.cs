using Gwen.ControlInternal;
using Gwen.Controls.Layout;
using System;

namespace Gwen.Controls
{
    /// <summary>
    /// Numeric up/down.
    /// </summary>
    public class NumericUpDown : TextBoxNumeric
    {
        #region Fields

        public bool UserEdit;

        #endregion Fields

        #region Events

        /// <summary>
        /// Invoked when the value has been changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> ValueChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Maximum value.
        /// </summary>
        public int Max { get { return m_Max; } set { m_Max = value; } }

        /// <summary>
        /// Minimum value.
        /// </summary>
        public int Min { get { return m_Min; } set { m_Min = value; } }

        /// <summary>
        /// Numeric value of the control.
        /// </summary>
        public override float Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                if (value < m_Min) value = m_Min;
                if (value > m_Max) value = m_Max;
                if (value != m_Value)
                {
                    base.Value = value;
                    if (ValueChanged != null)
                        ValueChanged.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericUpDown"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public NumericUpDown(ControlBase parent)
            : base(parent)
        {
            SetSize(100, 20);

            m_Splitter = new Splitter(this);
            m_Splitter.Dock = Pos.Right;
            m_Splitter.SetSize(13, 13);

            m_Up = new UpDownButton_Up(m_Splitter);
            m_Up.Clicked += OnButtonUp;
            m_Up.IsTabable = false;
            m_Splitter.SetPanel(0, m_Up, false);

            m_Down = new UpDownButton_Down(m_Splitter);
            m_Down.Clicked += OnButtonDown;
            m_Down.IsTabable = false;
            m_Down.Padding = new Padding(0, 1, 1, 0);
            m_Splitter.SetPanel(1, m_Down, false);

            m_Max = 100;
            m_Min = 0;
            m_Value = 0f;
            Text = "0";
        }

        #endregion Constructors

        #region Methods

        public override void SetValue(float v)
        {
            if (v < m_Min)
                v = m_Min;
            if (v > m_Max)
                v = m_Max;
            if (v != base.Value)
            {
                base.SetValue(v);
                if (ValueChanged != null)
                    ValueChanged.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handler for the button down event.
        /// </summary>
        /// <param name="control">Event source.</param>
        protected virtual void OnButtonDown(ControlBase control, ClickedEventArgs args)
        {
            Value = m_Value - 1;
        }

        /// <summary>
        /// Handler for the button up event.
        /// </summary>
        /// <param name="control">Event source.</param>
        protected virtual void OnButtonUp(ControlBase control, EventArgs args)
        {
            Value = m_Value + 1;
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
            if (down) OnButtonDown(null, new ClickedEventArgs(0, 0, true));
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
            if (down) OnButtonUp(null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Handler for the text changed event.
        /// </summary>
        protected override void OnTextChanged()
        {
            base.OnTextChanged();
        }

        protected override void UpdateAfterTextChanged()
        {
            base.UpdateAfterTextChanged();
        }

        #endregion Methods

        private readonly UpDownButton_Down m_Down;
        private readonly Splitter m_Splitter;
        private readonly UpDownButton_Up m_Up;
        private int m_Max;
        private int m_Min;
    }
}