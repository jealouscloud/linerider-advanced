using System;

namespace Gwen.Controls
{
    /// <summary>
    /// CheckBox control.
    /// </summary>
    public class CheckBox : Button
    {
        private bool m_Checked;

        /// <summary>
        /// Indicates whether the checkbox is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return m_Checked; } 
            set
            {
                if (m_Checked == value) return;
                m_Checked = value;
                OnCheckChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckBox"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public CheckBox(ControlBase parent)
            : base(parent)
        {
            SetSize(15, 15);
			IsToggle = true;
        }

        /// <summary>
        /// Toggles the checkbox.
        /// </summary>
        public override void Toggle()
        {
            base.Toggle();
            IsChecked = !IsChecked;
        }

        /// <summary>
        /// Invoked when the checkbox has been checked.
        /// </summary>
		public event GwenEventHandler<EventArgs> Checked;

        /// <summary>
        /// Invoked when the checkbox has been unchecked.
        /// </summary>
		public event GwenEventHandler<EventArgs> UnChecked;

        /// <summary>
        /// Invoked when the checkbox state has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> CheckChanged;

        /// <summary>
        /// Determines whether unchecking is allowed.
        /// </summary>
        protected virtual bool AllowUncheck { get { return true; } }

        /// <summary>
        /// Handler for CheckChanged event.
        /// </summary>
        protected virtual void OnCheckChanged()
        {
            if (IsChecked)
            { 
                if (Checked != null)
					Checked.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (UnChecked != null)
					UnChecked.Invoke(this, EventArgs.Empty);
            }

            if (CheckChanged != null)
				CheckChanged.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            base.Render(skin);
            skin.DrawCheckBox(this, m_Checked, IsDepressed);
        }

        /// <summary>
        /// Internal OnPressed implementation.
        /// </summary>
        protected override void OnClicked(int x, int y)
        {
            if (IsDisabled)
                return;
            
            if (IsChecked && !AllowUncheck)
            {
                return;
            }

			base.OnClicked(x, y);
        }
    }
}
