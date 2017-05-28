using System;
using System.Drawing;

namespace Gwen.Skin
{
    /// <summary>
    /// Simple skin (non-textured). Deprecated and incomplete, do not use.
    /// </summary>
    [Obsolete]
    public class Simple : Skin.SkinBase
    {
        private readonly Color m_colBorderColor;
        private readonly Color m_colControlOutlineLight;
        private readonly Color m_colControlOutlineLighter;
        private readonly Color m_colBGDark;
        private readonly Color m_colControl;
        private readonly Color m_colControlDarker;
        private readonly Color m_colControlOutlineNormal;
        private readonly Color m_colControlBright;
        private readonly Color m_colControlDark;
        private readonly Color m_colHighlightBG;
        private readonly Color m_colHighlightBorder;
        private readonly Color m_colToolTipBackground;
        private readonly Color m_colToolTipBorder;
        private readonly Color m_colModal;

        public Simple(Renderer.RendererBase renderer) : base(renderer)
        {
            m_colBorderColor = Color.FromArgb(255, 80, 80, 80);
            //m_colBG = Color.FromArgb(255, 248, 248, 248);
            m_colBGDark = Color.FromArgb(255, 235, 235, 235);

            m_colControl = Color.FromArgb(255, 240, 240, 240);
            m_colControlBright = Color.FromArgb(255, 255, 255, 255);
            m_colControlDark = Color.FromArgb(255, 214, 214, 214);
            m_colControlDarker = Color.FromArgb(255, 180, 180, 180);

            m_colControlOutlineNormal = Color.FromArgb(255, 112, 112, 112);
            m_colControlOutlineLight = Color.FromArgb(255, 144, 144, 144);
            m_colControlOutlineLighter = Color.FromArgb(255, 210, 210, 210);

            m_colHighlightBG = Color.FromArgb(255, 192, 221, 252);
            m_colHighlightBorder = Color.FromArgb(255, 51, 153, 255);

            m_colToolTipBackground = Color.FromArgb(255, 255, 255, 225);
            m_colToolTipBorder = Color.FromArgb(255, 0, 0, 0);

            m_colModal = Color.FromArgb(150, 25, 25, 25);
        }

        #region UI elements
        public override void DrawButton(Controls.ControlBase control, bool depressed, bool hovered, bool disabled)
        {
            int w = control.Width;
            int h = control.Height;

            DrawButton(w, h, depressed, hovered);
        }

