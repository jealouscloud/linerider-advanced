using System;
using System.Drawing;
using System.Windows.Forms;
using Gwen.Controls;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Grab point for resizing.
    /// </summary>
    public class Resizer : Dragger
    {
        private Pos m_ResizeDir;

        /// <summary>
        /// Invoked when the control has been resized.
        /// </summary>
        public event GwenEventHandler<EventArgs> Resized;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resizer"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Resizer(Controls.ControlBase parent)
            : base(parent)
        {
            m_ResizeDir = Pos.Left;
            MouseInputEnabled = true;
            SetSize(6, 6);
            Target = parent;
        }

        /// <summary>
        /// Handler invoked on mouse moved event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="dx">X change.</param>
        /// <param name="dy">Y change.</param>
        protected override void OnMouseMoved(int x, int y, int dx, int dy)
        {
            if (null == m_Target) return;
            if (!m_Held) return;

            Rectangle oldBounds = m_Target.Bounds;
            Rectangle bounds = m_Target.Bounds;

            Point min = m_Target.MinimumSize;

            Point pCursorPos = m_Target.CanvasPosToLocal(new Point(x, y));

            Point delta = m_Target.LocalPosToCanvas(m_HoldPos);
            delta.X -= x;
            delta.Y -= y;

            if (0 != (m_ResizeDir & Pos.Left))
            {
                bounds.X -= delta.X;
                bounds.Width += delta.X;

                // Conform to minimum size here so we don't
                // go all weird when we snap it in the base conrt

                if (bounds.Width < min.X)
                {
                    int diff = min.X - bounds.Width;
                    bounds.Width += diff;
                    bounds.X -= diff;
                }
            }

            if (0 != (m_ResizeDir & Pos.Top))
            {
                bounds.Y -= delta.Y;
                bounds.Height += delta.Y;

                // Conform to minimum size here so we don't
                // go all weird when we snap it in the base conrt

                if (bounds.Height < min.Y)
                {
                    int diff = min.Y - bounds.Height;
                    bounds.Height += diff;
                    bounds.Y -= diff;
                }
            }

            if (0 != (m_ResizeDir & Pos.Right))
            {
                // This is complicated.
                // Basically we want to use the HoldPos, so it doesn't snap to the edge of the control
                // But we need to move the HoldPos with the window movement. Yikes.
                // I actually think this might be a big hack around the way this control works with regards
                // to the holdpos being on the parent panel.

                int woff = bounds.Width - m_HoldPos.X;
                int diff = bounds.Width;
                bounds.Width = pCursorPos.X + woff;
                if (bounds.Width < min.X) bounds.Width = min.X;
                diff -= bounds.Width;

                m_HoldPos.X -= diff;
            }

            if (0 != (m_ResizeDir & Pos.Bottom))
            {
                int hoff = bounds.Height - m_HoldPos.Y;
                int diff = bounds.Height;
                bounds.Height = pCursorPos.Y + hoff;
                if (bounds.Height < min.Y) bounds.Height = min.Y;
                diff -= bounds.Height;

                m_HoldPos.Y -= diff;
            }

            m_Target.SetBounds(bounds);

            if (Resized != null)
                Resized.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets the sizing direction.
        /// </summary>
        public Pos ResizeDir
        {
            set
            {
                m_ResizeDir = value;

                if ((0 != (value & Pos.Left) && 0 != (value & Pos.Top)) || (0 != (value & Pos.Right) && 0 != (value & Pos.Bottom)))
                {
                    Cursor = Cursors.SizeNWSE;
                    return;
                }
                if ((0 != (value & Pos.Right) && 0 != (value & Pos.Top)) || (0 != (value & Pos.Left) && 0 != (value & Pos.Bottom)))
                {
                    Cursor = Cursors.SizeNESW;
                    return;
                }
                if (0 != (value & Pos.Right) || 0 != (value & Pos.Left))
                {
                    Cursor = Cursors.SizeWE;
                    return;
                }
                if (0 != (value & Pos.Top) || 0 != (value & Pos.Bottom))
                {
                    Cursor = Cursors.SizeNS;
                    return;
                }
            }
        }
    }
}
