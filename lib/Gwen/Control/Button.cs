using Gwen.Input;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Gwen.Controls
{
    /// <summary>
    /// Button control.
    /// </summary>
    public class Button : Label
    {
        #region Events

        /// <summary>
        /// Invoked when the button is pressed.
        /// </summary>
        public event GwenEventHandler<EventArgs> Pressed;

        /// <summary>
        /// Invoked when the button is released.
        /// </summary>
        public event GwenEventHandler<EventArgs> Released;

        /// <summary>
        /// Invoked when the button's toggle state has changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> Toggled;

        /// <summary>
        /// Invoked when the button's toggle state has changed to Off.
        /// </summary>
        public event GwenEventHandler<EventArgs> ToggledOff;

        /// <summary>
        /// Invoked when the button's toggle state has changed to On.
        /// </summary>
        public event GwenEventHandler<EventArgs> ToggledOn;

        #endregion Events

        #region Properties

        /// <summary>
        /// Indicates whether the button is depressed.
        /// </summary>
        public bool IsDepressed
        {
            get { return m_Depressed; }
            set
            {
                if (m_Depressed == value)
                    return;
                m_Depressed = value;
                Redraw();
            }
        }

        /// <summary>
        /// Indicates whether the button is toggleable.
        /// </summary>
        public bool IsToggle { get { return m_Toggle; } set { m_Toggle = value; } }

        /// <summary>
        /// Determines the button's toggle state.
        /// </summary>
        public bool ToggleState
        {
            get { return m_ToggleStatus; }
            set
            {
                if (!m_Toggle) return;
                if (m_ToggleStatus == value) return;

                m_ToggleStatus = value;

                if (Toggled != null)
                    Toggled.Invoke(this, EventArgs.Empty);

                if (m_ToggleStatus)
                {
                    if (ToggledOn != null)
                        ToggledOn.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (ToggledOff != null)
                        ToggledOff.Invoke(this, EventArgs.Empty);
                }

                Redraw();
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Control constructor.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Button(ControlBase parent)
            : base(parent)
        {
            AutoSizeToContents = false;
            SetSize(100, 20);
            MouseInputEnabled = true;
            Alignment = Pos.Center;
            TextPadding = new Padding(3, 3, 3, 3);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// "Clicks" the button.
        /// </summary>
        public virtual void Press(ControlBase control = null)
        {
            OnClicked(0, 0);
        }

        /// <summary>
        /// Sets the button's image.
        /// </summary>
        /// <param name="textureName">Texture name. Null to remove.</param>
        /// <param name="center">Determines whether the image should be centered.</param>
        public virtual void SetImage(string textureName, bool center = false)
        {
            if (String.IsNullOrEmpty(textureName))
            {
                if (m_Image != null)
                    m_Image.Dispose();
                m_Image = null;
                return;
            }

            if (m_Image == null)
            {
                m_Image = new ImagePanel(this);
            }

            m_Image.ImageName = textureName;
            m_Image.SizeToContents();
            m_Image.SetPosition(Math.Max(Padding.Left, 2), 2);
            m_CenterImage = center;

            TextPadding = new Padding(m_Image.Right + 2, TextPadding.Top, TextPadding.Right, TextPadding.Bottom);
        }

        /// <summary>
        /// Sizes to contents.
        /// </summary>
        public override void SizeToContents()
        {
            base.SizeToContents();
            if (m_Image != null)
            {
                int height = m_Image.Height + 4;
                if (Height < height)
                {
                    Height = height;
                }
            }
        }

        /// <summary>
        /// Toggles the button.
        /// </summary>
        public virtual void Toggle()
        {
            ToggleState = !ToggleState;
        }

        /// <summary>
        /// Updates control colors.
        /// </summary>
        public override void UpdateColors()
        {
            if (IsDisabled)
            {
                TextColor = Skin.Colors.Button.Disabled;
                return;
            }

            if (IsDepressed || ToggleState)
            {
                TextColor = Skin.Colors.Button.Down;
                return;
            }

            if (IsHovered)
            {
                TextColor = Skin.Colors.Button.Hover;
                return;
            }

            TextColor = Skin.Colors.Button.Normal;
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            base.Layout(skin);
            if (m_Image != null)
            {
                Align.CenterVertically(m_Image);

                if (m_CenterImage)
                    Align.CenterHorizontally(m_Image);
            }
        }

        /// <summary>
        /// Default accelerator handler.
        /// </summary>
        protected override void OnAccelerator()
        {
            OnClicked(0, 0);
        }

        /// <summary>
        /// Internal OnPressed implementation.
        /// </summary>
        protected virtual void OnClicked(int x, int y)
        {
            if (IsToggle)
            {
                Toggle();
            }

            base.OnMouseClickedLeft(x, y, true);
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
            return base.OnKeySpace(down);
            //if (down)
            //    OnClicked(0, 0);
            //return true;
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
            //base.OnMouseClickedLeft(x, y, down);
            if (down)
            {
                IsDepressed = true;
                InputHandler.MouseFocus = this;
                if (Pressed != null)
                    Pressed.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (this.GetCanvas().GetControlAt(x, y) == this && IsHovered && m_Depressed)
                {
                    OnClicked(x, y);
                }

                IsDepressed = false;
                InputHandler.MouseFocus = null;
                if (Released != null)
                    Released.Invoke(this, EventArgs.Empty);
            }

            Redraw();
        }

        /// <summary>
        /// Handler invoked on mouse double click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        protected override void OnMouseDoubleClickedLeft(int x, int y)
        {
            base.OnMouseDoubleClickedLeft(x, y);
            OnMouseClickedLeft(x, y, true);
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            base.Render(skin);

            if (ShouldDrawBackground)
            {
                bool drawDepressed = IsDepressed && IsHovered;
                if (IsToggle)
                    drawDepressed = drawDepressed || ToggleState;

                bool bDrawHovered = IsHovered && ShouldDrawHover;

                skin.DrawButton(this, drawDepressed, bDrawHovered, IsDisabled);
            }
        }

        #endregion Methods

        #region Fields

        private bool m_CenterImage;
        private bool m_Depressed;
        private ImagePanel m_Image;
        private bool m_Toggle;
        private bool m_ToggleStatus;

        #endregion Fields
    }
}