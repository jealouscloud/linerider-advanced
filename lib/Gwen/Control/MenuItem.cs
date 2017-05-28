using System;
using System.Drawing;
using Gwen.ControlInternal;

namespace Gwen.Controls
{
    /// <summary>
    /// Menu item.
    /// </summary>
    public class MenuItem : Button
    {
        private bool m_OnStrip;
        private bool m_Checkable;
        private bool m_Checked;
        private Menu m_Menu;
        private ControlBase m_SubmenuArrow;
        private Label m_Accelerator;

        /// <summary>
        /// Indicates whether the item is on a menu strip.
        /// </summary>
        public bool IsOnStrip { get { return m_OnStrip; } set { m_OnStrip = value; } }

        /// <summary>
        /// Determines if the menu item is checkable.
        /// </summary>
        public bool IsCheckable { get { return m_Checkable; } set { m_Checkable = value; } }

        /// <summary>
        /// Indicates if the parent menu is open.
        /// </summary>
        public bool IsMenuOpen { get { if (m_Menu == null) return false; return !m_Menu.IsHidden; } }

        /// <summary>
        /// Gets or sets the check value.
        /// </summary>
        public bool IsChecked
        {
            get { return m_Checked; }
            set
            {
                if (value == m_Checked)
                    return;

                m_Checked = value;

                if (CheckChanged != null)
                    CheckChanged.Invoke(this, EventArgs.Empty);

                if (value)
                {
                    if (Checked != null)
						Checked.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (UnChecked != null)
						UnChecked.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the parent menu.
        /// </summary>
        public Menu Menu
        {
            get
            {
                if (null == m_Menu)
                {
                    m_Menu = new Menu(GetCanvas());
                    m_Menu.IsHidden = true;

                    if (!m_OnStrip)
                    {
                        if (m_SubmenuArrow != null)
                            m_SubmenuArrow.Dispose();
                        m_SubmenuArrow = new RightArrow(this);
                        m_SubmenuArrow.SetSize(15, 15);
                    }

                    Invalidate();
                }

                return m_Menu;
            }
        }

        /// <summary>
        /// Invoked when the item is selected.
        /// </summary>
		public event GwenEventHandler<ItemSelectedEventArgs> Selected;

        /// <summary>
        /// Invoked when the item is checked.
        /// </summary>
		public event GwenEventHandler<EventArgs> Checked;

        /// <summary>
        /// Invoked when the item is unchecked.
        /// </summary>
		public event GwenEventHandler<EventArgs> UnChecked;

        /// <summary>
        /// Invoked when the item's check value is changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> CheckChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuItem"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public MenuItem(ControlBase parent)
            : base(parent)
        {
			AutoSizeToContents = true;
			m_OnStrip = false;
            IsTabable = false;
            IsCheckable = false;
            IsChecked = false;

            m_Accelerator = new Label(this);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawMenuItem(this, IsMenuOpen, m_Checkable ? m_Checked : false);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            if (m_SubmenuArrow != null)
            {
                m_SubmenuArrow.Position(Pos.Right | Pos.CenterV, 4, 0);
            }
            base.Layout(skin);
        }

        /// <summary>
        /// Internal OnPressed implementation.
        /// </summary>
        protected override void OnClicked(int x, int y)
        {
            if (m_Menu != null)
            {
                ToggleMenu();
            }
            else if (!m_OnStrip)
            {
                IsChecked = !IsChecked;
                if (Selected != null)
					Selected.Invoke(this, new ItemSelectedEventArgs(this));
                GetCanvas().CloseMenus();
            }
            base.OnClicked(x, y);
        }

        /// <summary>
        /// Toggles the menu open state.
        /// </summary>
        public void ToggleMenu()
        {
            if (IsMenuOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public void OpenMenu()
        {
            if (null == m_Menu) return;

            m_Menu.IsHidden = false;
            m_Menu.BringToFront();

            Point p = LocalPosToCanvas(Point.Empty);

            // Strip menus open downwards
            if (m_OnStrip)
            {
                m_Menu.SetPosition(p.X, p.Y + Height + 1);
            }
            // Submenus open sidewards
            else
            {
                m_Menu.SetPosition(p.X + Width, p.Y);
            }

            // TODO: Option this.
            // TODO: Make sure on screen, open the other side of the 
            // parent if it's better...
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void CloseMenu()
        {
            if (null == m_Menu) return;
            m_Menu.Close();
            m_Menu.CloseAll();
        }

        public override void SizeToContents()
        {
            base.SizeToContents();
            if (m_Accelerator != null)
            {
                m_Accelerator.SizeToContents();
                Width = Width + m_Accelerator.Width;
            }
        }

        public MenuItem SetAction(GwenEventHandler<EventArgs> handler)
        {
            if (m_Accelerator != null)
            {
                AddAccelerator(m_Accelerator.Text, handler);
            }

            Selected += handler;
            return this;
        }

        public void SetAccelerator(string acc)
        {
            if (m_Accelerator != null)
            {
                //m_Accelerator.DelayedDelete(); // to prevent double disposing
                m_Accelerator = null;
            }

            if (acc == String.Empty)
                return;

            m_Accelerator = new Label(this);
            m_Accelerator.Dock = Pos.Right;
            m_Accelerator.Alignment = Pos.Right | Pos.CenterV;
            m_Accelerator.Text = acc;
            m_Accelerator.Margin = new Margin(0, 0, 16, 0);
            // todo
        }
    }
}
