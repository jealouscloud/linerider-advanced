using System;
using Gwen.Input;

namespace Gwen.Controls
{
    /// <summary>
    /// RadioButton with label.
    /// </summary>
    public class LabeledRadioButton : ControlBase
    {
        private readonly RadioButton m_RadioButton;
        private readonly Label m_Label;

        /// <summary>
        /// Label text.
        /// </summary>
        public string Text { get { return m_Label.Text; } set { m_Label.Text = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledRadioButton"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public LabeledRadioButton(ControlBase parent)
            : base(parent)
        {
			MouseInputEnabled = true;
            SetSize(100, 20);

            m_RadioButton = new RadioButton(this);
            //m_RadioButton.Dock = Pos.Left; // no docking, it causes resizing
            //m_RadioButton.Margin = new Margin(0, 2, 2, 2);
            m_RadioButton.IsTabable = false;
            m_RadioButton.KeyboardInputEnabled = false;

            m_Label = new Label(this);
            m_Label.Alignment = Pos.CenterV | Pos.Left;
            m_Label.Text = "Radio Button";
			m_Label.Clicked += delegate(ControlBase control, ClickedEventArgs args) { m_RadioButton.Press(control); };
            m_Label.IsTabable = false;
            m_Label.KeyboardInputEnabled = false;
        }

        protected override void Layout(Skin.SkinBase skin)
        {
            // ugly stuff because we don't have anchoring without docking (docking resizes children)
            if (m_Label.Height > m_RadioButton.Height) // usually radio is smaller than label so it gets repositioned to avoid clipping with negative Y
            {
                m_RadioButton.Y = (m_Label.Height - m_RadioButton.Height)/2;
            }
            Align.PlaceRightBottom(m_Label, m_RadioButton);
            SizeToChildren();
            base.Layout(skin);
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

        // todo: would be nice to remove that
        internal RadioButton RadioButton { get { return m_RadioButton; } }

        /// <summary>
        /// Handler for Space keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeySpace(bool down)
        {
            if (down)
                m_RadioButton.IsChecked = !m_RadioButton.IsChecked;
            return true;
        }

        /// <summary>
        /// Selects the radio button.
        /// </summary>
        public virtual void Select()
        {
            m_RadioButton.IsChecked = true;
        }
    }
}
