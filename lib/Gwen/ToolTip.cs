using System.Drawing;
using Gwen.Controls;

namespace Gwen
{
    /// <summary>
    /// Tooltip handling.
    /// </summary>
    public static class ToolTip
    {
        private static Controls.ControlBase g_ToolTip;

        /// <summary>
        /// Enables tooltip display for the specified control.
        /// </summary>
        /// <param name="control">Target control.</param>
        public static void Enable(Controls.ControlBase control)
        {
            if (null == control.Tooltip)
                return;

            g_ToolTip = control;
        }

        /// <summary>
        /// Disables tooltip display for the specified control.
        /// </summary>
        /// <param name="control">Target control.</param>
        public static void Disable(Controls.ControlBase control)
        {
            if (g_ToolTip == control)
            {
                g_ToolTip = null;
            }
        }

        /// <summary>
        /// Disables tooltip display for the specified control.
        /// </summary>
        /// <param name="control">Target control.</param>
        public static void ControlDeleted(Controls.ControlBase control)
        {
            Disable(control);
        }

        /// <summary>
        /// Renders the currently visible tooltip.
        /// </summary>
        /// <param name="skin"></param>
        public static void RenderToolTip(Skin.SkinBase skin)
        {
            if (null == g_ToolTip) return;
            var canvas = g_ToolTip.GetCanvas();
            canvas.m_ToolTip.Text = g_ToolTip.Tooltip;
            Renderer.RendererBase render = skin.Renderer;

            Point oldRenderOffset = render.RenderOffset;
            Point mousePos = Input.InputHandler.MousePosition;
            Rectangle bounds = canvas.m_ToolTip.Bounds;
            
            Rectangle offset = Util.FloatRect(mousePos.X - bounds.Width*0.5f, mousePos.Y - bounds.Height - 10,
                                                 bounds.Width, bounds.Height);
            offset = Util.ClampRectToRect(offset, g_ToolTip.GetCanvas().Bounds);

            //Calculate offset on screen bounds
            render.AddRenderOffset(offset);
            render.EndClip();

            skin.DrawToolTip(canvas.m_ToolTip);
            canvas.m_ToolTip.DoRender(skin);

            render.RenderOffset = oldRenderOffset;
        }
    }
}
