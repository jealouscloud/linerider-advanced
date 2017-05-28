using Gwen.Anim;
using Gwen.DragDrop;
using Gwen.Input;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Gwen.Controls
{
    /// <summary>
    /// Canvas control. It should be the root parent for all other controls.
    /// </summary>
    public class Canvas : ControlBase
    {
        private bool m_NeedsRedraw;
        private float m_Scale;

        private Color m_BackgroundColor;

        // [omeg] these are not created by us, so no disposing
        internal ControlBase FirstTab;

        internal ControlBase NextTab;

        internal Label m_ToolTip;

        private readonly List<IDisposable> m_DisposeQueue; // dictionary for faster access?

        /// <summary>
        /// Scale for rendering.
        /// </summary>
        public float Scale
        {
            get { return m_Scale; }
            set
            {
                if (m_Scale == value)
                    return;

                m_Scale = value;

                if (Skin != null && Skin.Renderer != null)
                    Skin.Renderer.Scale = m_Scale;

                OnScaleChanged();
                Redraw();
            }
        }

        /// <summary>
        /// Background color.
        /// </summary>
        public Color BackgroundColor { get { return m_BackgroundColor; } set { m_BackgroundColor = value; } }

        /// <summary>
        /// In most situations you will be rendering the canvas every frame.
        /// But in some situations you will only want to render when there have been changes.
        /// You can do this by checking NeedsRedraw.
        /// </summary>
        public bool NeedsRedraw { get { return m_NeedsRedraw; } set { m_NeedsRedraw = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Canvas"/> class.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        public Canvas(Skin.SkinBase skin)
        {
            SetBounds(0, 0, 10000, 10000);
            SetSkin(skin);
            Scale = 1.0f;
            BackgroundColor = Color.White;
            ShouldDrawBackground = false;

            m_DisposeQueue = new List<IDisposable>();
            m_ToolTip = new Label(this);
            m_ToolTip.TextColorOverride = Skin.Colors.TooltipText;
            m_ToolTip.Padding = new Padding(5, 3, 5, 3);
            m_ToolTip.IsHidden = true;
        }

        public override void Dispose()
        {
            ProcessDelayedDeletes();
            base.Dispose();
        }

        /// <summary>
        /// Re-renders the control, invalidates cached texture.
        /// </summary>
        public override void Redraw()
        {
            NeedsRedraw = true;
            base.Redraw();
        }

        // Children call parent.GetCanvas() until they get to
        // this top level function.
        public override Canvas GetCanvas()
        {
            return this;
        }

        /// <summary>
        /// Additional initialization (which is sometimes not appropriate in the constructor)
        /// </summary>
        protected void Initialize()
        {
        }

        /// <summary>
        /// Renders the canvas. Call in your rendering loop.
        /// </summary>
        public void RenderCanvas()
        {
            DoThink();

            Renderer.RendererBase render = Skin.Renderer;

            render.Begin();

            RecurseLayout(Skin);

            render.ClipRegion = Bounds;
            render.RenderOffset = Point.Empty;
            render.Scale = Scale;

            if (ShouldDrawBackground)
            {
                render.DrawColor = m_BackgroundColor;
                render.DrawFilledRect(RenderBounds);
            }

            DoRender(Skin);

            DragAndDrop.RenderOverlay(this, Skin);

            Gwen.ToolTip.RenderToolTip(Skin);

            render.EndClip();

            render.End();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            //skin.Renderer.rnd = new Random(1);
            base.Render(skin);
            m_NeedsRedraw = false;
        }

        /// <summary>
        /// Handler invoked when control's bounds change.
        /// </summary>
        /// <param name="oldBounds">Old bounds.</param>
        protected override void OnBoundsChanged(Rectangle oldBounds)
        {
            base.OnBoundsChanged(oldBounds);
            InvalidateChildren(true);
        }

        /// <summary>
        /// Processes input and layout. Also purges delayed delete queue.
        /// </summary>
        private void DoThink()
        {
            if (IsHidden)
                return;

            Animation.GlobalThink();

            // Reset tabbing
            NextTab = null;
            FirstTab = null;

            ProcessDelayedDeletes();

            // Check has focus etc..
            RecurseLayout(Skin);

            // If we didn't have a next tab, cycle to the start.
            if (NextTab == null)
                NextTab = FirstTab;

            InputHandler.OnCanvasThink(this);
        }

        /// <summary>
        /// Adds given control to the delete queue and detaches it from canvas. Don't call from Dispose, it modifies child list.
        /// </summary>
        /// <param name="control">Control to delete.</param>
        public void AddDelayedDelete(ControlBase control)
        {
            if (!m_DisposeQueue.Contains(control))
            {
                m_DisposeQueue.Add(control);
                RemoveChild(control, false);
            }
#if DEBUG
            else
                throw new InvalidOperationException("Control deleted twice");
#endif
        }

        private void ProcessDelayedDeletes()
        {
            //if (m_DisposeQueue.Count > 0)
            //    System.Diagnostics.Debug.Print("Canvas.ProcessDelayedDeletes: {0} items", m_DisposeQueue.Count);
            foreach (IDisposable control in m_DisposeQueue)
            {
                control.Dispose();
            }
            m_DisposeQueue.Clear();
        }

        /// <summary>
        /// Handles mouse movement events. Called from Input subsystems.
        /// </summary>
        /// <returns>True if handled.</returns>
        public bool Input_MouseMoved(int x, int y, int dx, int dy)
        {
            if (IsHidden)
                return false;

            // Todo: Handle scaling here..
            //float fScale = 1.0f / Scale();

            InputHandler.OnMouseMoved(this, x, y, dx, dy);

            if (InputHandler.HoveredControl == null) return false;
            if (InputHandler.HoveredControl == this) return false;
            if (InputHandler.HoveredControl.GetCanvas() != this) return false;

            InputHandler.HoveredControl.InputMouseMoved(x, y, dx, dy);
            InputHandler.HoveredControl.UpdateCursor();

            DragAndDrop.OnMouseMoved(InputHandler.HoveredControl, x, y);
            return true;
        }

        /// <summary>
        /// Handles mouse button events. Called from Input subsystems.
        /// </summary>
        /// <returns>True if handled.</returns>
        public bool Input_MouseButton(int button, bool down)
        {
            if (IsHidden) return false;

            return InputHandler.OnMouseClicked(this, button, down);
        }

        /// <summary>
        /// Handles keyboard events. Called from Input subsystems.
        /// </summary>
        /// <returns>True if handled.</returns>
        public bool Input_Key(Key key, bool down)
        {
            if (IsHidden) return false;
            if (key <= Key.Invalid) return false;
            if (key >= Key.Count) return false;

            return InputHandler.OnKeyEvent(this, key, down);
        }

        /// <summary>
        /// Handles keyboard events. Called from Input subsystems.
        /// </summary>
        /// <returns>True if handled.</returns>
        public bool Input_Character(char chr)
        {
            if (IsHidden) return false;
            if (char.IsControl(chr)) return false;

            //Handle Accelerators
            if (InputHandler.HandleAccelerator(this, chr))
                return true;

            //Handle characters
            if (InputHandler.KeyboardFocus == null) return false;
            if (InputHandler.KeyboardFocus.GetCanvas() != this) return false;
            if (!InputHandler.KeyboardFocus.IsVisible) return false;
            if (InputHandler.IsControlDown) return false;

            return InputHandler.KeyboardFocus.InputChar(chr);
        }

        /// <summary>
        /// Handles the mouse wheel events. Called from Input subsystems.
        /// </summary>
        /// <returns>True if handled.</returns>
        public bool Input_MouseWheel(int val)
        {
            if (IsHidden) return false;
            if (InputHandler.HoveredControl == null) return false;
            if (InputHandler.HoveredControl == this) return false;
            if (InputHandler.HoveredControl.GetCanvas() != this) return false;

            return InputHandler.HoveredControl.InputMouseWheeled(val);
        }
    }
}