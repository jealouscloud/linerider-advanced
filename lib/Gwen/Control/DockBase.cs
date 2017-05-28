using System;
using System.Drawing;
using Gwen.ControlInternal;
using Gwen.DragDrop;

namespace Gwen.Controls
{
    /// <summary>
    /// Base for dockable containers.
    /// </summary>
    public class DockBase : ControlBase
    {
        private DockBase m_Left;
        private DockBase m_Right;
        private DockBase m_Top;
        private DockBase m_Bottom;
        private Resizer m_Sizer;

        // Only CHILD dockpanels have a tabcontrol.
        private DockedTabControl m_DockedTabControl;

        private bool m_DrawHover;
        private bool m_DropFar;
        private Rectangle m_HoverRect;

        // todo: dock events?

        /// <summary>
        /// Control docked on the left side.
        /// </summary>
        public DockBase LeftDock { get { return GetChildDock(Pos.Left); } }

        /// <summary>
        /// Control docked on the right side.
        /// </summary>
        public DockBase RightDock { get { return GetChildDock(Pos.Right); } }

        /// <summary>
        /// Control docked on the top side.
        /// </summary>
        public DockBase TopDock { get { return GetChildDock(Pos.Top); } }

        /// <summary>
        /// Control docked on the bottom side.
        /// </summary>
        public DockBase BottomDock { get { return GetChildDock(Pos.Bottom); } }

