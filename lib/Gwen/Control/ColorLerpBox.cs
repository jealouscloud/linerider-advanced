using System;
using System.Drawing;
using Gwen.Input;

namespace Gwen.Controls
{
    /// <summary>
    /// Linear-interpolated HSV color box.
    /// </summary>
    public class ColorLerpBox : ControlBase
    {
        private Point m_CursorPos;
        private bool m_Depressed;
        private float m_Hue;
        private Texture m_Texture;

        /// <summary>
        /// Invoked when the selected color has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ColorChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorLerpBox"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public ColorLerpBox(ControlBase parent) : base(parent)
        {
            SetColor(Color.FromArgb(255, 255, 128, 0));
            SetSize(128, 128);
            MouseInputEnabled = true;
            m_Depressed = false;

            // texture is initialized in Render() if null
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (m_Texture != null)
                m_Texture.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Linear color interpolation.
        /// </summary>
        public static Color Lerp(Color toColor, Color fromColor, float amount)
        {
            Color delta = toColor.Subtract(fromColor);
            delta = delta.Multiply(amount);
            return fromColor.Add(delta);
        }

        /// <summary>
        /// Selected color.
        /// </summary>
        public Color SelectedColor
        {
            get { return GetColorAt(m_CursorPos.X, m_CursorPos.Y); }
        }

        /// <summary>
        /// Sets the selected color.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="onlyHue">Deetrmines whether to only set H value (not SV).</param>
        public void SetColor(Color value, bool onlyHue = true)
        {
            HSV hsv = value.ToHSV();
            m_Hue = hsv.h;

            if (!onlyHue)
            {
                m_CursorPos.X = (int)(hsv.s * Width);
                m_CursorPos.Y = (int)((1 - hsv.v) * Height);
            }
            Invalidate();

            if (ColorChanged != null)
				ColorChanged.Invoke(this, EventArgs.Empty);
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
            if (m_Depressed)
            {
                m_CursorPos = CanvasPosToLocal(new Point(x, y));
                //Do we have clamp?
                if (m_CursorPos.X < 0)
                    m_CursorPos.X = 0;
                if (m_CursorPos.X > Width)
                    m_CursorPos.X = Width;

                if (m_CursorPos.Y < 0)
                    m_CursorPos.Y = 0;
                if (m_CursorPos.Y > Height)
                    m_CursorPos.Y = Height;

                if (ColorChanged != null)
                    ColorChanged.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
			base.OnMouseClickedLeft(x, y, down);
            m_Depressed = down;
            if (down)
                InputHandler.MouseFocus = this;
            else
                InputHandler.MouseFocus = null;

            OnMouseMoved(x, y, 0, 0);
        }

        /// <summary>
        /// Gets the color from specified coordinates.
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Color value.</returns>
        private Color GetColorAt(int x, int y)
        {
            float xPercent = (x / (float)Width);
            float yPercent = 1 - (y / (float)Height);

            Color result = Util.HSVToColor(m_Hue, xPercent, yPercent);

            return result;
        }

        /// <summary>
        /// Invalidates the control.
        /// </summary>
        public override void Invalidate()
        {
            if (m_Texture != null)
            {
                m_Texture.Dispose();
                m_Texture = null;
            }
            base.Invalidate();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            if (m_Texture == null)
            {
                byte[] pixelData = new byte[Width*Height*4];

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Color c = GetColorAt(x, y);
                        pixelData[4*(x + y*Width)] = c.R;
                        pixelData[4*(x + y*Width) + 1] = c.G;
                        pixelData[4*(x + y*Width) + 2] = c.B;
                        pixelData[4*(x + y*Width) + 3] = c.A;
                    }
                }

                m_Texture = new Texture(skin.Renderer);
                m_Texture.Width = Width;
                m_Texture.Height = Height;
                m_Texture.LoadRaw(Width, Height, pixelData);
            }

            skin.Renderer.DrawColor = Color.White;
            skin.Renderer.DrawTexturedRect(m_Texture, RenderBounds);


            skin.Renderer.DrawColor = Color.Black;
            skin.Renderer.DrawLinedRect(RenderBounds);

            Color selected = SelectedColor;
            if ((selected.R + selected.G + selected.B)/3 < 170)
                skin.Renderer.DrawColor = Color.White;
            else
                skin.Renderer.DrawColor = Color.Black;

            Rectangle testRect = new Rectangle(m_CursorPos.X - 3, m_CursorPos.Y - 3, 6, 6);

            skin.Renderer.DrawShavedCornerRect(testRect);

            base.Render(skin);
        }
    }
}
