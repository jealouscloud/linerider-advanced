using System;
using System.Linq;

namespace Gwen.Controls.Layout
{
    /// <summary>
    /// Base class for multi-column tables.
    /// </summary>
    public class Table : ControlBase
    {
        // only children of this control should be TableRow.

        private bool m_SizeToContents;
        private int m_ColumnCount;
        private int m_DefaultRowHeight;
        private int m_MaxWidth; // for autosizing, if nonzero - fills last cell up to this size

        private readonly int[] m_ColumnWidth;

        /// <summary>
        /// Column count (default 1).
        /// </summary>
        public int ColumnCount { get { return m_ColumnCount; } set { SetColumnCount(value); Invalidate(); } }

        /// <summary>
        /// Row count.
        /// </summary>
        public int RowCount { get { return Children.Count; } }

        /// <summary>
        /// Gets or sets default height for new table rows.
        /// </summary>
        public int DefaultRowHeight { get { return m_DefaultRowHeight; } set { m_DefaultRowHeight = value; } }

        /// <summary>
        /// Returns specific row of the table.
        /// </summary>
        /// <param name="index">Row index.</param>
        /// <returns>Row at the specified index.</returns>
        public TableRow this[int index] { get { return Children[index] as TableRow; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Table(ControlBase parent) : base(parent)
        {
            m_ColumnCount = 1;
            m_DefaultRowHeight = 22;

            m_ColumnWidth = new int[TableRow.MaxColumns];

            for (int i = 0; i < TableRow.MaxColumns; i++)
            {
                m_ColumnWidth[i] = 20;
            }

            m_SizeToContents = false;
        }

        /// <summary>
        /// Sets the number of columns.
        /// </summary>
        /// <param name="count">Number of columns.</param>
        public void SetColumnCount(int count)
        {
            if (m_ColumnCount == count) return;
            foreach (TableRow row in Children.OfType<TableRow>())
            {
                row.ColumnCount = count;
            }

            m_ColumnCount = count;
        }

        /// <summary>
        /// Sets the column width (in pixels).
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <param name="width">Column width.</param>
        public void SetColumnWidth(int column, int width)
        {
            if (m_ColumnWidth[column] == width) return;
            m_ColumnWidth[column] = width;
            Invalidate();
        }

        /// <summary>
        /// Gets the column width (in pixels).
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <returns>Column width.</returns>
        public int GetColumnWidth(int column)
        {
            return m_ColumnWidth[column];
        }

        /// <summary>
        /// Adds a new empty row.
        /// </summary>
        /// <returns>Newly created row.</returns>
        public TableRow AddRow()
        {
            TableRow row = new TableRow(this);
            row.ColumnCount = m_ColumnCount;
            row.Height = m_DefaultRowHeight;
            row.Dock = Pos.Top;
            return row;
        }

        /// <summary>
        /// Adds a new row.
        /// </summary>
        /// <param name="row">Row to add.</param>
        public void AddRow(TableRow row)
        {
            row.Parent = this;
            row.ColumnCount = m_ColumnCount;
            row.Height = m_DefaultRowHeight;
            row.Dock = Pos.Top;
        }

        /// <summary>
        /// Adds a new row with specified text in first column.
        /// </summary>
        /// <param name="text">Text to add.</param>
        /// <returns>New row.</returns>
        public TableRow AddRow(string text)
        {
            var row = AddRow();
            row.SetCellText(0, text);
            return row;
        }

        /// <summary>
        /// Removes a row by reference.
        /// </summary>
        /// <param name="row">Row to remove.</param>
        public void RemoveRow(TableRow row)
        {
            RemoveChild(row, true);
        }

        /// <summary>
        /// Removes a row by index.
        /// </summary>
        /// <param name="idx">Row index.</param>
        public void RemoveRow(int idx)
        {
            var row = Children[idx];
            RemoveRow(row as TableRow);
        }

        /// <summary>
        /// Removes all rows.
        /// </summary>
        public void RemoveAll()
        {
            while (RowCount > 0)
                RemoveRow(0);
        }

        /// <summary>
        /// Gets the index of a specified row.
        /// </summary>
        /// <param name="row">Row to search for.</param>
        /// <returns>Row index if found, -1 otherwise.</returns>
        public int GetRowIndex(TableRow row)
        {
            return Children.IndexOf(row);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            base.Layout(skin);

            bool even = false;
            foreach (TableRow row in Children)
            {
                row.EvenRow = even;
                even = !even;
                for (int i = 0; i < m_ColumnCount; i++)
                {
                    row.SetColumnWidth(i, m_ColumnWidth[i]);
                }
            }
        }

        protected override void PostLayout(Skin.SkinBase skin)
        {
            base.PostLayout(skin);
            if (m_SizeToContents)
            {
                DoSizeToContents();
                m_SizeToContents = false;
            }
        }

        /// <summary>
        /// Sizes to fit contents.
        /// </summary>
        public void SizeToContents(int maxWidth)
        {
            m_MaxWidth = maxWidth;
            m_SizeToContents = true;
            Invalidate();
        }

        protected void DoSizeToContents()
        {
            int height = 0;
            int width = 0;

            foreach (TableRow row in Children)
            {
                row.SizeToContents(); // now all columns fit but only in this particular row

                for (int i = 0; i < ColumnCount; i++)
                {
                    ControlBase cell = row.GetColumn(i);
                    if (null != cell)
                    {
                        if (i < ColumnCount - 1 || m_MaxWidth == 0)
                            m_ColumnWidth[i] = Math.Max(m_ColumnWidth[i], cell.Width + cell.Margin.Left + cell.Margin.Right);
                        else
                            m_ColumnWidth[i] = m_MaxWidth - width; // last cell - fill
                    }
                }
                height += row.Height;
            }

            // sum all column widths 
            for (int i = 0; i < ColumnCount; i++)
            {
                width += m_ColumnWidth[i];
            }

            SetSize(width, height);
            //InvalidateParent();
        }
    }
}