        public TabControl TabControl { get { return m_DockedTabControl; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockBase"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public DockBase(ControlBase parent)
            : base(parent)
        {
            Padding = Padding.One;
            SetSize(200, 200);
        }

        /// <summary>
        /// Handler for Space keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeySpace(bool down)
        {
            // No action on space (default button action is to press)
            return false;
        }

        /// <summary>
        /// Initializes an inner docked control for the specified position.
        /// </summary>
        /// <param name="pos">Dock position.</param>
        protected virtual void SetupChildDock(Pos pos)
        {
            if (m_DockedTabControl == null)
            {
                m_DockedTabControl = new DockedTabControl(this);
                m_DockedTabControl.TabRemoved += OnTabRemoved;
                m_DockedTabControl.TabStripPosition = Pos.Bottom;
                m_DockedTabControl.TitleBarVisible = true;
            }

            Dock = pos;

            Pos sizeDir;
            if (pos == Pos.Right) sizeDir = Pos.Left;
            else if (pos == Pos.Left) sizeDir = Pos.Right;
            else if (pos == Pos.Top) sizeDir = Pos.Bottom;
            else if (pos == Pos.Bottom) sizeDir = Pos.Top;
            else throw new ArgumentException("Invalid dock", "pos");

            if (m_Sizer != null)
                m_Sizer.Dispose();
            m_Sizer = new Resizer(this);
            m_Sizer.Dock = sizeDir;
            m_Sizer.ResizeDir = sizeDir;
            m_Sizer.SetSize(2, 2);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {

        }

        /// <summary>
        /// Gets an inner docked control for the specified position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected virtual DockBase GetChildDock(Pos pos)
        {
            // todo: verify
            DockBase dock = null;
            switch (pos)
            {
                case Pos.Left:
                    if (m_Left == null)
                    {
                        m_Left = new DockBase(this);
                        m_Left.SetupChildDock(pos);
                    }
                    dock = m_Left;
                    break;

                case Pos.Right:
                    if (m_Right == null)
                    {
                        m_Right = new DockBase(this);
                        m_Right.SetupChildDock(pos);
                    }
                    dock = m_Right;
                    break;

                case Pos.Top:
                    if (m_Top == null)
                    {
                        m_Top = new DockBase(this);
                        m_Top.SetupChildDock(pos);
                    }
                    dock = m_Top;
                    break;

                case Pos.Bottom:
                    if (m_Bottom == null)
                    {
                        m_Bottom = new DockBase(this);
                        m_Bottom.SetupChildDock(pos);
                    }
                    dock = m_Bottom;
                    break;
            }

            if (dock != null)
                dock.IsHidden = false;

            return dock;
        }

        /// <summary>
        /// Calculates dock direction from dragdrop coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>Dock direction.</returns>
        protected virtual Pos GetDroppedTabDirection(int x, int y)
        {
            int w = Width;
            int h = Height;
            float top = y / (float)h;
            float left = x / (float)w;
            float right = (w - x) / (float)w;
            float bottom = (h - y) / (float)h;
            float minimum = Math.Min(Math.Min(Math.Min(top, left), right), bottom);

            m_DropFar = (minimum < 0.2f);

            if (minimum > 0.3f)
                return Pos.Fill;

            if (top == minimum && (null == m_Top || m_Top.IsHidden))
                return Pos.Top;
            if (left == minimum && (null == m_Left || m_Left.IsHidden))
                return Pos.Left;
            if (right == minimum && (null == m_Right || m_Right.IsHidden))
                return Pos.Right;
            if (bottom == minimum && (null == m_Bottom || m_Bottom.IsHidden))
                return Pos.Bottom;

            return Pos.Fill;
        }

        public override bool DragAndDrop_CanAcceptPackage(Package p)
        {
            // A TAB button dropped 
            if (p.Name == "TabButtonMove")
                return true;

            // a TAB window dropped
            if (p.Name == "TabWindowMove")
                return true;

            return false;
        }

        public override bool DragAndDrop_HandleDrop(Package p, int x, int y)
        {
            Point pos = CanvasPosToLocal(new Point(x, y));
            Pos dir = GetDroppedTabDirection(pos.X, pos.Y);

            DockedTabControl addTo = m_DockedTabControl;
            if (dir == Pos.Fill && addTo == null)
                return false;

            if (dir != Pos.Fill)
            {
                DockBase dock = GetChildDock(dir);
                addTo = dock.m_DockedTabControl;

                if (!m_DropFar)
                    dock.BringToFront();
                else
                    dock.SendToBack();
            }

            if (p.Name == "TabButtonMove")
            {
                TabButton tabButton = DragAndDrop.SourceControl as TabButton;
                if (null == tabButton)
                    return false;

                addTo.AddPage(tabButton);
            }

            if (p.Name == "TabWindowMove")
            {
                DockedTabControl tabControl = DragAndDrop.SourceControl as DockedTabControl;
                if (null == tabControl)
                    return false;
                if (tabControl == addTo)
                    return false;

                tabControl.MoveTabsTo(addTo);
            }

            Invalidate();

            return true;
        }

        /// <summary>
        /// Indicates whether the control contains any docked children.
        /// </summary>
        public virtual bool IsEmpty
        {
            get
            {
                if (m_DockedTabControl != null && m_DockedTabControl.TabCount > 0) return false;

                if (m_Left != null && !m_Left.IsEmpty) return false;
                if (m_Right != null && !m_Right.IsEmpty) return false;
                if (m_Top != null && !m_Top.IsEmpty) return false;
                if (m_Bottom != null && !m_Bottom.IsEmpty) return false;

                return true;
            }
        }

		protected virtual void OnTabRemoved(ControlBase control, EventArgs args)
        {
            DoRedundancyCheck();
            DoConsolidateCheck();
        }

        protected virtual void DoRedundancyCheck()
        {
            if (!IsEmpty) return;

            DockBase pDockParent = Parent as DockBase;
            if (null == pDockParent) return;

            pDockParent.OnRedundantChildDock(this);
        }

        protected virtual void DoConsolidateCheck()
        {
            if (IsEmpty) return;
            if (null == m_DockedTabControl) return;
            if (m_DockedTabControl.TabCount > 0) return;

            if (m_Bottom != null && !m_Bottom.IsEmpty)
            {
                m_Bottom.m_DockedTabControl.MoveTabsTo(m_DockedTabControl);
                return;
            }

            if (m_Top != null && !m_Top.IsEmpty)
            {
                m_Top.m_DockedTabControl.MoveTabsTo(m_DockedTabControl);
                return;
            }

            if (m_Left != null && !m_Left.IsEmpty)
            {
                m_Left.m_DockedTabControl.MoveTabsTo(m_DockedTabControl);
                return;
            }

            if (m_Right != null && !m_Right.IsEmpty)
            {
                m_Right.m_DockedTabControl.MoveTabsTo(m_DockedTabControl);
                return;
            }
        }

        protected virtual void OnRedundantChildDock(DockBase dock)
        {
            dock.IsHidden = true;
            DoRedundancyCheck();
            DoConsolidateCheck();
        }

        public override void DragAndDrop_HoverEnter(Package p, int x, int y)
        {
            m_DrawHover = true;
        }

        public override void DragAndDrop_HoverLeave(Package p)
        {
            m_DrawHover = false;
        }

        public override void DragAndDrop_Hover(Package p, int x, int y)
        {
            Point pos = CanvasPosToLocal(new Point(x, y));
            Pos dir = GetDroppedTabDirection(pos.X, pos.Y);

            if (dir == Pos.Fill)
            {
                if (null == m_DockedTabControl)
                {
                    m_HoverRect = Rectangle.Empty;
                    return;
                }

                m_HoverRect = InnerBounds;
                return;
            }

            m_HoverRect = RenderBounds;

            int HelpBarWidth = 0;

            if (dir == Pos.Left)
            {
                HelpBarWidth = (int)(m_HoverRect.Width * 0.25f);
                m_HoverRect.Width = HelpBarWidth;
            }

            if (dir == Pos.Right)
            {
                HelpBarWidth = (int)(m_HoverRect.Width * 0.25f);
                m_HoverRect.X = m_HoverRect.Width - HelpBarWidth;
                m_HoverRect.Width = HelpBarWidth;
            }

            if (dir == Pos.Top)
            {
                HelpBarWidth = (int)(m_HoverRect.Height * 0.25f);
                m_HoverRect.Height = HelpBarWidth;
            }

            if (dir == Pos.Bottom)
            {
                HelpBarWidth = (int)(m_HoverRect.Height * 0.25f);
                m_HoverRect.Y = m_HoverRect.Height - HelpBarWidth;
                m_HoverRect.Height = HelpBarWidth;
            }

            if ((dir == Pos.Top || dir == Pos.Bottom) && !m_DropFar)
            {
                if (m_Left != null && m_Left.IsVisible)
                {
                    m_HoverRect.X += m_Left.Width;
                    m_HoverRect.Width -= m_Left.Width;
                }

                if (m_Right != null && m_Right.IsVisible)
                {
                    m_HoverRect.Width -= m_Right.Width;
                }
            }

            if ((dir == Pos.Left || dir == Pos.Right) && !m_DropFar)
            {
                if (m_Top != null && m_Top.IsVisible)
                {
                    m_HoverRect.Y += m_Top.Height;
                    m_HoverRect.Height -= m_Top.Height;
                }

                if (m_Bottom != null && m_Bottom.IsVisible)
                {
                    m_HoverRect.Height -= m_Bottom.Height;
                }
            }
        }

        /// <summary>
        /// Renders over the actual control (overlays).
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void RenderOver(Skin.SkinBase skin)
        {
            if (!m_DrawHover)
                return;

            Renderer.RendererBase render = skin.Renderer;
            render.DrawColor = Color.FromArgb(20, 255, 200, 255);
            render.DrawFilledRect(RenderBounds);

            if (m_HoverRect.Width == 0)
                return;

            render.DrawColor = Color.FromArgb(100, 255, 200, 255);
            render.DrawFilledRect(m_HoverRect);

            render.DrawColor = Color.FromArgb(200, 255, 200, 255);
            render.DrawLinedRect(m_HoverRect);
        }
    }
}
