using System;
using System.Collections.Generic;

namespace Gwen.Controls.Property
{
    /// <summary>
    /// Text property.
    /// </summary>
    public class KeyProperty : PropertyBase
    {
        private Label keytxt;
        /// <summary>
        /// Initializes a new instance of the <see cref="Text"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public KeyProperty(Gwen.Controls.ControlBase parent) : base(parent)
        {
            keytxt = new Label(this);
            keytxt.KeyboardInputEnabled = false;
            keytxt.MouseInputEnabled = false;
            keytxt.Dock = Pos.Fill;
            keytxt.ShouldDrawBackground = false;
            KeyboardInputEnabled = true;
            MouseInputEnabled = true;
        }
        protected override bool OnKeyPressed(Key key, bool down = true)
        {
            keytxt.Text = key.ToString();
            OnValueChanged(this, EventArgs.Empty);
            return base.OnKeyPressed(key, down);
        }
        protected override bool OnChar(char chr)
        {
            keytxt.Text = Char.ToUpper(chr).ToString();
            OnValueChanged(this, EventArgs.Empty);
            return base.OnChar(chr);
        }
        /// <summary>
        /// Property value.
        /// </summary>
        public override string Value
        {
            get { return keytxt.Text; }
            set { base.Value = value; }
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="fireEvents">Determines whether to fire "value changed" event.</param>
        public override void SetValue(string value, bool fireEvents = false)
        {
            keytxt.Text = value;

        }
        public void SetValue(string val, string defaultval = null)
        {
            Key parseme;
            if (val?.Length == 1 && char.IsLetterOrDigit(val[0]))
                Value = val;
            else if (Enum.TryParse(val, out parseme))
            {
                Value = parseme.ToString();
            }
            else
                Value = defaultval;
            OnValueChanged(this, EventArgs.Empty);
        }
        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public override bool IsEditing
        {
            get { return this.HasFocus || keytxt.HasFocus; }
        }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get { return base.IsHovered | keytxt.IsHovered; }
        }
    }
}
