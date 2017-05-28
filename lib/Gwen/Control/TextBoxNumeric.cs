using System;

namespace Gwen.Controls
{
    /// <summary>
    /// Numeric text box - accepts only float numbers.
    /// </summary>
    public class TextBoxNumeric : TextBox
    {
        #region Properties

        /// <summary>
        /// Current numerical value.
        /// </summary>
        public virtual float Value
        {
            get { return m_Value; }
            set
            {
                if (value != m_Value)
                {
                    m_Value = value;
                    Text = value.ToString();
                    Invalidate();
                }
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBoxNumeric"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public TextBoxNumeric(ControlBase parent) : base(parent)
        {
            AutoSizeToContents = false;
            SetText("0", false);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Sets the control text.
        /// </summary>
        /// <param name="str">Text to set.</param>
        /// <param name="doEvents">Determines whether to invoke "text changed" event.</param>
        public override void SetText(string str, bool doEvents = true)
        {
            if (IsTextAllowed(str))
                base.SetText(str, doEvents);
        }

        public virtual void SetValue(float v)
        {
            m_Value = v;
        }

        #endregion Methods

        #region Fields

        /// <summary>
        /// Current numeric value.
        /// </summary>
        protected float m_Value;

        #endregion Fields

        protected virtual bool IsTextAllowed(string str)
        {
            if (str == "" || str == "-")
                return true; // annoying if single - is not allowed
            float d;
            return float.TryParse(str, out d);
        }

        /// <summary>
        /// Determines whether the control can insert text at a given cursor position.
        /// </summary>
        /// <param name="text">Text to check.</param>
        /// <param name="position">Cursor position.</param>
        /// <returns>True if allowed.</returns>
        protected override bool IsTextAllowed(string text, int position)
        {
            string newText = Text.Insert(position, text);
            return IsTextAllowed(newText);
        }

        protected override bool OnKeyReturn(bool down)
        {
            if (down)
            {
                UpdateValue();
                Text = Value.ToString();
            }
            return true;
        }

        protected override void OnLostKeyboardFocus()
        {
            UpdateValue();
            base.OnLostKeyboardFocus();
        }

        // text -> value
        /// <summary>
        /// Handler for text changed event.
        /// </summary>
        protected override void OnTextChanged()
        {
            base.OnTextChanged();
            UpdateAfterTextChanged();
        }

        protected virtual void UpdateAfterTextChanged()
        {
            UpdateValue();
        }

        protected virtual void UpdateValue()
        {
            if (Validate(Text))
            {
                SetValue(float.Parse(Text));
            }
            else if (String.IsNullOrEmpty(Text) || Text == "-")
            {
                SetValue(0);
            }
        }

        /// <summary>
        /// Determines whether the text can be assighed to the control.
        /// </summary>
        /// <param name="str">Text to evaluate.</param>
        /// <returns>True if the text is allowed.</returns>
        protected virtual bool Validate(string str)
        {
            float d;
            if (!float.TryParse(str, out d))
                return false;
            return true;
        }
    }
}