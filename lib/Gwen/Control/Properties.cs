using Gwen.ControlInternal;
using System;
using System.Windows.Forms;

namespace Gwen.Controls
{
    /// <summary>
    /// Properties table.
    /// </summary>
    public class Properties : ControlBase
    {
        #region Events

        /// <summary>
        /// Invoked when a property value has been changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> ValueChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Returns the width of the first column (property names).
        /// </summary>
        public int SplitWidth
        {
            get
            {
                return m_SplitterBar.X;
            }
            set
            {
                m_SplitterBar.X = value;
            }
        }

        #endregion Properties

        #region Constructors

        // todo: rename?
        /// <summary>
        /// Initializes a new instance of the <see cref="Properties"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Properties(ControlBase parent)
            : base(parent)
        {
            m_SplitterBar = new SplitterBar(this);
            m_SplitterBar.SetPosition(80, 0);
            m_SplitterBar.Cursor = Cursors.SizeWE;
            m_SplitterBar.Dragged += OnSplitterMoved;
            m_SplitterBar.ShouldDrawBackground = false;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Adds a new text property row.
        /// </summary>
        /// <param name="label">Property name.</param>
        /// <param name="value">Initial value.</param>
        /// <returns>Newly created row.</returns>
        public PropertyRow Add(string label, string value = "")
        {
            return Add(label, new Property.Text(this), value);
        }

        /// <summary>
        /// Adds a new property row.
        /// </summary>
        /// <param name="label">Property name.</param>
        /// <param name="prop">Property control.</param>
        /// <param name="value">Initial value.</param>
        /// <returns>Newly created row.</returns>
        public PropertyRow Add(string label, Property.PropertyBase prop, string value = "")
        {
            PropertyRow row = new PropertyRow(this, prop);
            row.Dock = Pos.Top;
            row.Label = label;
            row.ValueChanged += OnRowValueChanged;

            prop.SetValue(value, true);

            m_SplitterBar.BringToFront();
            return row;
        }
        /// <summary>
        /// Adds a new property row.
        /// </summary>
        /// <param name="label">Property name.</param>
        /// <param name="prop">Property control.</param>
        /// <returns>Newly created row.</returns>
        public PropertyRow Add(string label, Property.KeyProperty prop)
        {
            PropertyRow row = new PropertyRow(this, prop);
            row.Dock = Pos.Top;
            row.Label = label;
            row.ValueChanged += OnRowValueChanged;

            m_SplitterBar.BringToFront();
            return row;
        }


        /// <summary>
        /// Deletes all rows.
        /// </summary>
        public void DeleteAll()
        {
            m_InnerPanel.DeleteAllChildren();
        }

        /// <summary>
        /// Handles the splitter moved event.
        /// </summary>
        /// <param name="control">Event source.</param>
        protected virtual void OnSplitterMoved(ControlBase control, EventArgs args)
        {
            InvalidateChildren();
        }

        /// <summary>
        /// Function invoked after layout.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void PostLayout(Skin.SkinBase skin)
        {
            m_SplitterBar.Height = 0;

            if (SizeToChildren(false, true))
            {
                InvalidateParent();
            }

            m_SplitterBar.SetSize(3, Height);
        }

        #endregion Methods

        #region Fields

        private readonly SplitterBar m_SplitterBar;

        #endregion Fields

        private void OnRowValueChanged(ControlBase control, EventArgs args)
        {
            if (ValueChanged != null)
                ValueChanged.Invoke(control, EventArgs.Empty);
        }
    }
}