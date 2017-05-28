using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Gwen.Controls.Layout;

namespace Gwen.Controls
{
    /// <summary>
    /// ListBox control.
    /// </summary>
    public class ListBox : ScrollControl
    {
        private readonly Table m_Table;
        private readonly List<ListBoxRow> m_SelectedRows;

        private bool m_MultiSelect;
        private bool m_IsToggle;
        private bool m_SizeToContents;
        private Pos m_OldDock; // used while autosizing

        /// <summary>
        /// Determines whether multiple rows can be selected at once.
        /// </summary>
        public bool AllowMultiSelect
        {
            get { return m_MultiSelect; }
            set
            {
                m_MultiSelect = value;
                if (value)
                    IsToggle = true;
            }
        }

        /// <summary>
        /// Determines whether rows can be unselected by clicking on them again.
        /// </summary>
        public bool IsToggle { get { return m_IsToggle; } set { m_IsToggle = value; } }

        /// <summary>
        /// Number of rows in the list box.
        /// </summary>
        public int RowCount { get { return m_Table.RowCount; } }

        /// <summary>
        /// Returns specific row of the ListBox.
        /// </summary>
        /// <param name="index">Row index.</param>
        /// <returns>Row at the specified index.</returns>
        public ListBoxRow this[int index] { get { return m_Table[index] as ListBoxRow; } }

        /// <summary>
        /// List of selected rows.
        /// </summary>
        public IEnumerable<TableRow> SelectedRows { get { return m_SelectedRows; } }

