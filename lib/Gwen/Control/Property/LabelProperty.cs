using System;
using Gwen.Skin;

namespace Gwen.Controls.Property
{
    /// <summary>
    /// Label property.
    /// </summary>
    public class LabelProperty : PropertyBase
    {
        protected readonly Gwen.Controls.Label m_text;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabelProperty"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public LabelProperty(Gwen.Controls.ControlBase parent) : base(parent)
        {
            m_text = new Gwen.Controls.Label(this);
            var marg = m_text.Margin;
            marg.Left = 2;
            m_text.Margin = marg;
            m_text.Dock = Pos.Fill;
            m_text.ShouldDrawBackground = false;
        }
        
        /// <summary>
        /// Property value.
        /// </summary>
        public override string Value
        {
            get { return m_text.Text; }
            set { base.Value = value;
                m_text.SetText(value); }
        }
        protected override void Render(SkinBase skin)
        {
            skin.Renderer.DrawColor = System.Drawing.Color.LightGray;
            var r = this.RenderBounds;
            r.X += 1;
            r.Y += 1;
            r.Width -= 2;
            r.Height -= 2;
            skin.Renderer.DrawFilledRect(r);
            base.Render(skin);
        }
        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="fireEvents">Determines whether to fire "value changed" event.</param>
        public override void SetValue(string value, bool fireEvents = false)
        {
            m_text.SetText(value, fireEvents);
        }

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public override bool IsEditing
        {
            get { return m_text.HasFocus; }
        }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get { return base.IsHovered | m_text.IsHovered; }
        }
    }
}
