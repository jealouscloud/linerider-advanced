using System;
using System.Drawing;
using Gwen.ControlInternal;
using Gwen.DragDrop;

namespace Gwen.Controls
{
    /// <summary>
    /// Tab strip - groups TabButtons and allows reordering.
    /// </summary>
    public class TabStrip : ControlBase
    {
        private ControlBase m_TabDragControl;
        private bool m_AllowReorder;

        /// <summary>
        /// Determines whether it is possible to reorder tabs by mouse dragging.
        /// </summary>
        public bool AllowReorder { get { return m_AllowReorder; } set { m_AllowReorder = value; } }

        /// <summary>
        /// Determines whether the control should be clipped to its bounds while rendering.
        /// </summary>
        protected override bool ShouldClip
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabStrip"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public TabStrip(ControlBase parent)
            : base(parent)
        {
            m_AllowReorder = false;
        }

        /// <summary>
        /// Strip position (top/left/right/bottom).
        /// </summary>
        public Pos StripPosition
        {
            get { return Dock; }
            set
            {
                Dock = value;
                if (Dock == Pos.Top)
                    Padding = new Padding(5, 0, 0, 0);
                if (Dock == Pos.Left)
                    Padding = new Padding(0, 5, 0, 0);
                if (Dock == Pos.Bottom)
                    Padding = new Padding(5, 0, 0, 0);
                if (Dock == Pos.Right)
                    Padding = new Padding(0, 5, 0, 0);
            }
        }

        public override bool DragAndDrop_HandleDrop(Package p, int x, int y)
        {
            Point LocalPos = CanvasPosToLocal(new Point(x, y));

            TabButton button = DragAndDrop.SourceControl as TabButton;
            TabControl tabControl = Parent as TabControl;
            if (tabControl != null && button != null)
            {
                if (button.TabControl != tabControl)
                {
                    // We've moved tab controls!
                    tabControl.AddPage(button);
                }
            }

            ControlBase droppedOn = GetControlAt(LocalPos.X, LocalPos.Y);
            if (droppedOn != null)
            {
                Point dropPos = droppedOn.CanvasPosToLocal(new Point(x, y));
                DragAndDrop.SourceControl.BringNextToControl(droppedOn, dropPos.X > droppedOn.Width/2);
            }
            else
            {
                DragAndDrop.SourceControl.BringToFront();
            }
            return true;
        }

        public override bool DragAndDrop_CanAcceptPackage(Package p)
        {
            if (!m_AllowReorder)
                return false;

            if (p.Name == "TabButtonMove")
                return true;

            return false;
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            Point largestTab = new Point(5, 5);

            int num = 0;
            foreach (var child in Children)
            {
                TabButton button = child as TabButton;
                if (null == button) continue;

                button.SizeToContents();

                Margin m = new Margin();
                int notFirst = num > 0 ? -1 : 0;

                if (Dock == Pos.Top)
                {
                    m.Left = notFirst;
                    button.Dock = Pos.Left;
                }

                if (Dock == Pos.Left)
                {
                    m.Top = notFirst;
                    button.Dock = Pos.Top;
                }

                if (Dock == Pos.Right)
                {
                    m.Top = notFirst;
                    button.Dock = Pos.Top;
                }

                if (Dock == Pos.Bottom)
                {
                    m.Left = notFirst;
                    button.Dock = Pos.Left;
                }

                largestTab.X = Math.Max(largestTab.X, button.Width);
                largestTab.Y = Math.Max(largestTab.Y, button.Height);

                button.Margin = m;
                num++;
            }

            if (Dock == Pos.Top || Dock == Pos.Bottom)
                SetSize(Width, largestTab.Y);

            if (Dock == Pos.Left || Dock == Pos.Right)
                SetSize(largestTab.X, Height);

            base.Layout(skin);
        }

        public override void DragAndDrop_HoverEnter(Package p, int x, int y)
        {
            if (m_TabDragControl != null)
            {
                throw new InvalidOperationException("ERROR! TabStrip::DragAndDrop_HoverEnter");
            }

            m_TabDragControl = new Highlight(this);
            m_TabDragControl.MouseInputEnabled = false;
            m_TabDragControl.SetSize(3, Height);
        }

        public override void DragAndDrop_HoverLeave(Package p)
        {
            if (m_TabDragControl != null)
            {
                RemoveChild(m_TabDragControl, false); // [omeg] need to do that explicitely
                m_TabDragControl.Dispose();
            }
            m_TabDragControl = null;
        }

        public override void DragAndDrop_Hover(Package p, int x, int y)
        {
            Point localPos = CanvasPosToLocal(new Point(x, y));

            ControlBase droppedOn = GetControlAt(localPos.X, localPos.Y);
            if (droppedOn != null && droppedOn != this)
            {
                Point dropPos = droppedOn.CanvasPosToLocal(new Point(x, y));
                m_TabDragControl.SetBounds(new Rectangle(0, 0, 3, Height));
                m_TabDragControl.BringToFront();
                m_TabDragControl.SetPosition(droppedOn.X - 1, 0);

                if (dropPos.X > droppedOn.Width/2)
                {
                    m_TabDragControl.MoveBy(droppedOn.Width - 1, 0);
                }
                m_TabDragControl.Dock = Pos.None;
            }
            else
            {
                m_TabDragControl.Dock = Pos.Left;
                m_TabDragControl.BringToFront();
            }
        }
    }
}
