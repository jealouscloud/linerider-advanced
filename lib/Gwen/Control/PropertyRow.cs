using Gwen.ControlInternal;
using System;

namespace Gwen.Controls
{
    /// <summary>
    /// Single property row.
    /// </summary>
    public class PropertyRow : ControlBase
    {
        #region Events

        /// <summary>
        /// Invoked when the property value has changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> ValueChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public bool IsEditing { get { return m_Property != null && m_Property.IsEditing; } }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get
            {
                return base.IsHovered || (m_Property != null && m_Property.IsHovered);
            }
        }

        /// <summary>
        /// Property name.
        /// </summary>
        public string Label { get { return m_Label.Text; } set { m_Label.Text = value; } }

        public System.Drawing.Color LabelColor
        {
            get
            {
                return m_Label.TextColorOverride;
            }
            set
            {
                m_Label.TextColorOverride = value;
            }
        }

        /// <summary>
        /// Property value.
        /// </summary>
        public string Value { get { return m_Property.Value; } set { m_Property.Value = value; } }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        /// <param name="prop">Property control associated with this row.</param>
        public PropertyRow(ControlBase parent, Property.PropertyBase prop)
            : base(parent)
        {
            PropertyRowLabel label = new PropertyRowLabel(this);
            label.Dock = Pos.Left;
            label.Alignment = Pos.Left | Pos.Top;
            label.Margin = new Margin(2, 2, 0, 0);
            m_Label = label;

            m_Property = prop;
            m_Property.Parent = this;
            m_Property.Dock = Pos.Fill;
            m_Property.ValueChanged += OnValueChanged;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            Properties parent = Parent as Properties;
            if (null == parent) return;

            m_Label.Width = parent.SplitWidth;

            if (m_Property != null)
            {
                Height = m_Property.Height;
            }
        }

        protected virtual void OnValueChanged(ControlBase control, EventArgs args)
        {
            if (ValueChanged != null)
                ValueChanged.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            /* SORRY */
            if (IsEditing != m_LastEditing)
            {
                OnEditingChanged();
                m_LastEditing = IsEditing;
            }

            if (IsHovered != m_LastHover)
            {
                OnHoverChanged();
                m_LastHover = IsHovered;
            }
            /* SORRY */

            skin.DrawPropertyRow(this, m_Label.Right, IsEditing, IsHovered | m_Property.IsHovered);
        }

        #endregion Methods

        #region Fields

        private readonly Label m_Label;
        private readonly Property.PropertyBase m_Property;
        private bool m_LastEditing;
        private bool m_LastHover;

        #endregion Fields

        private void OnEditingChanged()
        {
            m_Label.Redraw();
        }

        private void OnHoverChanged()
        {
            m_Label.Redraw();
            if (!IsHovered)
            {
                ToolTip.Disable(this);
            }
        }
        public override void SetToolTipText(string text)
        {
              base.SetToolTipText(text);
            m_Label.SetToolTipText(text);
        }
    }
}