        /// <summary>
        /// First selected row (and only if list is not multiselectable).
        /// </summary>
        public ListBoxRow SelectedRow
        {
            get
            {
                if (m_SelectedRows.Count == 0)
                    return null;
                return m_SelectedRows[0];
            }
            set
            {
                if (m_Table.Children.Contains(value))
                {
                    if (AllowMultiSelect)
                    {
                        SelectRow(value, false);
                    }
                    else
                    {
                        SelectRow(value, true);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the selected row number.
        /// </summary>
        public int SelectedRowIndex
        {
            get
            {
                var selected = SelectedRow;
                if (selected == null)
                    return -1;
                return m_Table.GetRowIndex(selected);
            }
            set
            {
                SelectRow(value);
            }
        }

        /// <summary>
        /// Column count of table rows.
        /// </summary>
        public int ColumnCount { get { return m_Table.ColumnCount; } set { m_Table.ColumnCount = value; Invalidate(); } }

        /// <summary>
        /// Invoked when a row has been selected.
        /// </summary>
        public event GwenEventHandler<ItemSelectedEventArgs> RowSelected;

        /// <summary>
        /// Invoked whan a row has beed unselected.
        /// </summary>
        public event GwenEventHandler<ItemSelectedEventArgs> RowUnselected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListBox"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public ListBox(ControlBase parent)
            : base(parent)
        {
            m_SelectedRows = new List<ListBoxRow>();

			MouseInputEnabled = true;
            EnableScroll(false, true);
            AutoHideBars = true;
            Margin = Margin.One;

            m_Table = new Table(this);
            m_Table.Dock = Pos.Fill;
            m_Table.ColumnCount = 1;
            m_Table.BoundsChanged += TableResized;

            m_MultiSelect = false;
            m_IsToggle = false;
        }

        /// <summary>
        /// Selects the specified row by index.
        /// </summary>
        /// <param name="index">Row to select.</param>
        /// <param name="clearOthers">Determines whether to deselect previously selected rows.</param>
        public void SelectRow(int index, bool clearOthers = false)
        {
            if (index < 0 || index >= m_Table.RowCount)
                return;

            SelectRow(m_Table.Children[index], clearOthers);
        }

        /// <summary>
        /// Selects the specified row(s) by text.
        /// </summary>
        /// <param name="rowText">Text to search for (exact match).</param>
        /// <param name="clearOthers">Determines whether to deselect previously selected rows.</param>
        public void SelectRows(string rowText, bool clearOthers = false)
        {
            var rows = m_Table.Children.OfType<ListBoxRow>().Where(x => x.Text == rowText);
            foreach (ListBoxRow row in rows)
            {
                SelectRow(row, clearOthers);
            }
        }

        /// <summary>
        /// Selects the specified row(s) by regex text search.
        /// </summary>
        /// <param name="pattern">Regex pattern to search for.</param>
        /// <param name="regexOptions">Regex options.</param>
        /// <param name="clearOthers">Determines whether to deselect previously selected rows.</param>
        public void SelectRowsByRegex(string pattern, RegexOptions regexOptions = RegexOptions.None, bool clearOthers = false)
        {
            var rows = m_Table.Children.OfType<ListBoxRow>().Where(x => Regex.IsMatch(x.Text, pattern));
            foreach (ListBoxRow row in rows)
            {
                SelectRow(row, clearOthers);
            }
        }

        /// <summary>
        /// Slelects the specified row.
        /// </summary>
        /// <param name="control">Row to select.</param>
        /// <param name="clearOthers">Determines whether to deselect previously selected rows.</param>
        public void SelectRow(ControlBase control, bool clearOthers = false)
        {
            if (!AllowMultiSelect || clearOthers)
                UnselectAll();

            ListBoxRow row = control as ListBoxRow;
            if (row == null)
                return;

            // TODO: make sure this is one of our rows!
            row.IsSelected = true;
            m_SelectedRows.Add(row);
            if (RowSelected != null)
				RowSelected.Invoke(this, new ItemSelectedEventArgs(row));
        }

        /// <summary>
        /// Removes the all rows from the ListBox
        /// </summary>
        /// <param name="idx">Row index.</param>
        public void RemoveAllRows()
        {
            m_Table.DeleteAllChildren();
        }

        /// <summary>
        /// Removes the specified row by index.
        /// </summary>
        /// <param name="idx">Row index.</param>
        public void RemoveRow(int idx)
        {
            m_Table.RemoveRow(idx); // this calls Dispose()
        }

        /// <summary>
        /// Adds a new row.
        /// </summary>
        /// <param name="label">Row text.</param>
        /// <returns>Newly created control.</returns>
        public ListBoxRow AddRow(string label)
        {
            return AddRow(label, String.Empty);
        }

        /// <summary>
        /// Adds a new row.
        /// </summary>
        /// <param name="label">Row text.</param>
        /// <param name="name">Internal control name.</param>
        /// <returns>Newly created control.</returns>
        public ListBoxRow AddRow(string label, string name)
        {
            return AddRow(label, name, null);
        }

        /// <summary>
        /// Adds a new row.
        /// </summary>
        /// <param name="label">Row text.</param>
        /// <param name="name">Internal control name.</param>
        /// <param name="UserData">User data for newly created row</param>
        /// <returns>Newly created control.</returns>
        public ListBoxRow AddRow(string label, string name, Object UserData)
        {
            ListBoxRow row = new ListBoxRow(this);
            m_Table.AddRow(row);

            row.SetCellText(0, label);
            row.Name = name;
            row.UserData = UserData;

            row.Selected += OnRowSelected;

            m_Table.SizeToContents(Width);

            return row;
        }

        /// <summary>
        /// Sets the column width (in pixels).
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <param name="width">Column width.</param>
        public void SetColumnWidth(int column, int width)
        {
            m_Table.SetColumnWidth(column, width);
            Invalidate();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawListBox(this);
        }

        /// <summary>
        /// Deselects all rows.
        /// </summary>
        public virtual void UnselectAll()
        {
            foreach (ListBoxRow row in m_SelectedRows)
            {
                row.IsSelected = false;
                if (RowUnselected != null)
					RowUnselected.Invoke(this, new ItemSelectedEventArgs(row));
            }
            m_SelectedRows.Clear();
        }

        /// <summary>
        /// Unselects the specified row.
        /// </summary>
        /// <param name="row">Row to unselect.</param>
        public void UnselectRow(ListBoxRow row)
        {
            row.IsSelected = false;
            m_SelectedRows.Remove(row);

            if (RowUnselected != null)
                RowUnselected.Invoke(this, new ItemSelectedEventArgs(row));
        }

        /// <summary>
        /// Handler for the row selection event.
        /// </summary>
        /// <param name="control">Event source.</param>
        protected virtual void OnRowSelected(ControlBase control, ItemSelectedEventArgs args)
        {
            // [omeg] changed default behavior
            bool clear = false;// !InputHandler.InputHandler.IsShiftDown;
			ListBoxRow row = args.SelectedItem as ListBoxRow;
            if (row == null)
                return;

            if (row.IsSelected)
            {
                if (IsToggle)
                    UnselectRow(row);
            }
            else
            {
                SelectRow(row, clear);
            }
        }

        /// <summary>
        /// Removes all rows.
        /// </summary>
        public virtual void Clear()
        {
            UnselectAll();
            m_Table.RemoveAll();
        }

        public void SizeToContents()
        {
            m_SizeToContents = true;
            // docking interferes with autosizing so we disable it until sizing is done
            m_OldDock = m_Table.Dock;
            m_Table.Dock = Pos.None;
            m_Table.SizeToContents(0); // autosize without constraints
        }

        private void TableResized(ControlBase control, EventArgs args)
        {
            if (m_SizeToContents)
            {
                SetSize(m_Table.Width, m_Table.Height);
                m_SizeToContents = false;
                m_Table.Dock = m_OldDock;
                Invalidate();
            }
        }

        /// <summary>
        /// Selects the first menu item with the given text it finds. 
        /// If a menu item can not be found that matches input, nothing happens.
        /// </summary>
        /// <param name="label">The label to look for, this is what is shown to the user.</param>
        public void SelectByText(string text)
        {
            foreach (ListBoxRow item in m_Table.Children)
            {
                if (item.Text == text)
                {
                    SelectedRow = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Selects the first menu item with the given internal name it finds.
        /// If a menu item can not be found that matches input, nothing happens.
        /// </summary>
        /// <param name="name">The internal name to look for. To select by what is displayed to the user, use "SelectByText".</param>
        public void SelectByName(string name)
        {
            foreach (ListBoxRow item in m_Table.Children)
            {
                if (item.Name == name)
                {
                    SelectedRow = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Selects the first menu item with the given user data it finds.
        /// If a menu item can not be found that matches input, nothing happens.
        /// </summary>
        /// <param name="userdata">The UserData to look for. The equivalency check uses "param.Equals(item.UserData)".
        /// If null is passed in, it will look for null/unset UserData.</param>
        public void SelectByUserData(object userdata)
        {
            foreach (ListBoxRow item in m_Table.Children)
            {
                if (userdata == null)
                {
                    if (item.UserData == null)
                    {
                        SelectedRow = item;
                        return;
                    }
                }
                else if (userdata.Equals(item.UserData))
                {
                    SelectedRow = item;
                    return;
                }
            }
        }
    }
}
