using Gwen.Anim;
using Gwen.DragDrop;
using Gwen.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gwen.Controls
{
    /// <summary>
    /// Base control class.
    /// </summary>
    public class ControlBase : IDisposable
    {
        /// <summary>
        /// Delegate used for all control event handlers.
        /// </summary>
        /// <param name="control">Event source.</param>
        /// <param name="args" >Additional arguments. May be empty (EventArgs.Empty).</param>
		public delegate void GwenEventHandler<in T>(ControlBase sender, T arguments) where T : System.EventArgs;

        private bool m_Disposed;

        private ControlBase m_Parent;

        /// <summary>
        /// This is the panel's actual parent - most likely the logical
        /// parent's InnerPanel (if it has one). You should rarely need this.
        /// </summary>
        private ControlBase m_ActualParent;

        /// <summary>
        /// If the innerpanel exists our children will automatically become children of that
        /// instead of us - allowing us to move them all around by moving that panel (useful for scrolling etc).
        /// </summary>
        protected ControlBase m_InnerPanel;

        private Skin.SkinBase m_Skin;

        private Rectangle m_Bounds;
        private Rectangle m_RenderBounds;
        private Rectangle m_InnerBounds;
        private Padding m_Padding;
        private Margin m_Margin;

        private string m_Name;

        private bool m_RestrictToParent;
        private bool m_Disabled;
        private bool m_Hidden;
        private bool m_MouseInputEnabled;
        private bool m_KeyboardInputEnabled;
        private bool m_DrawBackground;

        private Pos m_Dock;

        private Cursor m_Cursor;

        private bool m_Tabable;

        private bool m_NeedsLayout;
        private bool m_CacheTextureDirty;
        private bool m_CacheToTexture;

        private Package m_DragAndDrop_Package;

        private object m_UserData;

        private bool m_DrawDebugOutlines;

        /// <summary>
        /// Real list of children.
        /// </summary>
        private readonly List<ControlBase> m_Children;

        /// <summary>
        /// Invoked when mouse pointer enters the control.
        /// </summary>
		public event GwenEventHandler<EventArgs> HoverEnter;

        /// <summary>
        /// Invoked when mouse pointer leaves the control.
        /// </summary>
		public event GwenEventHandler<EventArgs> HoverLeave;

        /// <summary>
        /// Invoked when control's bounds have been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> BoundsChanged;

        /// <summary>
        /// Invoked when the control has been left-clicked.
        /// </summary>
        public virtual event GwenEventHandler<ClickedEventArgs> Clicked;

        /// <summary>
        /// Invoked when the control has been double-left-clicked.
        /// </summary>
        public virtual event GwenEventHandler<ClickedEventArgs> DoubleClicked;

        /// <summary>
        /// Invoked when the control has been right-clicked.
        /// </summary>
        public virtual event GwenEventHandler<ClickedEventArgs> RightClicked;

        /// <summary>
        /// Invoked when the control has been double-right-clicked.
        /// </summary>
        public virtual event GwenEventHandler<ClickedEventArgs> DoubleRightClicked;

        /// <summary>
        /// Returns true if any on click events are set.
        /// </summary>
        internal bool ClickEventAssigned
        {
            get
            {
                return Clicked != null || RightClicked != null || DoubleClicked != null || DoubleRightClicked != null;
            }
        }

        /// <summary>
        /// Accelerator map.
        /// </summary>
        private readonly Dictionary<string, GwenEventHandler<EventArgs>> m_Accelerators;

        public const int MaxCoord = 4096; // added here from various places in code

        /// <summary>
        /// Logical list of children. If InnerPanel is not null, returns InnerPanel's children.
        /// </summary>
        public List<ControlBase> Children
        {
            get
            {
                if (m_InnerPanel != null)
                    return m_InnerPanel.Children;
                return m_Children;
            }
        }

        /// <summary>
        /// The logical parent. It's usually what you expect, the control you've parented it to.
        /// </summary>
        public ControlBase Parent
        {
            get { return m_Parent; }
            set
            {
                if (m_Parent == value)
                    return;

                if (m_Parent != null)
                {
                    m_Parent.RemoveChild(this, false);
                }

                m_Parent = value;
                m_ActualParent = null;

                if (m_Parent != null)
                {
                    m_Parent.AddChild(this);
                }
            }
        }

        // todo: ParentChanged event?

        /// <summary>
        /// Dock position.
        /// </summary>
        public Pos Dock
        {
            get { return m_Dock; }
            set
            {
                if (m_Dock == value)
                    return;

                m_Dock = value;

                Invalidate();
                InvalidateParent();
            }
        }

        /// <summary>
        /// Current skin.
        /// </summary>
        public Skin.SkinBase Skin
        {
            get
            {
                if (m_Skin != null)
                    return m_Skin;
                if (m_Parent != null)
                    return m_Parent.Skin;

                throw new InvalidOperationException("GetSkin: null");
            }
        }

        /// <summary>
        /// Indicates whether this control is a menu component.
        /// </summary>
        internal virtual bool IsMenuComponent
        {
            get
            {
                if (m_Parent == null)
                    return false;
                return m_Parent.IsMenuComponent;
            }
        }

        /// <summary>
        /// Determines whether the control should be clipped to its bounds while rendering.
        /// </summary>
        protected virtual bool ShouldClip { get { return true; } }

        /// <summary>
        /// Current padding - inner spacing.
        /// </summary>
        public Padding Padding
        {
            get { return m_Padding; }
            set
            {
                if (m_Padding == value)
                    return;

                m_Padding = value;
                Invalidate();
                InvalidateParent();
            }
        }

        /// <summary>
        /// Current margin - outer spacing.
        /// </summary>
        public Margin Margin
        {
            get { return m_Margin; }
            set
            {
                if (m_Margin == value)
                    return;

                m_Margin = value;
                Invalidate();
                InvalidateParent();
            }
        }

        private string _tooltip = null;

        public virtual string Tooltip
        {
            get
            {
                return _tooltip;
            }
            set
            {
                _tooltip = value;
            }
        }

        /// <summary>
        /// Indicates whether the control is on top of its parent's children.
        /// </summary>
        public virtual bool IsOnTop { get { return this == Parent.m_Children.First(); } } // todo: validate

        /// <summary>
        /// User data associated with the control.
        /// </summary>
        public object UserData { get { return m_UserData; } set { m_UserData = value; } }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public virtual bool IsHovered { get { return InputHandler.HoveredControl == this; } }

        /// <summary>
        /// Indicates whether the control has focus.
        /// </summary>
        public bool HasFocus { get { return InputHandler.KeyboardFocus == this; } }

        /// <summary>
        /// Indicates whether the control is disabled.
        /// </summary>
        public bool IsDisabled { get { return m_Disabled; } set { m_Disabled = value; } }

        /// <summary>
        /// Indicates whether the control is hidden.
        /// </summary>
        public virtual bool IsHidden { get { return m_Hidden; } set { if (value == m_Hidden) return; m_Hidden = value; Invalidate(); } }

        /// <summary>
        /// Determines whether the control's position should be restricted to parent's bounds.
        /// </summary>
        public bool RestrictToParent { get { return m_RestrictToParent; } set { m_RestrictToParent = value; } }

        /// <summary>
        /// Determines whether the control receives mouse input events.
        /// </summary>
        public bool MouseInputEnabled { get { return m_MouseInputEnabled; } set { m_MouseInputEnabled = value; } }

        /// <summary>
        /// Determines whether the control receives keyboard input events.
        /// </summary>
        public bool KeyboardInputEnabled { get { return m_KeyboardInputEnabled; } set { m_KeyboardInputEnabled = value; } }

        /// <summary>
        /// Gets or sets the mouse cursor when the cursor is hovering the control.
        /// </summary>
        public Cursor Cursor { get { return m_Cursor; } set { m_Cursor = value; } }

        /// <summary>
        /// Indicates whether the control is tabable (can be focused by pressing Tab).
        /// </summary>
        public bool IsTabable { get { return m_Tabable; } set { m_Tabable = value; } }

        /// <summary>
        /// Indicates whether control's background should be drawn during rendering.
        /// </summary>
        public bool ShouldDrawBackground { get { return m_DrawBackground; } set { m_DrawBackground = value; } }

        /// <summary>
        /// Indicates whether the renderer should cache drawing to a texture to improve performance (at the cost of memory).
        /// </summary>
        public bool ShouldCacheToTexture { get { return m_CacheToTexture; } set { m_CacheToTexture = value; /*Children.ForEach(x => x.ShouldCacheToTexture=value);*/ } }

        /// <summary>
        /// Gets or sets the control's internal name.
        /// </summary>
        public string Name { get { return m_Name; } set { m_Name = value; } }

        /// <summary>
        /// Control's size and position relative to the parent.
        /// </summary>
        public Rectangle Bounds { get { return m_Bounds; } }

        /// <summary>
        /// Bounds for the renderer.
        /// </summary>
        public Rectangle RenderBounds { get { return m_RenderBounds; } }

        /// <summary>
        /// Bounds adjusted by padding.
        /// </summary>
        public Rectangle InnerBounds { get { return m_InnerBounds; } }

        /// <summary>
        /// Size restriction.
        /// </summary>
        public Point MinimumSize { get { return m_MinimumSize; } set { m_MinimumSize = value; } }

        /// <summary>
        /// Size restriction.
        /// </summary>
        public Point MaximumSize { get { return m_MaximumSize; } set { m_MaximumSize = value; } }

        private Point m_MinimumSize = new Point(1, 1);
        private Point m_MaximumSize = new Point(MaxCoord, MaxCoord);

        /// <summary>
        /// Determines whether hover should be drawn during rendering.
        /// </summary>
        protected bool ShouldDrawHover { get { return InputHandler.MouseFocus == this || InputHandler.MouseFocus == null; } }

        protected virtual bool AccelOnlyFocus { get { return false; } }
        protected virtual bool NeedsInputChars { get { return false; } }

        /// <summary>
        /// Indicates whether the control and its parents are visible.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                if (IsHidden)
                    return false;

                if (Parent != null)
                    return Parent.IsVisible;

                return true;
            }
        }

        /// <summary>
        /// Leftmost coordinate of the control.
        /// </summary>
        public int X { get { return m_Bounds.X; } set { SetPosition(value, Y); } }

        /// <summary>
        /// Topmost coordinate of the control.
        /// </summary>
        public int Y { get { return m_Bounds.Y; } set { SetPosition(X, value); } }

        // todo: Bottom/Right includes margin but X/Y not?

        public int Width { get { return m_Bounds.Width; } set { SetSize(value, Height); } }
        public int Height { get { return m_Bounds.Height; } set { SetSize(Width, value); } }
        public int Bottom { get { return m_Bounds.Bottom + m_Margin.Bottom; } }
        public int Right { get { return m_Bounds.Right + m_Margin.Right; } }

        /// <summary>
        /// Determines whether margin, padding and bounds outlines for the control will be drawn. Applied recursively to all children.
        /// </summary>
        public bool DrawDebugOutlines
        {
            get { return m_DrawDebugOutlines; }
            set
            {
                if (m_DrawDebugOutlines == value)
                    return;
                m_DrawDebugOutlines = value;
                foreach (ControlBase child in Children)
                {
                    child.DrawDebugOutlines = value;
                }
            }
        }

        public Color PaddingOutlineColor { get; set; }
        public Color MarginOutlineColor { get; set; }
        public Color BoundsOutlineColor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlBase"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public ControlBase(ControlBase parent = null)
        {
            m_Children = new List<ControlBase>();
            m_Accelerators = new Dictionary<string, GwenEventHandler<EventArgs>>();

            Parent = parent;

            m_Hidden = false;
            m_Bounds = new Rectangle(0, 0, 10, 10);
            m_Padding = Padding.Zero;
            m_Margin = Margin.Zero;

            RestrictToParent = false;

            MouseInputEnabled = true;
            KeyboardInputEnabled = false;

            Invalidate();
            Cursor = Cursors.Default;
            //ToolTip = null;
            IsTabable = false;
            ShouldDrawBackground = true;
            m_Disabled = false;
            m_CacheTextureDirty = true;
            m_CacheToTexture = false;

            BoundsOutlineColor = Color.Red;
            MarginOutlineColor = Color.Green;
            PaddingOutlineColor = Color.Blue;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            //Debug.Print("Control.Base: Disposing {0} {1:X}", this, GetHashCode());
            if (m_Disposed)
            {
#if DEBUG
                throw new ObjectDisposedException(String.Format("Control.Base [{1:X}] disposed twice: {0}", this, GetHashCode()));
#else
                return;
#endif
            }

            if (InputHandler.HoveredControl == this)
                InputHandler.HoveredControl = null;
            if (InputHandler.KeyboardFocus == this)
                InputHandler.KeyboardFocus = null;
            if (InputHandler.MouseFocus == this)
                InputHandler.MouseFocus = null;

            DragAndDrop.ControlDeleted(this);
            Gwen.ToolTip.ControlDeleted(this);
            Animation.Cancel(this);

            foreach (ControlBase child in m_Children)
                child.Dispose();

            m_Children.Clear();

            m_Disposed = true;
            GC.SuppressFinalize(this);
        }

