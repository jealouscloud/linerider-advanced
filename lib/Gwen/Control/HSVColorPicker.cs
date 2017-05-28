using System;
using System.Drawing;
using Gwen.ControlInternal;

namespace Gwen.Controls
{
    /// <summary>
    /// HSV color picker with "before" and "after" color boxes.
    /// </summary>
    public class HSVColorPicker : ControlBase, IColorPicker
    {
        private readonly ColorLerpBox m_LerpBox;
        private readonly ColorSlider m_ColorSlider;
        private readonly ColorDisplay m_Before;
        private readonly ColorDisplay m_After;

        /// <summary>
        /// Invoked when the selected color has changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ColorChanged;

        /// <summary>
        /// The "before" color.
        /// </summary>
        public Color DefaultColor { get { return m_Before.Color; } set { m_Before.Color = value; } }

        /// <summary>
        /// Selected color.
        /// </summary>
        public Color SelectedColor { get { return m_LerpBox.SelectedColor; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="HSVColorPicker"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public HSVColorPicker(ControlBase parent)
            : base(parent)
        {
            MouseInputEnabled = true;
            SetSize(256, 128);
            //ShouldCacheToTexture = true;

            m_LerpBox = new ColorLerpBox(this);
            m_LerpBox.ColorChanged += ColorBoxChanged;
            m_LerpBox.Dock = Pos.Left;

            m_ColorSlider = new ColorSlider(this);
            m_ColorSlider.SetPosition(m_LerpBox.Width + 15, 5);
            m_ColorSlider.ColorChanged += ColorSliderChanged;
            m_ColorSlider.Dock = Pos.Left;

            m_After = new ColorDisplay(this);
            m_After.SetSize(48, 24);
            m_After.SetPosition(m_ColorSlider.X + m_ColorSlider.Width + 15, 5);

            m_Before = new ColorDisplay(this);
            m_Before.SetSize(48, 24);
            m_Before.SetPosition(m_After.X, 28);

            int x = m_Before.X;
            int y = m_Before.Y + 30;

            {
                Label label = new Label(this);
                label.SetText("R:");
                label.SizeToContents();
                label.SetPosition(x, y);

                TextBoxNumeric numeric = new TextBoxNumeric(this);
                numeric.Name = "RedBox";
                numeric.SetPosition(x + 15, y - 1);
                numeric.SetSize(26, 16);
                numeric.SelectAllOnFocus = true;
                numeric.TextChanged += NumericTyped;
            }

            y += 20;

            {
                Label label = new Label(this);
                label.SetText("G:");
                label.SizeToContents();
                label.SetPosition(x, y);

                TextBoxNumeric numeric = new TextBoxNumeric(this);
                numeric.Name = "GreenBox";
                numeric.SetPosition(x + 15, y - 1);
                numeric.SetSize(26, 16);
                numeric.SelectAllOnFocus = true;
                numeric.TextChanged += NumericTyped;
            }

            y += 20;

            {
                Label label = new Label(this);
                label.SetText("B:");
                label.SizeToContents();
                label.SetPosition(x, y);

                TextBoxNumeric numeric = new TextBoxNumeric(this);
                numeric.Name = "BlueBox";
                numeric.SetPosition(x + 15, y - 1);
                numeric.SetSize(26, 16);
                numeric.SelectAllOnFocus = true;
                numeric.TextChanged += NumericTyped;
            }

            SetColor(DefaultColor);
        }

		private void NumericTyped(ControlBase control, EventArgs args)
        {
            TextBoxNumeric box = control as TextBoxNumeric;
            if (null == box) return;

            if (box.Text == String.Empty) return;

            int textValue = (int)box.Value;
            if (textValue < 0) textValue = 0;
            if (textValue > 255) textValue = 255;

            Color newColor = SelectedColor;

            if (box.Name.Contains("Red"))
            {
                newColor = Color.FromArgb(SelectedColor.A, textValue, SelectedColor.G, SelectedColor.B);
            }
            else if (box.Name.Contains("Green"))
            {
                newColor = Color.FromArgb(SelectedColor.A, SelectedColor.R, textValue, SelectedColor.B);
            }
            else if (box.Name.Contains("Blue"))
            {
                newColor = Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, textValue);
            }
            else if (box.Name.Contains("Alpha"))
            {
                newColor = Color.FromArgb(textValue, SelectedColor.R, SelectedColor.G, SelectedColor.B);
            }

            SetColor(newColor);
        }

        private void UpdateControls(Color color)
        {
            // [???] TODO: Make this code safer.
			// [halfofastaple] This code SHOULD (in theory) never crash/not work as intended, but referencing children by their name is unsafe.
            //		Instead, a direct reference to their objects should be maintained. Worst case scenario, we grab the wrong "RedBox".

            TextBoxNumeric redBox = FindChildByName("RedBox", false) as TextBoxNumeric;
            if (redBox != null)
                redBox.SetText(color.R.ToString(), false);

            TextBoxNumeric greenBox = FindChildByName("GreenBox", false) as TextBoxNumeric;
            if (greenBox != null)
                greenBox.SetText(color.G.ToString(), false);

            TextBoxNumeric blueBox = FindChildByName("BlueBox", false) as TextBoxNumeric;
            if (blueBox != null)
                blueBox.SetText(color.B.ToString(), false);

            m_After.Color = color;

            if (ColorChanged != null)
				ColorChanged.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the selected color.
        /// </summary>
        /// <param name="color">Color to set.</param>
        /// <param name="onlyHue">Determines whether only the hue should be set.</param>
        /// <param name="reset">Determines whether the "before" color should be set as well.</param>
        public void SetColor(Color color, bool onlyHue = false, bool reset = false)
        {
            UpdateControls(color);

            if (reset)
                m_Before.Color = color;

            m_ColorSlider.SelectedColor = color;
            m_LerpBox.SetColor(color, onlyHue);
            m_After.Color = color;
        }

		private void ColorBoxChanged(ControlBase control, EventArgs args)
        {
            UpdateControls(SelectedColor);
            Invalidate();
        }

		private void ColorSliderChanged(ControlBase control, EventArgs args)
        {
            if (m_LerpBox != null)
                m_LerpBox.SetColor(m_ColorSlider.SelectedColor, true);
            Invalidate();
        }
    }
}
