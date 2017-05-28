using System;

namespace Gwen.Controls
{
    /// <summary>
    /// Menu strip.
    /// </summary>
    public class MenuStrip : Menu
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuStrip"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public MenuStrip(ControlBase parent)
            : base(parent)
        {
            SetBounds(0, 0, 200, 22);
            Dock = Pos.Top;
            m_InnerPanel.Padding = new Padding(5, 0, 0, 0);
        }

        /// <summary>
        /// Closes the current menu.
        /// </summary>
        public override void Close()
        {
            
        }

        /// <summary>
        /// Renders under the actual control (shadows etc).
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void RenderUnder(Skin.SkinBase skin)
        {
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawMenuStrip(this);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            //TODO: We don't want to do vertical sizing the same as Menu, do nothing for now
        }

        /// <summary>
        /// Determines whether the menu should open on mouse hover.
        /// </summary>
        protected override bool ShouldHoverOpenMenu
        {
            get { return IsMenuOpen(); }
        }

        /// <summary>
        /// Add item handler.
        /// </summary>
        /// <param name="item">Item added.</param>
        protected override void OnAddItem(MenuItem item)
        {
            item.Dock = Pos.Left;
            item.TextPadding = new Padding(5, 0, 5, 0);
            item.Padding = new Padding(10, 0, 10, 0);
            item.SizeToContents();
            item.IsOnStrip = true;
            item.HoverEnter += OnHoverItem;
        }
    }
}