#if DEBUG

        ~ControlBase()
        {
           // throw new InvalidOperationException(String.Format("IDisposable object finalized [{1:X}]: {0}", this, GetHashCode()));
            Debug.WriteLine(String.Format("IDisposable object finalized: {0}", GetType()));
        }

#endif

        /// <summary>
        /// Detaches the control from canvas and adds to the deletion queue (processed in Canvas.DoThink).
        /// </summary>
        public void DelayedDelete()
        {
            GetCanvas().AddDelayedDelete(this);
        }

        public override string ToString()
        {
            if (this is MenuItem)
                return "[MenuItem: " + (this as MenuItem).Text + "]";
            if (this is Label)
                return "[Label: " + (this as Label).Text + "]";
            if (this is ControlInternal.Text)
                return "[Text: " + (this as ControlInternal.Text).String + "]";
            return GetType().ToString();
        }

        /// <summary>
        /// Gets the canvas (root parent) of the control.
        /// </summary>
        /// <returns></returns>
        public virtual Canvas GetCanvas()
        {
            ControlBase canvas = m_Parent;
            if (canvas == null)
                return null;

            return canvas.GetCanvas();
        }

        /// <summary>
        /// Enables the control.
        /// </summary>
        public void Enable()
        {
            IsDisabled = false;
        }

        /// <summary>
        /// Disables the control.
        /// </summary>
        public virtual void Disable()
        {
            IsDisabled = true;
        }

        /// <summary>
        /// Default accelerator handler.
        /// </summary>
        /// <param name="control">Event source.</param>
        private void DefaultAcceleratorHandler(ControlBase control, EventArgs args)
        {
            OnAccelerator();
        }

        /// <summary>
        /// Default accelerator handler.
        /// </summary>
        protected virtual void OnAccelerator()
        {
        }

        /// <summary>
        /// Hides the control.
        /// </summary>
        public virtual void Hide()
        {
            IsHidden = true;
        }

        /// <summary>
        /// Shows the control.
        /// </summary>
        public virtual void Show()
        {
            IsHidden = false;
        }

        /// <summary>
        /// Creates a tooltip for the control.
        /// </summary>
        /// <param name="text">Tooltip text.</param>
        public virtual void SetToolTipText(string text)
        {
            Tooltip = text;
        }

        /// <summary>
        /// Invalidates the control's children (relayout/repaint).
        /// </summary>
        /// <param name="recursive">Determines whether the operation should be carried recursively.</param>
        protected virtual void InvalidateChildren(bool recursive = false)
        {
            foreach (ControlBase child in m_Children)
            {
                child.Invalidate();
                if (recursive)
                    child.InvalidateChildren(true);
            }

            if (m_InnerPanel != null)
            {
                foreach (ControlBase child in m_InnerPanel.m_Children)
                {
                    child.Invalidate();
                    if (recursive)
                        child.InvalidateChildren(true);
                }
            }
        }

        /// <summary>
        /// Invalidates the control.
        /// </summary>
        /// <remarks>
        /// Causes layout, repaint, invalidates cached texture.
        /// </remarks>
        public virtual void Invalidate()
        {
            m_NeedsLayout = true;
            m_CacheTextureDirty = true;
        }

        /// <summary>
        /// Sends the control to the bottom of paren't visibility stack.
        /// </summary>
        public virtual void SendToBack()
        {
            if (m_ActualParent == null)
                return;
            if (m_ActualParent.m_Children.Count == 0)
                return;
            if (m_ActualParent.m_Children.First() == this)
                return;

            m_ActualParent.m_Children.Remove(this);
            m_ActualParent.m_Children.Insert(0, this);

            InvalidateParent();
        }

        /// <summary>
        /// Brings the control to the top of paren't visibility stack.
        /// </summary>
        public virtual void BringToFront()
        {
            if (m_ActualParent == null)
                return;
            if (m_ActualParent.m_Children.Last() == this)
                return;

            m_ActualParent.m_Children.Remove(this);
            m_ActualParent.m_Children.Add(this);
            InvalidateParent();
            Redraw();
        }

        public virtual void BringNextToControl(ControlBase child, bool behind)
        {
            if (null == m_ActualParent)
                return;

            m_ActualParent.m_Children.Remove(this);

            // todo: validate
            int idx = m_ActualParent.m_Children.IndexOf(child);
            if (idx == m_ActualParent.m_Children.Count - 1)
            {
                BringToFront();
                return;
            }

            if (behind)
            {
                ++idx;

                if (idx == m_ActualParent.m_Children.Count - 1)
                {
                    BringToFront();
                    return;
                }
            }

            m_ActualParent.m_Children.Insert(idx, this);
            InvalidateParent();
        }

        /// <summary>
        /// Finds a child by name.
        /// </summary>
        /// <param name="name">Child name.</param>
        /// <param name="recursive">Determines whether the search should be recursive.</param>
        /// <returns>Found control or null.</returns>
        public virtual ControlBase FindChildByName(string name, bool recursive = false)
        {
            ControlBase b = m_Children.Find(x => x.m_Name == name);
            if (b != null)
                return b;

            if (recursive)
            {
                foreach (ControlBase child in m_Children)
                {
                    b = child.FindChildByName(name, true);
                    if (b != null)
                        return b;
                }
            }
            return null;
        }

        /// <summary>
        /// Attaches specified control as a child of this one.
        /// </summary>
        /// <remarks>
        /// If InnerPanel is not null, it will become the parent.
        /// </remarks>
        /// <param name="child">Control to be added as a child.</param>
        public virtual void AddChild(ControlBase child)
        {
            if (m_InnerPanel != null)
            {
                m_InnerPanel.AddChild(child);
            }
            else {
                m_Children.Add(child);
                child.m_ActualParent = this;
            }
            OnChildAdded(child);
        }

        /// <summary>
        /// Detaches specified control from this one.
        /// </summary>
        /// <param name="child">Child to be removed.</param>
        /// <param name="dispose">Determines whether the child should be disposed (added to delayed delete queue).</param>
        public virtual void RemoveChild(ControlBase child, bool dispose)
        {
            // If we removed our innerpanel
            // remove our pointer to it
            if (m_InnerPanel == child)
            {
                m_Children.Remove(m_InnerPanel);
                m_InnerPanel.DelayedDelete();
                m_InnerPanel = null;
                return;
            }

            if (m_InnerPanel != null && m_InnerPanel.Children.Contains(child))
            {
                m_InnerPanel.RemoveChild(child, dispose);
                return;
            }

            m_Children.Remove(child);
            OnChildRemoved(child);

            if (dispose)
                child.DelayedDelete();
        }

        /// <summary>
        /// Removes all children (and disposes them).
        /// </summary>
        public virtual void DeleteAllChildren()
        {
            // todo: probably shouldn't invalidate after each removal
            while (m_Children.Count > 0)
                RemoveChild(m_Children[0], true);
        }

        /// <summary>
        /// Handler invoked when a child is added.
        /// </summary>
        /// <param name="child">Child added.</param>
        protected virtual void OnChildAdded(ControlBase child)
        {
            Invalidate();
        }

        /// <summary>
        /// Handler invoked when a child is removed.
        /// </summary>
        /// <param name="child">Child removed.</param>
        protected virtual void OnChildRemoved(ControlBase child)
        {
            Invalidate();
        }

        /// <summary>
        /// Moves the control by a specific amount.
        /// </summary>
        /// <param name="x">X-axis movement.</param>
        /// <param name="y">Y-axis movement.</param>
        public virtual void MoveBy(int x, int y)
        {
            SetBounds(X + x, Y + y, Width, Height);
        }

        /// <summary>
        /// Moves the control to a specific point.
        /// </summary>
        /// <param name="x">Target x coordinate.</param>
        /// <param name="y">Target y coordinate.</param>
        public virtual void MoveTo(float x, float y)
        {
            MoveTo((int)x, (int)y);
        }

        /// <summary>
        /// Moves the control to a specific point, clamping on paren't bounds if RestrictToParent is set.
        /// </summary>
        /// <param name="x">Target x coordinate.</param>
        /// <param name="y">Target y coordinate.</param>
        public virtual void MoveTo(int x, int y)
        {
            if (RestrictToParent && (Parent != null))
            {
                ControlBase parent = Parent;
                if (x - Padding.Left < parent.Margin.Left)
                    x = parent.Margin.Left + Padding.Left;
                if (y - Padding.Top < parent.Margin.Top)
                    y = parent.Margin.Top + Padding.Top;
                if (x + Width + Padding.Right > parent.Width - parent.Margin.Right)
                    x = parent.Width - parent.Margin.Right - Width - Padding.Right;
                if (y + Height + Padding.Bottom > parent.Height - parent.Margin.Bottom)
                    y = parent.Height - parent.Margin.Bottom - Height - Padding.Bottom;
            }

            SetBounds(x, y, Width, Height);
        }

        /// <summary>
        /// Sets the control position.
        /// </summary>
        /// <param name="x">Target x coordinate.</param>
        /// <param name="y">Target y coordinate.</param>
        public virtual void SetPosition(float x, float y)
        {
            SetPosition((int)x, (int)y);
        }

        /// <summary>
        /// Sets the control position.
        /// </summary>
        /// <param name="x">Target x coordinate.</param>
        /// <param name="y">Target y coordinate.</param>
        public virtual void SetPosition(int x, int y)
        {
            SetBounds(x, y, Width, Height);
        }

        /// <summary>
        /// Sets the control size.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        /// <returns>True if bounds changed.</returns>
        public virtual bool SetSize(int width, int height)
        {
            return SetBounds(X, Y, width, height);
        }

        /// <summary>
        /// Sets the control bounds.
        /// </summary>
        /// <param name="bounds">New bounds.</param>
        /// <returns>True if bounds changed.</returns>
        public virtual bool SetBounds(Rectangle bounds)
        {
            return SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Sets the control bounds.
        /// </summary>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <returns>
        /// True if bounds changed.
        /// </returns>
        public virtual bool SetBounds(float x, float y, float width, float height)
        {
            return SetBounds((int)x, (int)y, (int)width, (int)height);
        }

        /// <summary>
        /// Sets the control bounds.
        /// </summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <returns>
        /// True if bounds changed.
        /// </returns>
        public virtual bool SetBounds(int x, int y, int width, int height)
        {
            if (m_Bounds.X == x &&
                m_Bounds.Y == y &&
                m_Bounds.Width == width &&
                m_Bounds.Height == height)
                return false;

            Rectangle oldBounds = Bounds;

            m_Bounds.X = x;
            m_Bounds.Y = y;

            m_Bounds.Width = width;
            m_Bounds.Height = height;

            OnBoundsChanged(oldBounds);

            if (BoundsChanged != null)
                BoundsChanged.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Positions the control inside its parent.
        /// </summary>
        /// <param name="pos">Target position.</param>
        /// <param name="xpadding">X padding.</param>
        /// <param name="ypadding">Y padding.</param>
        public virtual void Position(Pos pos, int xpadding = 0, int ypadding = 0) // todo: a bit ambiguous name
        {
            int w = Parent.Width;
            int h = Parent.Height;
            Padding padding = Parent.Padding;

            int x = X;
            int y = Y;
            if (0 != (pos & Pos.Left)) x = padding.Left + xpadding;
            if (0 != (pos & Pos.Right)) x = w - Width - padding.Right - xpadding;
            if (0 != (pos & Pos.CenterH))
                x = (int)(padding.Left + xpadding + (w - Width - padding.Left - padding.Right) * 0.5f);

            if (0 != (pos & Pos.Top)) y = padding.Top + ypadding;
            if (0 != (pos & Pos.Bottom)) y = h - Height - padding.Bottom - ypadding;
            if (0 != (pos & Pos.CenterV))
                y = (int)(padding.Top + ypadding + (h - Height - padding.Bottom - padding.Top) * 0.5f);

            SetPosition(x, y);
        }

        /// <summary>
        /// Handler invoked when control's bounds change.
        /// </summary>
        /// <param name="oldBounds">Old bounds.</param>
        protected virtual void OnBoundsChanged(Rectangle oldBounds)
        {
            //Anything that needs to update on size changes
            //Iterate my children and tell them I've changed
            //
            if (Parent != null)
                Parent.OnChildBoundsChanged(oldBounds, this);

            if (m_Bounds.Width != oldBounds.Width || m_Bounds.Height != oldBounds.Height)
            {
                Invalidate();
            }

            Redraw();
            UpdateRenderBounds();
        }

        /// <summary>
        /// Handler invoked when control's scale changes.
        /// </summary>
        protected virtual void OnScaleChanged()
        {
            foreach (ControlBase child in m_Children)
            {
                child.OnScaleChanged();
            }
        }

        /// <summary>
        /// Handler invoked when control children's bounds change.
        /// </summary>
        protected virtual void OnChildBoundsChanged(Rectangle oldChildBounds, ControlBase child)
        {
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void Render(Skin.SkinBase skin)
        {
        }

        /// <summary>
        /// Renders the control to a cache using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        /// <param name="master">Root parent.</param>
        protected virtual void DoCacheRender(Skin.SkinBase skin, ControlBase master)
        {
            Renderer.RendererBase render = skin.Renderer;
            Renderer.ICacheToTexture cache = render.CTT;

            if (cache == null)
                return;

            Point oldRenderOffset = render.RenderOffset;
            Rectangle oldRegion = render.ClipRegion;

            if (this != master)
            {
                render.AddRenderOffset(Bounds);
                render.AddClipRegion(Bounds);
            }
            else
            {
                render.RenderOffset = Point.Empty;
                render.ClipRegion = new Rectangle(0, 0, Width, Height);
            }

            if (m_CacheTextureDirty && render.ClipRegionVisible)
            {
                render.StartClip();

                if (ShouldCacheToTexture)
                    cache.SetupCacheTexture(this);

                //Render myself first
                //var old = render.ClipRegion;
                //render.ClipRegion = Bounds;
                //var old = render.RenderOffset;
                //render.RenderOffset = new Point(Bounds.X, Bounds.Y);
                Render(skin);
                //render.RenderOffset = old;
                //render.ClipRegion = old;

                if (m_Children.Count > 0)
                {
                    //Now render my kids
                    foreach (ControlBase child in m_Children)
                    {
                        if (child.IsHidden)
                            continue;
                        child.DoCacheRender(skin, master);
                    }
                }

                if (ShouldCacheToTexture)
                {
                    cache.FinishCacheTexture(this);
                    m_CacheTextureDirty = false;
                }
            }

            render.ClipRegion = oldRegion;
            render.StartClip();
            render.RenderOffset = oldRenderOffset;

            if (ShouldCacheToTexture)
                cache.DrawCachedControlTexture(this);
        }

        /// <summary>
        /// Rendering logic implementation.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        internal virtual void DoRender(Skin.SkinBase skin)
        {
            // If this control has a different skin,
            // then so does its children.
            if (m_Skin != null)
                skin = m_Skin;

            // Do think
            Think();

            Renderer.RendererBase render = skin.Renderer;

            if (render.CTT != null && ShouldCacheToTexture)
            {
                DoCacheRender(skin, this);
                return;
            }

            RenderRecursive(skin, Bounds);

            if (DrawDebugOutlines)
                skin.DrawDebugOutlines(this);
        }

        /// <summary>
        /// Recursive rendering logic.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        /// <param name="clipRect">Clipping rectangle.</param>
        protected virtual void RenderRecursive(Skin.SkinBase skin, Rectangle clipRect)
        {
            Renderer.RendererBase render = skin.Renderer;
            Point oldRenderOffset = render.RenderOffset;

            render.AddRenderOffset(clipRect);

            RenderUnder(skin);

            Rectangle oldRegion = render.ClipRegion;

            if (ShouldClip)
            {
                render.AddClipRegion(clipRect);

                if (!render.ClipRegionVisible)
                {
                    render.RenderOffset = oldRenderOffset;
                    render.ClipRegion = oldRegion;
                    return;
                }

                render.StartClip();
            }

            //Render myself first
            Render(skin);

            if (m_Children.Count > 0)
            {
                //Now render my kids
                foreach (ControlBase child in m_Children)
                {
                    if (child.IsHidden)
                        continue;
                    child.DoRender(skin);
                }
            }

            render.ClipRegion = oldRegion;
            render.StartClip();
            RenderOver(skin);

            RenderFocus(skin);

            render.RenderOffset = oldRenderOffset;
        }

        /// <summary>
        /// Sets the control's skin.
        /// </summary>
        /// <param name="skin">New skin.</param>
        /// <param name="doChildren">Deterines whether to change children skin.</param>
        public virtual void SetSkin(Skin.SkinBase skin, bool doChildren = false)
        {
            if (m_Skin == skin)
                return;
            m_Skin = skin;
            Invalidate();
            Redraw();
            OnSkinChanged(skin);

            if (doChildren)
            {
                foreach (ControlBase child in m_Children)
                {
                    child.SetSkin(skin, true);
                }
            }
        }

        /// <summary>
        /// Handler invoked when control's skin changes.
        /// </summary>
        /// <param name="newSkin">New skin.</param>
        protected virtual void OnSkinChanged(Skin.SkinBase newSkin)
        {
        }

        /// <summary>
        /// Handler invoked on mouse wheel event.
        /// </summary>
        /// <param name="delta">Scroll delta.</param>
        protected virtual bool OnMouseWheeled(int delta)
        {
            if (m_ActualParent != null)
                return m_ActualParent.OnMouseWheeled(delta);

            return false;
        }

        /// <summary>
        /// Invokes mouse wheeled event (used by input system).
        /// </summary>
        internal bool InputMouseWheeled(int delta)
        {
            return OnMouseWheeled(delta);
        }

        /// <summary>
        /// Handler invoked on mouse moved event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="dx">X change.</param>
        /// <param name="dy">Y change.</param>
        protected virtual void OnMouseMoved(int x, int y, int dx, int dy)
        {
        }

        /// <summary>
        /// Invokes mouse moved event (used by input system).
        /// </summary>
        internal void InputMouseMoved(int x, int y, int dx, int dy)
        {
            OnMouseMoved(x, y, dx, dy);
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected virtual void OnMouseClickedLeft(int x, int y, bool down)
        {
            if (down && Clicked != null)
                Clicked(this, new ClickedEventArgs(x, y, down));
        }

        /// <summary>
        /// Invokes left mouse click event (used by input system).
        /// </summary>
        internal void InputMouseClickedLeft(int x, int y, bool down)
        {
            OnMouseClickedLeft(x, y, down);
        }

        /// <summary>
        /// Handler invoked on mouse click (right) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected virtual void OnMouseClickedRight(int x, int y, bool down)
        {
            if (down && RightClicked != null)
                RightClicked(this, new ClickedEventArgs(x, y, down));
        }

        /// <summary>
        /// Invokes right mouse click event (used by input system).
        /// </summary>
        internal void InputMouseClickedRight(int x, int y, bool down)
        {
            OnMouseClickedRight(x, y, down);
        }

        /// <summary>
        /// Handler invoked on mouse double click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        protected virtual void OnMouseDoubleClickedLeft(int x, int y)
        {
            // [omeg] should this be called?
            // [halfofastaple] Maybe. Technically, a double click is still technically a single click. However, this shouldn't be called here, and
            //					Should be called by the event handler.
            OnMouseClickedLeft(x, y, true);

            if (DoubleClicked != null)
                DoubleClicked(this, new ClickedEventArgs(x, y, true));
        }

        /// <summary>
        /// Invokes left double mouse click event (used by input system).
        /// </summary>
        internal void InputMouseDoubleClickedLeft(int x, int y)
        {
            OnMouseDoubleClickedLeft(x, y);
        }

        /// <summary>
        /// Handler invoked on mouse double click (right) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        protected virtual void OnMouseDoubleClickedRight(int x, int y)
        {
            // [halfofastaple] See: OnMouseDoubleClicked for discussion on triggering single clicks in a double click event
            OnMouseClickedRight(x, y, true);

            if (DoubleRightClicked != null)
                DoubleRightClicked(this, new ClickedEventArgs(x, y, true));
        }

        /// <summary>
        /// Invokes right double mouse click event (used by input system).
        /// </summary>
        internal void InputMouseDoubleClickedRight(int x, int y)
        {
            OnMouseDoubleClickedRight(x, y);
        }

        /// <summary>
        /// Handler invoked on mouse cursor entering control's bounds.
        /// </summary>
        protected virtual void OnMouseEntered()
        {
            if (HoverEnter != null)
                HoverEnter.Invoke(this, EventArgs.Empty);

            if (Tooltip != null)
                Gwen.ToolTip.Enable(this);
            else if (Parent != null && Parent.Tooltip != null)
                Gwen.ToolTip.Enable(Parent);

            Redraw();
        }

        /// <summary>
        /// Invokes mouse enter event (used by input system).
        /// </summary>
        internal void InputMouseEntered()
        {
            OnMouseEntered();
        }

        /// <summary>
        /// Handler invoked on mouse cursor leaving control's bounds.
        /// </summary>
        protected virtual void OnMouseLeft()
        {
            if (HoverLeave != null)
                HoverLeave.Invoke(this, EventArgs.Empty);

            if (Tooltip != null)
                Gwen.ToolTip.Disable(this);

            Redraw();
        }

        /// <summary>
        /// Invokes mouse leave event (used by input system).
        /// </summary>
        internal void InputMouseLeft()
        {
            OnMouseLeft();
        }

        /// <summary>
        /// Focuses the control.
        /// </summary>
        public virtual void Focus()
        {
            if (InputHandler.KeyboardFocus == this)
                return;

            if (InputHandler.KeyboardFocus != null)
                InputHandler.KeyboardFocus.OnLostKeyboardFocus();

            InputHandler.KeyboardFocus = this;
            OnKeyboardFocus();
            Redraw();
        }

        /// <summary>
        /// Unfocuses the control.
        /// </summary>
        public virtual void Blur()
        {
            if (InputHandler.KeyboardFocus != this)
                return;

            InputHandler.KeyboardFocus = null;
            OnLostKeyboardFocus();
            Redraw();
        }

        /// <summary>
        /// Control has been clicked - invoked by input system. Windows use it to propagate activation.
        /// </summary>
        public virtual void Touch()
        {
            if (Parent != null)
                Parent.OnChildTouched(this);
        }

        protected virtual void OnChildTouched(ControlBase control)
        {
            Touch();
        }

        /// <summary>
        /// Gets a child by its coordinates.
        /// </summary>
        /// <param name="x">Child X.</param>
        /// <param name="y">Child Y.</param>
        /// <returns>Control or null if not found.</returns>
        public virtual ControlBase GetControlAt(int x, int y)
        {
            if (IsHidden)
                return null;

            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;

            // todo: convert to linq FindLast
            var rev = ((IList<ControlBase>)m_Children).Reverse(); // IList.Reverse creates new list, List.Reverse works in place.. go figure
            foreach (ControlBase child in rev)
            {
                ControlBase found = child.GetControlAt(x - child.X, y - child.Y);
                if (found != null)
                    return found;
            }

            if (!MouseInputEnabled)
                return null;

            return this;
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void Layout(Skin.SkinBase skin)
        {
            if (skin.Renderer.CTT != null && ShouldCacheToTexture)
                skin.Renderer.CTT.CreateControlCacheTexture(this);
        }

        /// <summary>
        /// Recursively lays out the control's interior according to alignment, margin, padding, dock etc.
        /// </summary>
        public void Layout()
        {
            RecurseLayout(Skin);
        }

        /// <summary>
        /// Recursively lays out the control's interior according to alignment, margin, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void RecurseLayout(Skin.SkinBase skin)
        {
            if (m_Skin != null)
                skin = m_Skin;
            if (IsHidden)
                return;

            if (m_NeedsLayout)
            {
                m_NeedsLayout = false;
                Layout(skin);
            }

            Rectangle bounds = RenderBounds;

            // Adjust bounds for padding
            bounds.X += m_Padding.Left;
            bounds.Width -= m_Padding.Left + m_Padding.Right;
            bounds.Y += m_Padding.Top;
            bounds.Height -= m_Padding.Top + m_Padding.Bottom;

            foreach (ControlBase child in m_Children)
            {
                if (child.IsHidden)
                    continue;

                Pos dock = child.Dock;

                if (0 != (dock & Pos.Fill))
                    continue;

                if (0 != (dock & Pos.Top))
                {
                    Margin margin = child.Margin;

                    child.SetBounds(bounds.X + margin.Left, bounds.Y + margin.Top,
                                    bounds.Width - margin.Left - margin.Right, child.Height);

                    int height = margin.Top + margin.Bottom + child.Height;
                    bounds.Y += height;
                    bounds.Height -= height;
                }

                if (0 != (dock & Pos.Left))
                {
                    Margin margin = child.Margin;

                    child.SetBounds(bounds.X + margin.Left, bounds.Y + margin.Top, child.Width,
                                      bounds.Height - margin.Top - margin.Bottom);

                    int width = margin.Left + margin.Right + child.Width;
                    bounds.X += width;
                    bounds.Width -= width;
                }

                if (0 != (dock & Pos.Right))
                {
                    // TODO: THIS MARGIN CODE MIGHT NOT BE FULLY FUNCTIONAL
                    Margin margin = child.Margin;

                    child.SetBounds((bounds.X + bounds.Width) - child.Width - margin.Right, bounds.Y + margin.Top,
                                      child.Width, bounds.Height - margin.Top - margin.Bottom);

                    int width = margin.Left + margin.Right + child.Width;
                    bounds.Width -= width;
                }

                if (0 != (dock & Pos.Bottom))
                {
                    // TODO: THIS MARGIN CODE MIGHT NOT BE FULLY FUNCTIONAL
                    Margin margin = child.Margin;

                    child.SetBounds(bounds.X + margin.Left,
                                      (bounds.Y + bounds.Height) - child.Height - margin.Bottom,
                                      bounds.Width - margin.Left - margin.Right, child.Height);
                    bounds.Height -= child.Height + margin.Bottom + margin.Top;
                }

                child.RecurseLayout(skin);
            }

            m_InnerBounds = bounds;

            //
            // Fill uses the left over space, so do that now.
            //
            foreach (ControlBase child in m_Children)
            {
                Pos dock = child.Dock;

                if (!(0 != (dock & Pos.Fill)))
                    continue;

                Margin margin = child.Margin;

                child.SetBounds(bounds.X + margin.Left, bounds.Y + margin.Top,
                                  bounds.Width - margin.Left - margin.Right, bounds.Height - margin.Top - margin.Bottom);
                child.RecurseLayout(skin);
            }

            PostLayout(skin);

            if (IsTabable)
            {
                if (GetCanvas().FirstTab == null)
                    GetCanvas().FirstTab = this;
                if (GetCanvas().NextTab == null)
                    GetCanvas().NextTab = this;
            }

            if (InputHandler.KeyboardFocus == this)
            {
                GetCanvas().NextTab = null;
            }
        }

        /// <summary>
        /// Checks if the given control is a child of this instance.
        /// </summary>
        /// <param name="child">Control to examine.</param>
        /// <returns>True if the control is out child.</returns>
        public bool IsChild(ControlBase child)
        {
            return m_Children.Contains(child);
        }

        /// <summary>
        /// Converts local coordinates to canvas coordinates.
        /// </summary>
        /// <param name="pnt">Local coordinates.</param>
        /// <returns>Canvas coordinates.</returns>
        public virtual Point LocalPosToCanvas(Point pnt)
        {
            if (m_Parent != null)
            {
                int x = pnt.X + X;
                int y = pnt.Y + Y;

                // If our parent has an innerpanel and we're a child of it
                // add its offset onto us.
                //
                if (m_Parent.m_InnerPanel != null && m_Parent.m_InnerPanel.IsChild(this))
                {
                    x += m_Parent.m_InnerPanel.X;
                    y += m_Parent.m_InnerPanel.Y;
                }

                return m_Parent.LocalPosToCanvas(new Point(x, y));
            }

            return pnt;
        }

        /// <summary>
        /// Converts canvas coordinates to local coordinates.
        /// </summary>
        /// <param name="pnt">Canvas coordinates.</param>
        /// <returns>Local coordinates.</returns>
        public virtual Point CanvasPosToLocal(Point pnt)
        {
            if (m_Parent != null)
            {
                int x = pnt.X - X;
                int y = pnt.Y - Y;

                // If our parent has an innerpanel and we're a child of it
                // add its offset onto us.
                //
                if (m_Parent.m_InnerPanel != null && m_Parent.m_InnerPanel.IsChild(this))
                {
                    x -= m_Parent.m_InnerPanel.X;
                    y -= m_Parent.m_InnerPanel.Y;
                }

                return m_Parent.CanvasPosToLocal(new Point(x, y));
            }

            return pnt;
        }

        /// <summary>
        /// Closes all menus recursively.
        /// </summary>
        public virtual void CloseMenus()
        {
            //Debug.Print("Base.CloseMenus: {0}", this);

            // todo: not very efficient with the copying and recursive closing, maybe store currently open menus somewhere (canvas)?
            var childrenCopy = m_Children.FindAll(x => true);
            foreach (ControlBase child in childrenCopy)
            {
                child.CloseMenus();
            }
        }

        /// <summary>
        /// Copies Bounds to RenderBounds.
        /// </summary>
        protected virtual void UpdateRenderBounds()
        {
            m_RenderBounds.X = 0;
            m_RenderBounds.Y = 0;

            m_RenderBounds.Width = m_Bounds.Width;
            m_RenderBounds.Height = m_Bounds.Height;
        }

        /// <summary>
        /// Sets mouse cursor to current cursor.
        /// </summary>
        public virtual void UpdateCursor()
        {
            Platform.Neutral.SetCursor(m_Cursor);
        }

        // giver
        public virtual Package DragAndDrop_GetPackage(int x, int y)
        {
            return m_DragAndDrop_Package;
        }

        // giver
        public virtual bool DragAndDrop_Draggable()
        {
            if (m_DragAndDrop_Package == null)
                return false;

            return m_DragAndDrop_Package.IsDraggable;
        }

        // giver
        public virtual void DragAndDrop_SetPackage(bool draggable, string name = "", object userData = null)
        {
            if (m_DragAndDrop_Package == null)
            {
                m_DragAndDrop_Package = new Package();
                m_DragAndDrop_Package.IsDraggable = draggable;
                m_DragAndDrop_Package.Name = name;
                m_DragAndDrop_Package.UserData = userData;
            }
        }

        // giver
        public virtual bool DragAndDrop_ShouldStartDrag()
        {
            return true;
        }

        // giver
        public virtual void DragAndDrop_StartDragging(Package package, int x, int y)
        {
            package.HoldOffset = CanvasPosToLocal(new Point(x, y));
            package.DrawControl = this;
        }

        // giver
        public virtual void DragAndDrop_EndDragging(bool success, int x, int y)
        {
        }

        // receiver
        public virtual bool DragAndDrop_HandleDrop(Package p, int x, int y)
        {
            DragAndDrop.SourceControl.Parent = this;
            return true;
        }

        // receiver
        public virtual void DragAndDrop_HoverEnter(Package p, int x, int y)
        {
        }

        // receiver
        public virtual void DragAndDrop_HoverLeave(Package p)
        {
        }

        // receiver
        public virtual void DragAndDrop_Hover(Package p, int x, int y)
        {
        }

        // receiver
        public virtual bool DragAndDrop_CanAcceptPackage(Package p)
        {
            return false;
        }

        /// <summary>
        /// Resizes the control to fit its children.
        /// </summary>
        /// <param name="width">Determines whether to change control's width.</param>
        /// <param name="height">Determines whether to change control's height.</param>
        /// <returns>True if bounds changed.</returns>
        public virtual bool SizeToChildren(bool width = true, bool height = true)
        {
            Point size = GetChildrenSize();
            size.X += Padding.Right;
            size.Y += Padding.Bottom;
            return SetSize(width ? size.X : Width, height ? size.Y : Height);
        }

        /// <summary>
        /// Returns the total width and height of all children.
        /// </summary>
        /// <remarks>Default implementation returns maximum size of children since the layout is unknown.
        /// Implement this in derived compound controls to properly return their size.</remarks>
        /// <returns></returns>
        public virtual Point GetChildrenSize()
        {
            Point size = Point.Empty;

            foreach (ControlBase child in m_Children)
            {
                if (child.IsHidden)
                    continue;

                size.X = Math.Max(size.X, child.Right);
                size.Y = Math.Max(size.Y, child.Bottom);
            }

            return size;
        }

        /// <summary>
        /// Handles keyboard accelerator.
        /// </summary>
        /// <param name="accelerator">Accelerator text.</param>
        /// <returns>True if handled.</returns>
        internal virtual bool HandleAccelerator(string accelerator)
        {
            if (InputHandler.KeyboardFocus == this || !AccelOnlyFocus)
            {
                if (m_Accelerators.ContainsKey(accelerator))
                {
                    m_Accelerators[accelerator].Invoke(this, EventArgs.Empty);
                    return true;
                }
            }

            return m_Children.Any(child => child.HandleAccelerator(accelerator));
        }

        /// <summary>
        /// Adds keyboard accelerator.
        /// </summary>
        /// <param name="accelerator">Accelerator text.</param>
        /// <param name="handler">Handler.</param>
        public void AddAccelerator(string accelerator, GwenEventHandler<EventArgs> handler)
        {
            accelerator = accelerator.Trim().ToUpperInvariant();
            m_Accelerators[accelerator] = handler;
        }

        /// <summary>
        /// Adds keyboard accelerator with a default handler.
        /// </summary>
        /// <param name="accelerator">Accelerator text.</param>
        public void AddAccelerator(string accelerator)
        {
            m_Accelerators[accelerator] = DefaultAcceleratorHandler;
        }

        /// <summary>
        /// Function invoked after layout.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void PostLayout(Skin.SkinBase skin)
        {
        }

        /// <summary>
        /// Re-renders the control, invalidates cached texture.
        /// </summary>
        public virtual void Redraw()
        {
            UpdateColors();
            m_CacheTextureDirty = true;
            if (m_Parent != null)
                m_Parent.Redraw();
        }

        /// <summary>
        /// Updates control colors.
        /// </summary>
        /// <remarks>
        /// Used in composite controls like lists to differentiate row colors etc.
        /// </remarks>
        public virtual void UpdateColors()
        {
        }

        /// <summary>
        /// Invalidates control's parent.
        /// </summary>
        public void InvalidateParent()
        {
            if (m_Parent != null)
            {
                m_Parent.Invalidate();
            }
        }

        /// <summary>
        /// Handler for keyboard events.
        /// </summary>
        /// <param name="key">Key pressed.</param>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyPressed(Key key, bool down = true)
        {
            bool handled = false;
            switch (key)
            {
                case Key.Tab: handled = OnKeyTab(down); break;
                case Key.Space: handled = OnKeySpace(down); break;
                case Key.Home: handled = OnKeyHome(down); break;
                case Key.End: handled = OnKeyEnd(down); break;
                case Key.Return: handled = OnKeyReturn(down); break;
                case Key.Backspace: handled = OnKeyBackspace(down); break;
                case Key.Delete: handled = OnKeyDelete(down); break;
                case Key.Right: handled = OnKeyRight(down); break;
                case Key.Left: handled = OnKeyLeft(down); break;
                case Key.Up: handled = OnKeyUp(down); break;
                case Key.Down: handled = OnKeyDown(down); break;
                case Key.Escape: handled = OnKeyEscape(down); break;
                default: break;
            }

            if (!handled && Parent != null)
                Parent.OnKeyPressed(key, down);

            return handled;
        }

        /// <summary>
        /// Invokes key press event (used by input system).
        /// </summary>
        internal bool InputKeyPressed(Key key, bool down = true)
        {
            return OnKeyPressed(key, down);
        }

        /// <summary>
        /// Handler for keyboard events.
        /// </summary>
        /// <param name="key">Key pressed.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyReleaseed(Key key)
        {
            return OnKeyPressed(key, false);
        }

        /// <summary>
        /// Handler for Tab keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyTab(bool down)
        {
            if (!down)
                return true;

            if (GetCanvas().NextTab != null)
            {
                GetCanvas().NextTab.Focus();
                Redraw();
            }

            return true;
        }

        /// <summary>
        /// Handler for Space keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeySpace(bool down) { return false; }

        /// <summary>
        /// Handler for Return keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyReturn(bool down) { return false; }

        /// <summary>
        /// Handler for Backspace keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyBackspace(bool down) { return false; }

        /// <summary>
        /// Handler for Delete keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyDelete(bool down) { return false; }

        /// <summary>
        /// Handler for Right Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyRight(bool down) { return false; }

        /// <summary>
        /// Handler for Left Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyLeft(bool down) { return false; }

        /// <summary>
        /// Handler for Home keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyHome(bool down) { return false; }

        /// <summary>
        /// Handler for End keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyEnd(bool down) { return false; }

        /// <summary>
        /// Handler for Up Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyUp(bool down) { return false; }

        /// <summary>
        /// Handler for Down Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyDown(bool down) { return false; }

        /// <summary>
        /// Handler for Escape keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnKeyEscape(bool down) { return false; }

        /// <summary>
        /// Handler for Paste event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected virtual void OnPaste(ControlBase from, EventArgs args)
        {
        }

        /// <summary>
        /// Handler for Copy event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected virtual void OnCopy(ControlBase from, EventArgs args)
        {
        }

        /// <summary>
        /// Handler for Cut event.
        /// </summary>
        /// <param name="from">Source control.</param>
		protected virtual void OnCut(ControlBase from, EventArgs args)
        {
        }

        /// <summary>
        /// Handler for Select All event.
        /// </summary>
        /// <param name="from">Source control.</param>
		protected virtual void OnSelectAll(ControlBase from, EventArgs args)
        {
        }

        internal void InputCopy(ControlBase from)
        {
            OnCopy(from, EventArgs.Empty);
        }

        internal void InputPaste(ControlBase from)
        {
            OnPaste(from, EventArgs.Empty);
        }

        internal void InputCut(ControlBase from)
        {
            OnCut(from, EventArgs.Empty);
        }

        internal void InputSelectAll(ControlBase from)
        {
            OnSelectAll(from, EventArgs.Empty);
        }

        /// <summary>
        /// Renders the focus overlay.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void RenderFocus(Skin.SkinBase skin)
        {
            if (InputHandler.KeyboardFocus != this)
                return;
            if (!IsTabable)
                return;

            skin.DrawKeyboardHighlight(this, RenderBounds, 3);
        }

        /// <summary>
        /// Renders under the actual control (shadows etc).
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void RenderUnder(Skin.SkinBase skin)
        {
        }

        /// <summary>
        /// Renders over the actual control (overlays).
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected virtual void RenderOver(Skin.SkinBase skin)
        {
        }

        /// <summary>
        /// Called during rendering.
        /// </summary>
        public virtual void Think()
        {
        }

        /// <summary>
        /// Handler for gaining keyboard focus.
        /// </summary>
        protected virtual void OnKeyboardFocus()
        {
        }

        /// <summary>
        /// Handler for losing keyboard focus.
        /// </summary>
        protected virtual void OnLostKeyboardFocus()
        {
        }

        /// <summary>
        /// Handler for character input event.
        /// </summary>
        /// <param name="chr">Character typed.</param>
        /// <returns>True if handled.</returns>
        protected virtual bool OnChar(Char chr)
        {
            return false;
        }

        internal bool InputChar(Char chr)
        {
            return OnChar(chr);
        }

        public virtual void Anim_WidthIn(float length, float delay = 0.0f, float ease = 1.0f)
        {
            Animation.Add(this, new Anim.Size.Width(0, Width, length, false, delay, ease));
            Width = 0;
        }

        public virtual void Anim_HeightIn(float length, float delay, float ease)
        {
            Animation.Add(this, new Anim.Size.Height(0, Height, length, false, delay, ease));
            Height = 0;
        }

        public virtual void Anim_WidthOut(float length, bool hide, float delay, float ease)
        {
            Animation.Add(this, new Anim.Size.Width(Width, 0, length, hide, delay, ease));
        }

        public virtual void Anim_HeightOut(float length, bool hide, float delay, float ease)
        {
            Animation.Add(this, new Anim.Size.Height(Height, 0, length, hide, delay, ease));
        }

        public void FitChildrenToSize()
        {
            foreach (ControlBase child in Children)
            {
                //push them back into view if they are outside it
                child.X = Math.Min(Bounds.Width, child.X + child.Width) - child.Width;
                child.Y = Math.Min(Bounds.Height, child.Y + child.Height) - child.Height;

                //Non-negative has priority, so do it second.
                child.X = Math.Max(0, child.X);
                child.Y = Math.Max(0, child.Y);
            }
        }
    }
}