        public override void DrawMenuItem(Controls.ControlBase control, bool submenuOpen, bool isChecked)
        {
            Rectangle rect = control.RenderBounds;
            if (submenuOpen || control.IsHovered)
            {
                m_Renderer.DrawColor = m_colHighlightBG;
                m_Renderer.DrawFilledRect(rect);

                m_Renderer.DrawColor = m_colHighlightBorder;
                m_Renderer.DrawLinedRect(rect);
            }

            if (isChecked)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);

                Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
                DrawCheck(r);
            }
        }

        public override void DrawMenuStrip(Controls.ControlBase control)
        {
            int w = control.Width;
            int h = control.Height;

            m_Renderer.DrawColor = Color.FromArgb(255, 246, 248, 252);
            m_Renderer.DrawFilledRect(new Rectangle(0, 0, w, h));

            m_Renderer.DrawColor = Color.FromArgb(150, 218, 224, 241);

            m_Renderer.DrawFilledRect(Util.FloatRect(0, h * 0.4f, w, h * 0.6f));
            m_Renderer.DrawFilledRect(Util.FloatRect(0, h * 0.5f, w, h * 0.5f));
        }

        public override void DrawMenu(Controls.ControlBase control, bool paddingDisabled)
        {
            int w = control.Width;
            int h = control.Height;

            m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawFilledRect(new Rectangle(0, 0, w, h));

            if (!paddingDisabled)
            {
                m_Renderer.DrawColor = m_colControl;
                m_Renderer.DrawFilledRect(new Rectangle(1, 0, 22, h));
            }

            m_Renderer.DrawColor = m_colControlOutlineNormal;
            m_Renderer.DrawLinedRect(new Rectangle(0, 0, w, h));
        }

        public override void DrawShadow(Controls.ControlBase control)
        {
            int w = control.Width;
            int h = control.Height;

            int x = 4;
            int y = 6;

            m_Renderer.DrawColor = Color.FromArgb(10, 0, 0, 0);

            m_Renderer.DrawFilledRect(new Rectangle(x, y, w, h));
            x += 2;
            m_Renderer.DrawFilledRect(new Rectangle(x, y, w, h));
            y += 2;
            m_Renderer.DrawFilledRect(new Rectangle(x, y, w, h));
        }

        public virtual void DrawButton(int w, int h, bool depressed, bool bHovered, bool bSquared = false)
        {
            if (depressed) m_Renderer.DrawColor = m_colControlDark;
            else if (bHovered) m_Renderer.DrawColor = m_colControlBright;
            else m_Renderer.DrawColor = m_colControl;

            m_Renderer.DrawFilledRect(new Rectangle(1, 1, w - 2, h - 2));
            
            if (depressed) m_Renderer.DrawColor = m_colControlDark;
            else if (bHovered) m_Renderer.DrawColor = m_colControl;
            else m_Renderer.DrawColor = m_colControlDark;

            m_Renderer.DrawFilledRect(Util.FloatRect(1, h * 0.5f, w - 2, h * 0.5f - 2));

            if (!depressed)
            {
                m_Renderer.DrawColor = m_colControlBright;
            }
            else
            {
                m_Renderer.DrawColor = m_colControlDarker;
            }
            m_Renderer.DrawShavedCornerRect(new Rectangle(1, 1, w - 2, h - 2), bSquared);

            // Border
            m_Renderer.DrawColor = m_colControlOutlineNormal;
            m_Renderer.DrawShavedCornerRect(new Rectangle(0, 0, w, h), bSquared);
        }

        public override void DrawRadioButton(Controls.ControlBase control, bool selected, bool depressed)
        {
            Rectangle rect = control.RenderBounds;

            // Inside colour
            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 220, 242, 254);
            else m_Renderer.DrawColor = m_colControlBright;

            m_Renderer.DrawFilledRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 2));

            // Border
            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 85, 130, 164);
            else m_Renderer.DrawColor = m_colControlOutlineLight;

            m_Renderer.DrawShavedCornerRect(rect);

            m_Renderer.DrawColor = Color.FromArgb(15, 0, 50, 60);
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4));
            m_Renderer.DrawFilledRect(Util.FloatRect(rect.X + 2, rect.Y + 2, rect.Width * 0.3f, rect.Height - 4));
            m_Renderer.DrawFilledRect(Util.FloatRect(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height * 0.3f));

            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 121, 198, 249);
            else m_Renderer.DrawColor = Color.FromArgb(50, 0, 50, 60);

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 3, 1, rect.Height - 5));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 3, rect.Y + 2, rect.Width - 5, 1));


            if (selected)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 40, 230, 30);
                m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4));
            }
        }

        public override void DrawCheckBox(Controls.ControlBase control, bool selected, bool depressed)
        {
            Rectangle rect = control.RenderBounds;

            // Inside colour
            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 220, 242, 254);
            else m_Renderer.DrawColor = m_colControlBright;

            m_Renderer.DrawFilledRect(rect);

            // Border
            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 85, 130, 164);
            else m_Renderer.DrawColor = m_colControlOutlineLight;

            m_Renderer.DrawLinedRect(rect);

            m_Renderer.DrawColor = Color.FromArgb(15, 0, 50, 60);
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4));
            m_Renderer.DrawFilledRect(Util.FloatRect(rect.X + 2, rect.Y + 2, rect.Width * 0.3f, rect.Height - 4));
            m_Renderer.DrawFilledRect(Util.FloatRect(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height * 0.3f));

            if (control.IsHovered) m_Renderer.DrawColor = Color.FromArgb(255, 121, 198, 249);
            else m_Renderer.DrawColor = Color.FromArgb(50, 0, 50, 60);

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 2, 1, rect.Height - 4));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, 1));

            if (depressed)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 100, 100, 100);
                Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
                DrawCheck(r);
            }
            else if (selected)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
                Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
                DrawCheck(r);
            }
        }

        public override void DrawGroupBox(Controls.ControlBase control, int textStart, int textHeight, int textWidth)
        {
            Rectangle rect = control.RenderBounds;

            rect.Y += (int)(textHeight * 0.5f);
            rect.Height -= (int)(textHeight * 0.5f);

            Color m_colDarker = Color.FromArgb(50, 0, 50, 60);
            Color m_colLighter = Color.FromArgb(150, 255, 255, 255);

            m_Renderer.DrawColor = m_colLighter;

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + 1, textStart - 3, 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1 + textStart + textWidth, rect.Y + 1,
                                                  rect.Width - textStart + textWidth - 2, 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, (rect.Y + rect.Height) - 1, rect.Width - 2, 1));

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height));
            m_Renderer.DrawFilledRect(new Rectangle((rect.X + rect.Width) - 2, rect.Y + 1, 1, rect.Height - 1));

            m_Renderer.DrawColor = m_colDarker;

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y, textStart - 3, 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1 + textStart + textWidth, rect.Y,
                                                  rect.Width - textStart - textWidth - 2, 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, (rect.Y + rect.Height) - 1, rect.Width - 2, 1));

            m_Renderer.DrawFilledRect(new Rectangle(rect.X, rect.Y + 1, 1, rect.Height - 1));
            m_Renderer.DrawFilledRect(new Rectangle((rect.X + rect.Width) - 1, rect.Y + 1, 1, rect.Height - 1));
        }

        public override void DrawTextBox(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;
            bool bHasFocus = control.HasFocus;

            // Box inside
            m_Renderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
            m_Renderer.DrawFilledRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 2));

            m_Renderer.DrawColor = m_colControlOutlineLight;
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y, rect.Width - 2, 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X, rect.Y + 1, 1, rect.Height - 2));

            m_Renderer.DrawColor = m_colControlOutlineLighter;
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, (rect.Y + rect.Height) - 1, rect.Width - 2, 1));
            m_Renderer.DrawFilledRect(new Rectangle((rect.X + rect.Width) - 1, rect.Y + 1, 1, rect.Height - 2));

            if (bHasFocus)
            {
                m_Renderer.DrawColor = Color.FromArgb(150, 50, 200, 255);
                m_Renderer.DrawLinedRect(rect);
            }
        }

        public override void DrawTabButton(Controls.ControlBase control, bool active, Pos dir)
        {
            Rectangle rect = control.RenderBounds;
            bool bHovered = control.IsHovered;

            if (bHovered) m_Renderer.DrawColor = m_colControlBright;
            else m_Renderer.DrawColor = m_colControl;

            m_Renderer.DrawFilledRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 1));

            if (bHovered) m_Renderer.DrawColor = m_colControl;
            else m_Renderer.DrawColor = m_colControlDark;

            m_Renderer.DrawFilledRect(Util.FloatRect(1, rect.Height*0.5f, rect.Width - 2, rect.Height*0.5f - 1));

            m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawShavedCornerRect(new Rectangle(1, 1, rect.Width - 2, rect.Height));

            m_Renderer.DrawColor = m_colBorderColor;

            m_Renderer.DrawShavedCornerRect(new Rectangle(0, 0, rect.Width, rect.Height));
        }

        public override void DrawTabControl(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;

            m_Renderer.DrawColor = m_colControl;
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawLinedRect(rect);

            //m_Renderer.DrawColor = m_colControl;
            //m_Renderer.DrawFilledRect(CurrentButtonRect);
        }

        public override void DrawWindow(Controls.ControlBase control, int topHeight, bool inFocus)
        {
            Rectangle rect = control.RenderBounds;

            // Titlebar
            if (inFocus)
                m_Renderer.DrawColor = Color.FromArgb(230, 87, 164, 232);
            else
                m_Renderer.DrawColor = Color.FromArgb(230, (int)(87 * 0.70f), (int)(164 * 0.70f),
                                                    (int)(232 * 0.70f));

            int iBorderSize = 5;
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, topHeight - 1));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + topHeight - 1, iBorderSize,
                                                  rect.Height - 2 - topHeight));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + rect.Width - iBorderSize, rect.Y + topHeight - 1, iBorderSize,
                                                  rect.Height - 2 - topHeight));
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + rect.Height - iBorderSize, rect.Width - 2,
                                                  iBorderSize));

            // Main inner
            m_Renderer.DrawColor = Color.FromArgb(230, m_colControlDark.R, m_colControlDark.G, m_colControlDark.B);
            m_Renderer.DrawFilledRect(new Rectangle(rect.X + iBorderSize + 1, rect.Y + topHeight,
                                                  rect.Width - iBorderSize * 2 - 2,
                                                  rect.Height - topHeight - iBorderSize - 1));

            // Light inner border
            m_Renderer.DrawColor = Color.FromArgb(100, 255, 255, 255);
            m_Renderer.DrawShavedCornerRect(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2));

            // Dark line between titlebar and main
            m_Renderer.DrawColor = m_colBorderColor;

            // Inside border
            m_Renderer.DrawColor = m_colControlOutlineNormal;
            m_Renderer.DrawLinedRect(new Rectangle(rect.X + iBorderSize, rect.Y + topHeight - 1, rect.Width - 10,
                                                 rect.Height - topHeight - (iBorderSize - 1)));

            // Dark outer border
            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawShavedCornerRect(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public override void DrawWindowCloseButton(Controls.ControlBase control, bool depressed, bool hovered, bool disabled)
        {
            // TODO
            DrawButton(control, depressed, hovered, disabled);
        }

        public override void DrawHighlight(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;
            m_Renderer.DrawColor = Color.FromArgb(255, 255, 100, 255);
            m_Renderer.DrawFilledRect(rect);
        }

        public override void DrawScrollBar(Controls.ControlBase control, bool horizontal, bool depressed)
        {
            Rectangle rect = control.RenderBounds;
            if (depressed)
                m_Renderer.DrawColor = m_colControlDarker;
            else
                m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawFilledRect(rect);
        }

        public override void DrawScrollBarBar(Controls.ControlBase control, bool depressed, bool hovered, bool horizontal)
        {
            //TODO: something specialized
            DrawButton(control, depressed, hovered, false);
        }

        public override void DrawTabTitleBar(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;

            m_Renderer.DrawColor = Color.FromArgb(255, 177, 193, 214);
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;
            rect.Height += 1;
            m_Renderer.DrawLinedRect(rect);
        }

        public override void DrawProgressBar(Controls.ControlBase control, bool horizontal, float progress)
        {
            Rectangle rect = control.RenderBounds;
            Color FillColour = Color.FromArgb(255, 0, 211, 40);

            if (horizontal)
            {
                //Background
                m_Renderer.DrawColor = m_colControlDark;
                m_Renderer.DrawFilledRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 2));

                //Right half
                m_Renderer.DrawColor = FillColour;
                m_Renderer.DrawFilledRect(Util.FloatRect(1, 1, rect.Width * progress - 2, rect.Height - 2));

                m_Renderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
                m_Renderer.DrawFilledRect(Util.FloatRect(1, 1, rect.Width - 2, rect.Height * 0.45f));
            }
            else
            {
                //Background 
                m_Renderer.DrawColor = m_colControlDark;
                m_Renderer.DrawFilledRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 2));

                //Top half
                m_Renderer.DrawColor = FillColour;
                m_Renderer.DrawFilledRect(Util.FloatRect(1, 1 + (rect.Height * (1 - progress)), rect.Width - 2,
                                                         rect.Height * progress - 2));

                m_Renderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
                m_Renderer.DrawFilledRect(Util.FloatRect(1, 1, rect.Width * 0.45f, rect.Height - 2));
            }

            m_Renderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
            m_Renderer.DrawShavedCornerRect(new Rectangle(1, 1, rect.Width - 2, rect.Height - 2));

            m_Renderer.DrawColor = Color.FromArgb(70, 255, 255, 255);
            m_Renderer.DrawShavedCornerRect(new Rectangle(2, 2, rect.Width - 4, rect.Height - 4));

            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawShavedCornerRect(rect);
        }

        public override void DrawListBox(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;

            m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawLinedRect(rect);
        }

        public override void DrawListBoxLine(Controls.ControlBase control, bool selected, bool even)
        {
            Rectangle rect = control.RenderBounds;

            if (selected)
            {
                m_Renderer.DrawColor = m_colHighlightBorder;
                m_Renderer.DrawFilledRect(rect);
            }
            else if (control.IsHovered)
            {
                m_Renderer.DrawColor = m_colHighlightBG;
                m_Renderer.DrawFilledRect(rect);
            }
        }

        public override void DrawSlider(Controls.ControlBase control, bool horizontal, int numNotches, int barSize)
        {
            Rectangle rect = control.RenderBounds;
            Rectangle notchRect = rect;

            if (horizontal)
            {
                rect.Y += (int)(rect.Height * 0.4f);
                rect.Height -= (int)(rect.Height * 0.8f);
            }
            else
            {
                rect.X += (int)(rect.Width * 0.4f);
                rect.Width -= (int)(rect.Width * 0.8f);
            }

            m_Renderer.DrawColor = m_colBGDark;
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colControlDarker;
            m_Renderer.DrawLinedRect(rect);
        }

        public override void DrawComboBox(Controls.ControlBase control, bool down, bool open)
        {
            DrawTextBox(control);
        }

        public override void DrawKeyboardHighlight(Controls.ControlBase control, Rectangle r, int iOffset)
        {
            Rectangle rect = r;

            rect.X += iOffset;
            rect.Y += iOffset;
            rect.Width -= iOffset * 2;
            rect.Height -= iOffset * 2;

            //draw the top and bottom
            bool skip = true;
            for (int i = 0; i < rect.Width * 0.5; i++)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
                if (!skip)
                {
                    m_Renderer.DrawPixel(rect.X + (i * 2), rect.Y);
                    m_Renderer.DrawPixel(rect.X + (i * 2), rect.Y + rect.Height - 1);
                }
                else
                    skip = false;
            }

            for (int i = 0; i < rect.Height * 0.5; i++)
            {
                m_Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
                m_Renderer.DrawPixel(rect.X, rect.Y + i * 2);
                m_Renderer.DrawPixel(rect.X + rect.Width - 1, rect.Y + i * 2);
            }
        }

        public override void DrawToolTip(Controls.ControlBase control)
        {
            Rectangle rct = control.RenderBounds;
            rct.X -= 3;
            rct.Y -= 3;
            rct.Width += 6;
            rct.Height += 6;

            m_Renderer.DrawColor = m_colToolTipBackground;
            m_Renderer.DrawFilledRect(rct);

            m_Renderer.DrawColor = m_colToolTipBorder;
            m_Renderer.DrawLinedRect(rct);
        }

        public override void DrawScrollButton(Controls.ControlBase control, Pos direction, bool depressed, bool hovered, bool disabled)
        {
            DrawButton(control, depressed, false, false);

            m_Renderer.DrawColor = Color.FromArgb(240, 0, 0, 0);

            Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);

            if (direction == Pos.Top) DrawArrowUp(r);
            else if (direction == Pos.Bottom) DrawArrowDown(r);
            else if (direction == Pos.Left) DrawArrowLeft(r);
            else DrawArrowRight(r);
        }

        public override void DrawComboBoxArrow(Controls.ControlBase control, bool hovered, bool depressed, bool open, bool disabled)
        {
            //DrawButton( control.Width, control.Height, depressed, false, true );

            m_Renderer.DrawColor = Color.FromArgb(240, 0, 0, 0);

            Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
            DrawArrowDown(r);
        }

        public override void DrawNumericUpDownButton(Controls.ControlBase control, bool depressed, bool up)
        {
            //DrawButton( control.Width, control.Height, depressed, false, true );

            m_Renderer.DrawColor = Color.FromArgb(240, 0, 0, 0);

            Rectangle r = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);

            if (up) DrawArrowUp(r);
            else DrawArrowDown(r);
        }

        public override void DrawTreeButton(Controls.ControlBase control, bool open)
        {
            Rectangle rect = control.RenderBounds;
            rect.X += 2;
            rect.Y += 2;
            rect.Width -= 4;
            rect.Height -= 4;

            m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawLinedRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;

            if (!open) // ! because the button shows intention, not the current state
                m_Renderer.DrawFilledRect(new Rectangle(rect.X + rect.Width / 2, rect.Y + 2, 1, rect.Height - 4));

            m_Renderer.DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + rect.Height / 2, rect.Width - 4, 1));
        }

        public override void DrawTreeControl(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;

            m_Renderer.DrawColor = m_colControlBright;
            m_Renderer.DrawFilledRect(rect);

            m_Renderer.DrawColor = m_colBorderColor;
            m_Renderer.DrawLinedRect(rect);
        }

        public override void DrawTreeNode(Controls.ControlBase ctrl, bool open, bool selected, int labelHeight, int labelWidth, int halfWay, int lastBranch, bool isRoot)
        {
            if (selected)
            {
                Renderer.DrawColor = Color.FromArgb(100, 0, 150, 255);
                Renderer.DrawFilledRect(new Rectangle(17, 0, labelWidth + 2, labelHeight - 1));
                Renderer.DrawColor = Color.FromArgb(200, 0, 150, 255);
                Renderer.DrawLinedRect(new Rectangle(17, 0, labelWidth + 2, labelHeight - 1));
            }

            base.DrawTreeNode(ctrl, open, selected, labelHeight, labelWidth, halfWay, lastBranch, isRoot);
        }

        public override void DrawStatusBar(Controls.ControlBase control)
        {
            // todo
        }

        public override void DrawColorDisplay(Controls.ControlBase control, Color color)
        {
            Rectangle rect = control.RenderBounds;

            if (color.A != 255)
            {
                Renderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
                Renderer.DrawFilledRect(rect);

                Renderer.DrawColor = Color.FromArgb(128, 128, 128, 128);

                Renderer.DrawFilledRect(Util.FloatRect(0, 0, rect.Width * 0.5f, rect.Height * 0.5f));
                Renderer.DrawFilledRect(Util.FloatRect(rect.Width * 0.5f, rect.Height * 0.5f, rect.Width * 0.5f,
                                                         rect.Height * 0.5f));
            }

            Renderer.DrawColor = color;
            Renderer.DrawFilledRect(rect);

            Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
            Renderer.DrawLinedRect(rect);
        }

        public override void DrawModalControl(Controls.ControlBase control)
        {
            if (control.ShouldDrawBackground)
            {
                Rectangle rect = control.RenderBounds;
                Renderer.DrawColor = m_colModal;
                Renderer.DrawFilledRect(rect);
            }
        }

        public override void DrawMenuDivider(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;
            Renderer.DrawColor = m_colBGDark;
            Renderer.DrawFilledRect(rect);
            Renderer.DrawColor = m_colControlDarker;
            Renderer.DrawLinedRect(rect);
        }

        public override void DrawMenuRightArrow(Controls.ControlBase control)
        {
            DrawArrowRight(control.RenderBounds);
        }

        public override void DrawSliderButton(Controls.ControlBase control, bool depressed, bool horizontal)
        {
            DrawButton(control, depressed, control.IsHovered, control.IsDisabled);
        }

        public override void DrawCategoryHolder(Controls.ControlBase control)
        {
            // todo
        }

        public override void DrawCategoryInner(Controls.ControlBase control, bool collapsed)
        {
            // todo
        }
        #endregion
    }
}
