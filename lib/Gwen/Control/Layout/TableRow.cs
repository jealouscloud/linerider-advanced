using System;
using System.Drawing;

namespace Gwen.Controls.Layout
{
    /// <summary>
    /// Single table row.
    /// </summary>
    public class TableRow : ControlBase
    {
        // [omeg] todo: get rid of this
        public const int MaxColumns = 5;

        private int m_ColumnCount;
        private bool m_EvenRow;
        private readonly Label[] m_Columns;

        internal Label GetColumn(int index)
        {
            return m_Columns[index];
        }

        /// <summary>
        /// Invoked when the row has been selected.
        /// </summary>
        public event GwenEventHandler<ItemSelectedEventArgs> Selected;

        /// <summary>
        /// Column count.
        /// </summary>
        public int ColumnCount { get { return m_ColumnCount; } set { SetColumnCount(value); } }

        /// <summary>
        /// Indicates whether the row is even or odd (used for alternate coloring).
        /// </summary>
        public bool EvenRow { get { return m_EvenRow; } set { m_EvenRow = value; } }

        /// <summary>
        /// Text of the first column.
        /// </summary>
        public string Text { get { return GetText(0); } set { SetCellText(0, value); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableRow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public TableRow(ControlBase parent)
            : base(parent)
        {
            m_Columns = new Label[MaxColumns];
            m_ColumnCount = 0;
            KeyboardInputEnabled = true;
        }

        /// <summary>
        /// Sets the number of columns.
        /// </summary>
        /// <param name="columnCount">Number of columns.</param>
        protected void SetColumnCount(int columnCount)
        {
            if (columnCount == m_ColumnCount) return;

            if (columnCount >= MaxColumns)
                throw new ArgumentException("Invalid column count", "columnCount");

            for (int i = 0; i < MaxColumns; i++)
            {
                if (i < columnCount)
                {
                    if (null == m_Columns[i])
                    {
                        m_Columns[i] = new Label(this);
                        m_Columns[i].Padding = Padding.Three;
                        m_Columns[i].Margin = new Margin(0, 0, 2, 0); // to separate them slightly
                        if (i == columnCount - 1)
                        {
                            // last column fills remaining space
                            m_Columns[i].Dock = Pos.Fill;
                        }
                        else
                        {
                            m_Columns[i].Dock = Pos.Left;
                        }
                    }
                }
                else if (null != m_Columns[i])
                {
                    RemoveChild(m_Columns[i], true);
                    m_Columns[i] = null;
                }

                m_ColumnCount = columnCount;
            }
        }

        /// <summary>
        /// Sets the column width (in pixels).
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <param name="width">Column width.</param>
        public void SetColumnWidth(int column, int width)
        {
            if (null == m_Columns[column]) 
                return;
            if (m_Columns[column].Width == width) 
                return;

            m_Columns[column].Width = width;
        }

        /// <summary>
        /// Sets the text of a specified cell.
        /// </summary>
        /// <param name="column">Column number.</param>
        /// <param name="text">Text to set.</param>
        public void SetCellText(int column, string text)
        {
            if (null == m_Columns[column]) 
                return;

            m_Columns[column].Text = text;
        }

        /// <summary>
        /// Sets the contents of a specified cell.
        /// </summary>
        /// <param name="column">Column number.</param>
        /// <param name="control">Cell contents.</param>
        /// <param name="enableMouseInput">Determines whether mouse input should be enabled for the cell.</param>
        public void SetCellContents(int column, ControlBase control, bool enableMouseInput = false)
        {
            if (null == m_Columns[column]) 
                return;

            control.Parent = m_Columns[column];
            m_Columns[column].MouseInputEnabled = enableMouseInput;
        }

        /// <summary>
        /// Gets the contents of a specified cell.
        /// </summary>
        /// <param name="column">Column number.</param>
        /// <returns>Control embedded in the cell.</returns>
        public ControlBase GetCellContents(int column)
        {
            return m_Columns[column];
        }

        protected virtual void OnRowSelected()
        {
            if (Selected != null)
                Selected.Invoke(this, new ItemSelectedEventArgs(this));
        }

        /// <summary>
        /// Sizes all cells to fit contents.
        /// </summary>
        public void SizeToContents()
        {
            int width = 0;
            int height = 0;

            for (int i = 0; i < m_ColumnCount; i++)
            {
                if (null == m_Columns[i]) 
                    continue;

                // Note, more than 1 child here, because the 
                // label has a child built in ( The Text )
                if (m_Columns[i].Children.Count > 1)
                {
                    m_Columns[i].SizeToChildren();
                }
                else
                {
                    m_Columns[i].SizeToContents();
                }
    
                //if (i == m_ColumnCount - 1) // last column
                //    m_Columns[i].Width = Parent.Width - width; // fill if not autosized

                width += m_Columns[i].Width + m_Columns[i].Margin.Left + m_Columns[i].Margin.Right;
                height = Math.Max(height, m_Columns[i].Height + m_Columns[i].Margin.Top + m_Columns[i].Margin.Bottom);
            }

            SetSize(width, height);
        }

        /// <summary>
        /// Sets the text color for all cells.
        /// </summary>
        /// <param name="color">Text color.</param>
        public void SetTextColor(Color color)
        {
            for (int i = 0; i < m_ColumnCount; i++)
            {
                if (null == m_Columns[i]) continue;
                m_Columns[i].TextColor = color;
            }
        }

        /// <summary>
        /// Returns text of a specified row cell (default first).
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <returns>Column cell text.</returns>
        public string GetText(int column = 0)
        {
            return m_Columns[column].Text;
        }

        /// <summary>
        /// Handler for Copy event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected override void OnCopy(ControlBase from, EventArgs args)
        {
            Platform.Neutral.SetClipboardText(Text);
        }
    }
}
