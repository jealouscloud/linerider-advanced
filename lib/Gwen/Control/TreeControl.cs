using System;

namespace Gwen.Controls
{
    /// <summary>
    /// Tree control.
    /// </summary>
    public class TreeControl : TreeNode
    {
        #region Properties

        /// <summary>
        /// Determines if multiple nodes can be selected at the same time.
        /// </summary>
        public bool AllowMultiSelect { get { return m_MultiSelect; } set { m_MultiSelect = value; } }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeControl"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public TreeControl(ControlBase parent)
            : base(parent)
        {
            m_TreeControl = this;

            RemoveChild(m_ToggleButton, true);
            m_ToggleButton = null;
            RemoveChild(m_Title, true);
            m_Title = null;
            RemoveChild(m_InnerPanel, true);
            m_InnerPanel = null;

            m_MultiSelect = false;

            m_ScrollControl = new ScrollControl(this);
            m_ScrollControl.Dock = Pos.Fill;
            m_ScrollControl.EnableScroll(false, true);
            m_ScrollControl.AutoHideBars = true;
            m_ScrollControl.Margin = Margin.One;

            m_InnerPanel = m_ScrollControl;

            m_ScrollControl.SetInnerSize(1000, 1000); // todo: why such arbitrary numbers?

            Dock = Pos.None;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Handler for node added event.
        /// </summary>
        /// <param name="node">Node added.</param>
        public virtual void OnNodeAdded(TreeNode node)
        {
            node.LabelPressed += OnNodeSelected;
        }

        /// <summary>
        /// Removes all child nodes.
        /// </summary>
        public virtual void RemoveAll()
        {
            m_ScrollControl.DeleteAll();
        }

        /// <summary>
        /// Handler invoked when control children's bounds change.
        /// </summary>
        /// <param name="oldChildBounds"></param>
        /// <param name="child"></param>
        protected override void OnChildBoundsChanged(System.Drawing.Rectangle oldChildBounds, ControlBase child)
        {
            if (m_ScrollControl != null)
                m_ScrollControl.UpdateScrollBars();
        }

        /// <summary>
        /// Handler for node selected event.
        /// </summary>
        /// <param name="Control">Node selected.</param>
        protected virtual void OnNodeSelected(ControlBase Control, EventArgs args)
        {
            if (!m_MultiSelect /*|| InputHandler.InputHandler.IsKeyDown(Key.Control)*/)
                UnselectAll();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            if (ShouldDrawBackground)
                skin.DrawTreeControl(this);
        }

        #endregion Methods

        #region Fields

        private readonly ScrollControl m_ScrollControl;
        private bool m_MultiSelect;

        #endregion Fields
    }
}