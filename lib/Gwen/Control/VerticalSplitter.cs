using System;
using System.Windows.Forms;
using Gwen.ControlInternal;

namespace Gwen.Controls
{
    public class VerticalSplitter : ControlBase
    {
        private readonly SplitterBar m_HSplitter;
        private readonly ControlBase[] m_Sections;

        private float m_HVal; // 0-1
        private int m_BarSize; // pixels
        private int m_ZoomedSection; // 0-3

        /// <summary>
        /// Invoked when one of the panels has been zoomed (maximized).
        /// </summary>
		public event GwenEventHandler<EventArgs> PanelZoomed;

        /// <summary>
        /// Invoked when one of the panels has been unzoomed (restored).
        /// </summary>
		public event GwenEventHandler<EventArgs> PanelUnZoomed;

        /// <summary>
        /// Invoked when the zoomed panel has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ZoomChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSplitter"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public VerticalSplitter(ControlBase parent)
            : base(parent)
        {
            m_Sections = new ControlBase[2];

            m_HSplitter = new SplitterBar(this);
            m_HSplitter.SetPosition(128, 0);
            m_HSplitter.Dragged += OnHorizontalMoved;
            m_HSplitter.Cursor = Cursors.SizeWE;

            m_HVal = 0.5f;

            SetPanel(0, null);
            SetPanel(1, null);

            SplitterSize = 5;
            SplittersVisible = false;

            m_ZoomedSection = -1;
        }

        /// <summary>
        /// Centers the panels so that they take even amount of space.
        /// </summary>
        public void CenterPanels()
        {
            m_HVal = 0.5f;
            Invalidate();
        }

        public void SetHValue(float value)
        {
            if (value <= 1f || value >= 0)
                m_HVal = value;
        }

        /// <summary>
        /// Indicates whether any of the panels is zoomed.
        /// </summary>
        public bool IsZoomed { get { return m_ZoomedSection != -1; } }

        /// <summary>
        /// Gets or sets a value indicating whether splitters should be visible.
        /// </summary>
        public bool SplittersVisible
        {
            get { return m_HSplitter.ShouldDrawBackground; }
            set
            {
                m_HSplitter.ShouldDrawBackground = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the splitter.
        /// </summary>
        public int SplitterSize { get { return m_BarSize; } set { m_BarSize = value; } }

        private void UpdateHSplitter()
        {
            m_HSplitter.MoveTo((Width - m_HSplitter.Width) * (m_HVal), m_HSplitter.Y);
        }

		protected void OnHorizontalMoved(ControlBase control, EventArgs args)
        {
            m_HVal = CalculateValueHorizontal();
            Invalidate();
        }

        private float CalculateValueHorizontal()
        {
            return m_HSplitter.X / (float)(Width - m_HSplitter.Width);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            m_HSplitter.SetSize(m_BarSize, Height);
            
            UpdateHSplitter();
            
            if (m_ZoomedSection == -1)
            {
                if (m_Sections[0] != null)
                    m_Sections[0].SetBounds(0, 0, m_HSplitter.X, Height);

                if (m_Sections[1] != null)
                    m_Sections[1].SetBounds(m_HSplitter.X + m_BarSize, 0, Width - (m_HSplitter.X + m_BarSize), Height);
            }
            else
            {
                //This should probably use Fill docking instead
                m_Sections[m_ZoomedSection].SetBounds(0, 0, Width, Height);
            }
        }

        /// <summary>
        /// Assigns a control to the specific inner section.
        /// </summary>
        /// <param name="index">Section index (0-3).</param>
        /// <param name="panel">Control to assign.</param>
        public void SetPanel(int index, ControlBase panel)
        {
            m_Sections[index] = panel;

            if (panel != null)
            {
                panel.Dock = Pos.None;
                panel.Parent = this;
            }
            
            Invalidate();
        }
        
        /// <summary>
        /// Gets the specific inner section.
        /// </summary>
        /// <param name="index">Section index (0-3).</param>
        /// <returns>Specified section.</returns>
        public ControlBase GetPanel(int index)
        {
            return m_Sections[index];
        }

        /// <summary>
        /// Internal handler for the zoom changed event.
        /// </summary>
        protected void OnZoomChanged()
        {
            if (ZoomChanged != null)
				ZoomChanged.Invoke(this, EventArgs.Empty);

            if (m_ZoomedSection == -1)
            {
                if (PanelUnZoomed != null)
					PanelUnZoomed.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (PanelZoomed != null)
					PanelZoomed.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Maximizes the specified panel so it fills the entire control.
        /// </summary>
        /// <param name="section">Panel index (0-3).</param>
        public void Zoom(int section)
        {
            UnZoom();

            if (m_Sections[section] != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (i != section && m_Sections[i] != null)
                        m_Sections[i].IsHidden = true;
                }
                m_ZoomedSection = section;

                Invalidate();
            }
            OnZoomChanged();
        }

        /// <summary>
        /// Restores the control so all panels are visible.
        /// </summary>
        public void UnZoom()
        {
            m_ZoomedSection = -1;
            
            for (int i = 0; i < 2; i++)
            {
                if (m_Sections[i] != null)
                    m_Sections[i].IsHidden = false;
            }
            
            Invalidate();
            OnZoomChanged();
        }
    }
}
