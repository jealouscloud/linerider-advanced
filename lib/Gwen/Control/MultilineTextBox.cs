using Gwen.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Gwen.Controls
{
    public class MultilineTextBox : Label
    {
        #region Events

        /// <summary>
        /// Invoked when the text has changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> TextChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Indicates whether the control will accept Tab characters as input.
        /// </summary>
        public bool AcceptTabs { get; set; }

        /// <summary>
        /// Get a point representing where the endpoint of text selection.
        /// Y is line number, X is character position on that line.
        /// </summary>
        public Point CursorEnd
        {
            get
            {
                if (m_TextLines == null || m_TextLines.Count() == 0)
                    return new Point(0, 0);

                int Y = m_CursorEnd.Y;
                Y = Math.Max(Y, 0);
                Y = Math.Min(Y, m_TextLines.Count() - 1);

                int X = m_CursorEnd.X; //X may be beyond the last character, but we will want to draw it at the end of line.
                X = Math.Max(X, 0);
                X = Math.Min(X, m_TextLines[Y].Length);

                return new Point(X, Y);
            }
            set
            {
                m_CursorEnd.X = value.X;
                m_CursorEnd.Y = value.Y;
                RefreshCursorBounds();
            }
        }

        /// <summary>
        /// Get a point representing where the cursor physically appears on the screen.
        /// Y is line number, X is character position on that line.
        /// </summary>
        public Point CursorPosition
        {
            get
            {
                if (m_TextLines == null || m_TextLines.Count() == 0)
                    return new Point(0, 0);

                int Y = m_CursorPos.Y;
                Y = Math.Max(Y, 0);
                Y = Math.Min(Y, m_TextLines.Count() - 1);

                int X = m_CursorPos.X; //X may be beyond the last character, but we will want to draw it at the end of line.
                X = Math.Max(X, 0);
                X = Math.Min(X, m_TextLines[Y].Length);

                return new Point(X, Y);
            }
            set
            {
                m_CursorPos.X = value.X;
                m_CursorPos.Y = value.Y;
                RefreshCursorBounds();
            }
        }

        /// <summary>
        /// Indicates whether the text has active selection.
        /// </summary>
        public bool HasSelection { get { return m_CursorPos != m_CursorEnd; } }

        /// <summary>
        /// Gets and sets the text to display to the user. Each line is seperated by
        /// an Environment.NetLine character.
        /// </summary>
        public override string Text
        {
            get
            {
                string ret = "";
                for (int i = 0; i < TotalLines; i++)
                {
                    ret += m_TextLines[i];
                    if (i != TotalLines - 1)
                    {
                        ret += Environment.NewLine;
                    }
                }
                return ret;
            }
            set
            {
                //Label (base) calls SetText.
                //SetText is overloaded to dump value into TextLines.
                //We're cool.
                base.Text = value;
            }
        }

        /// <summary>
        /// Returns the number of lines that are in the Multiline Text Box.
        /// </summary>
        public int TotalLines
        {
            get
            {
                return m_TextLines.Count;
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBox"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public MultilineTextBox(ControlBase parent) : base(parent)
        {
            AutoSizeToContents = false;
            SetSize(200, 20);

            MouseInputEnabled = true;
            KeyboardInputEnabled = true;

            Alignment = Pos.Left | Pos.Top;
            TextPadding = new Padding(4, 2, 4, 2);

            m_CursorPos = new Point(0, 0);
            m_CursorEnd = new Point(0, 0);
            m_SelectAll = false;

            TextColor = Color.FromArgb(255, 50, 50, 50); // TODO: From Skin

            IsTabable = false;
            AcceptTabs = true;

            m_ScrollControl = new ScrollControl(this);
            m_ScrollControl.Dock = Pos.Fill;
            m_ScrollControl.EnableScroll(true, true);
            m_ScrollControl.AutoHideBars = true;
            m_ScrollControl.Margin = Margin.One;
            m_InnerPanel = m_ScrollControl;
            m_Text.Parent = m_InnerPanel;
            m_ScrollControl.InnerPanel.BoundsChanged += new GwenEventHandler<EventArgs>(ScrollChanged);

            m_TextLines.Add(String.Empty);

            // [halfofastaple] TODO Figure out where these numbers come from. See if we can remove the magic numbers.
            //	This should be as simple as 'm_ScrollControl.AutoSizeToContents = true' or 'm_ScrollControl.NoBounds()'
            m_ScrollControl.SetInnerSize(1000, 1000);

            AddAccelerator("Ctrl + C", OnCopy);
            AddAccelerator("Ctrl + X", OnCut);
            AddAccelerator("Ctrl + V", OnPaste);
            AddAccelerator("Ctrl + A", OnSelectAll);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Deletes selected text.
        /// </summary>
        public void EraseSelection()
        {
            if (StartPoint.Y == EndPoint.Y)
            {
                int start = StartPoint.X;
                int end = EndPoint.X;

                m_TextLines[StartPoint.Y] = m_TextLines[StartPoint.Y].Remove(start, end - start);
            }
            else {
                /* Remove Start */
                if (StartPoint.X < m_TextLines[StartPoint.Y].Length)
                {
                    m_TextLines[StartPoint.Y] = m_TextLines[StartPoint.Y].Remove(StartPoint.X);
                }

                /* Remove Middle */
                for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
                {
                    m_TextLines.RemoveAt(StartPoint.Y + 1);
                }

                /* Remove End */
                if (EndPoint.X < m_TextLines[StartPoint.Y + 1].Length)
                {
                    m_TextLines[StartPoint.Y] += m_TextLines[StartPoint.Y + 1].Substring(EndPoint.X);
                }
                m_TextLines.RemoveAt(StartPoint.Y + 1);
            }

            // Move the cursor to the start of the selection,
            // since the end is probably outside of the string now.
            m_CursorPos = StartPoint;
            m_CursorEnd = StartPoint;

            Invalidate();
            RefreshCursorBounds();
        }

        /// <summary>
        /// Returns currently selected text.
        /// </summary>
        /// <returns>Current selection.</returns>
        public string GetSelection()
        {
            if (!HasSelection) return String.Empty;

            string str = String.Empty;

            if (StartPoint.Y == EndPoint.Y)
            {
                int start = StartPoint.X;
                int end = EndPoint.X;

                str = m_TextLines[m_CursorPos.Y];
                str = str.Substring(start, end - start);
            }
            else {
                str = String.Empty;
                str += m_TextLines[StartPoint.Y].Substring(StartPoint.X); //Copy start
                for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
                {
                    str += m_TextLines[StartPoint.Y + i]; //Copy middle
                }
                str += m_TextLines[EndPoint.Y].Substring(0, EndPoint.X); //Copy end
            }

            return str;
        }

        public string GetTextLine(int index)
        {
            return m_TextLines[index];
        }

        /// <summary>
        /// Invalidates the control.
        /// </summary>
        /// <remarks>
        /// Causes layout, repaint, invalidates cached texture.
        /// </remarks>
        public override void Invalidate()
        {
            if (m_Text != null)
            {
                m_Text.String = Text;
            }
            if (AutoSizeToContents)
                SizeToContents();

            base.Invalidate();
            InvalidateParent();
            OnTextChanged();
        }

        /// <summary>
        /// Sets the label text.
        /// </summary>
        /// <param name="str">Text to set.</param>
        /// <param name="doEvents">Determines whether to invoke "text changed" event.</param>
        public override void SetText(string str, bool doEvents = true)
        {
            string EasySplit = str.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] Lines = EasySplit.Split('\n');

            m_TextLines = new List<string>(Lines);

            Invalidate();
            RefreshCursorBounds();
        }

        public void SetTextLine(int index, string value)
        {
            m_TextLines[index] = value;
        }

        #endregion Methods

        #region Fields

        protected Rectangle m_CaretBounds;

        #endregion Fields

        /// <summary>
        /// Returns index of the character closest to specified point (in canvas coordinates).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected override Point GetClosestCharacter(int px, int py)
        {
            Point p = CanvasPosToLocal(new Point(px, py));
            double distance = Double.MaxValue;
            Point Best = new Point(0, 0);
            string sub = String.Empty;

            /* Find the appropriate Y row (always pick whichever y the mouse currently is on) */
            for (int y = 0; y < m_TextLines.Count(); y++)
            {
                sub += m_TextLines[y] + Environment.NewLine;
                Point cp = Skin.Renderer.MeasureText(Font, sub);

                double YDist = Math.Abs(cp.Y - p.Y);
                if (YDist < distance)
                {
                    distance = YDist;
                    Best.Y = y;
                }
            }

            /* Find the best X row, closest char */
            sub = String.Empty;
            distance = Double.MaxValue;
            for (int x = 0; x <= m_TextLines[Best.Y].Count(); x++)
            {
                if (x < m_TextLines[Best.Y].Count())
                {
                    sub += m_TextLines[Best.Y][x];
                }
                else {
                    sub += " ";
                }

                Point cp = Skin.Renderer.MeasureText(Font, sub);

                double XDiff = Math.Abs(cp.X - p.X);

                if (XDiff < distance)
                {
                    distance = XDiff;
                    Best.X = x;
                }
            }

            return Best;
        }

        /// <summary>
        /// Inserts text at current cursor position, erasing selection if any.
        /// </summary>
        /// <param name="text">Text to insert.</param>
        protected void InsertText(string text)
        {
            // TODO: Make sure fits (implement maxlength)

            if (HasSelection)
            {
                EraseSelection();
            }

            string str = m_TextLines[m_CursorPos.Y];
            str = str.Insert(CursorPosition.X, text);
            m_TextLines[m_CursorPos.Y] = str;

            m_CursorPos.X = CursorPosition.X + text.Length;
            m_CursorEnd = m_CursorPos;

            Invalidate();
            RefreshCursorBounds();
        }

        protected virtual void MakeCaretVisible()
        {
            int caretPos = GetCharacterPosition(CursorPosition).X - TextX;

            // If the caret is already in a semi-good position, leave it.
            {
                int realCaretPos = caretPos + TextX;
                if (realCaretPos > Width * 0.1f && realCaretPos < Width * 0.9f)
                    return;
            }

            // The ideal position is for the caret to be right in the middle
            int idealx = (int)(-caretPos + Width * 0.5f);

            // Don't show too much whitespace to the right
            if (idealx + TextWidth < Width - TextPadding.Right)
                idealx = -TextWidth + (Width - TextPadding.Right);

            // Or the left
            if (idealx > TextPadding.Left)
                idealx = TextPadding.Left;

            SetTextPosition(idealx, TextY);
        }

        /// <summary>
        /// Handler for character input event.
        /// </summary>
        /// <param name="chr">Character typed.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnChar(char chr)
        {
            //base.OnChar(chr);
            if (chr == '\t' && !AcceptTabs) return false;

            InsertText(chr.ToString());
            return true;
        }

        /// <summary>
        /// Handler invoked when control children's bounds change.
        /// </summary>
        /// <param name="oldChildBounds"></param>
        /// <param name="child"></param>
        protected override void OnChildBoundsChanged(System.Drawing.Rectangle oldChildBounds, ControlBase child)
        {
            if (m_ScrollControl != null)
            {
                m_ScrollControl.UpdateScrollBars();
            }
        }

        /// <summary>
        /// Handler for Copy event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected override void OnCopy(ControlBase from, EventArgs args)
        {
            if (!HasSelection) return;
            base.OnCopy(from, args);

            Platform.Neutral.SetClipboardText(GetSelection());
        }

        /// <summary>
        /// Handler for Cut event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected override void OnCut(ControlBase from, EventArgs args)
        {
            if (!HasSelection) return;
            base.OnCut(from, args);

            Platform.Neutral.SetClipboardText(GetSelection());
            EraseSelection();
        }

        /// <summary>
        /// Handler for Backspace keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyBackspace(bool down)
        {
            if (!down) return true;

            if (HasSelection)
            {
                EraseSelection();
                return true;
            }

            if (m_CursorPos.X == 0)
            {
                if (m_CursorPos.Y == 0)
                {
                    return true; //Nothing left to delete
                }
                else {
                    string lhs = m_TextLines[m_CursorPos.Y - 1];
                    string rhs = m_TextLines[m_CursorPos.Y];
                    m_TextLines.RemoveAt(m_CursorPos.Y);
                    OnKeyUp(true);
                    OnKeyEnd(true);
                    m_TextLines[m_CursorPos.Y] = lhs + rhs;
                }
            }
            else {
                string CurrentLine = m_TextLines[m_CursorPos.Y];
                string lhs = CurrentLine.Substring(0, CursorPosition.X - 1);
                string rhs = CurrentLine.Substring(CursorPosition.X);
                m_TextLines[m_CursorPos.Y] = lhs + rhs;
                OnKeyLeft(true);
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Delete keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyDelete(bool down)
        {
            if (!down) return true;

            if (HasSelection)
            {
                EraseSelection();
                return true;
            }

            if (m_CursorPos.X == m_TextLines[m_CursorPos.Y].Length)
            {
                if (m_CursorPos.Y == m_TextLines.Count - 1)
                {
                    return true; //Nothing left to delete
                }
                else {
                    string lhs = m_TextLines[m_CursorPos.Y];
                    string rhs = m_TextLines[m_CursorPos.Y + 1];
                    m_TextLines.RemoveAt(m_CursorPos.Y + 1);
                    OnKeyEnd(true);
                    m_TextLines[m_CursorPos.Y] = lhs + rhs;
                }
            }
            else {
                string CurrentLine = m_TextLines[m_CursorPos.Y];
                string lhs = CurrentLine.Substring(0, CursorPosition.X);
                string rhs = CurrentLine.Substring(CursorPosition.X + 1);
                m_TextLines[m_CursorPos.Y] = lhs + rhs;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Down Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyDown(bool down)
        {
            if (!down) return true;

            if (m_CursorPos.Y < TotalLines - 1)
            {
                m_CursorPos.Y += 1;
            }

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for End Key keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyEnd(bool down)
        {
            if (!down) return true;

            m_CursorPos.X = m_TextLines[m_CursorPos.Y].Length;

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Home Key keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyHome(bool down)
        {
            if (!down) return true;

            m_CursorPos.X = 0;

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Left Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyLeft(bool down)
        {
            if (!down) return true;

            if (m_CursorPos.X > 0)
            {
                m_CursorPos.X = Math.Min(m_CursorPos.X - 1, m_TextLines[m_CursorPos.Y].Length);
            }
            else {
                if (m_CursorPos.Y > 0)
                {
                    OnKeyUp(down);
                    OnKeyEnd(down);
                }
            }

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Return keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyReturn(bool down)
        {
            if (down) return true;

            //Split current string, putting the rhs on a new line
            string CurrentLine = m_TextLines[m_CursorPos.Y];
            string lhs = CurrentLine.Substring(0, CursorPosition.X);
            string rhs = CurrentLine.Substring(CursorPosition.X);

            m_TextLines[m_CursorPos.Y] = lhs;
            m_TextLines.Insert(m_CursorPos.Y + 1, rhs);

            OnKeyDown(true);
            OnKeyHome(true);

            if (m_CursorPos.Y == TotalLines - 1)
            {
                m_ScrollControl.ScrollToBottom();
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Right Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyRight(bool down)
        {
            if (!down) return true;

            if (m_CursorPos.X < m_TextLines[m_CursorPos.Y].Length)
            {
                m_CursorPos.X = Math.Min(m_CursorPos.X + 1, m_TextLines[m_CursorPos.Y].Length);
            }
            else {
                if (m_CursorPos.Y < m_TextLines.Count - 1)
                {
                    OnKeyDown(down);
                    OnKeyHome(down);
                }
            }

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        /// <summary>
        /// Handler for Tab Key keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyTab(bool down)
        {
            if (!AcceptTabs) return base.OnKeyTab(down);
            if (!down) return false;

            OnChar('\t');
            return true;
        }

        /// <summary>
        /// Handler for Up Arrow keyboard event.
        /// </summary>
        /// <param name="down">Indicates whether the key was pressed or released.</param>
        /// <returns>
        /// True if handled.
        /// </returns>
        protected override bool OnKeyUp(bool down)
        {
            if (!down) return true;

            if (m_CursorPos.Y > 0)
            {
                m_CursorPos.Y -= 1;
            }

            if (!Input.InputHandler.IsShiftDown)
            {
                m_CursorEnd = m_CursorPos;
            }

            Invalidate();
            RefreshCursorBounds();

            return true;
        }

        //    }
        //}
        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down)
        {
            base.OnMouseClickedLeft(x, y, down);
            if (m_SelectAll)
            {
                OnSelectAll(this, EventArgs.Empty);
                //m_SelectAll = false;
                return;
            }

            Point coords = GetClosestCharacter(x, y);

            if (down)
            {
                CursorPosition = coords;

                if (!Input.InputHandler.IsShiftDown)
                    CursorEnd = coords;

                InputHandler.MouseFocus = this;
            }
            else {
                if (InputHandler.MouseFocus == this)
                {
                    CursorPosition = coords;
                    InputHandler.MouseFocus = null;
                }
            }

            Invalidate();
            RefreshCursorBounds();
        }

        /// <summary>
        /// Handler invoked on mouse double click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        protected override void OnMouseDoubleClickedLeft(int x, int y)
        {
            //base.OnMouseDoubleClickedLeft(x, y);
            OnSelectAll(this, EventArgs.Empty);
        }

        //        m_CursorEnd = m_CursorPos;
        //    /* Multiline Delete */
        //    } else {
        /// <summary>
        /// Handler invoked on mouse moved event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="dx">X change.</param>
        /// <param name="dy">Y change.</param>
        protected override void OnMouseMoved(int x, int y, int dx, int dy)
        {
            base.OnMouseMoved(x, y, dx, dy);
            if (InputHandler.MouseFocus != this) return;

            Point c = GetClosestCharacter(x, y);

            CursorPosition = c;

            Invalidate();
            RefreshCursorBounds();
        }

        protected override bool OnMouseWheeled(int delta)
        {
            return m_ScrollControl.InputMouseWheeled(delta);
        }

        /// <summary>
        /// Handler for Paste event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected override void OnPaste(ControlBase from, EventArgs args)
        {
            base.OnPaste(from, args);
            InsertText(Platform.Neutral.GetClipboardText());
        }

        /// <summary>
        /// Handler for Select All event.
        /// </summary>
        /// <param name="from">Source control.</param>
        protected override void OnSelectAll(ControlBase from, EventArgs args)
        {
            //base.OnSelectAll(from);
            m_CursorEnd = new Point(0, 0);
            m_CursorPos = new Point(m_TextLines.Last().Length, m_TextLines.Count());

            RefreshCursorBounds();
        }

        /// <summary>
        /// Handler for text changed event.
        /// </summary>
        protected override void OnTextChanged()
        {
            base.OnTextChanged();
            if (TextChanged != null)
                TextChanged.Invoke(this, EventArgs.Empty);
        }

        protected void RefreshCursorBounds()
        {
            m_LastInputTime = Platform.Neutral.GetTimeInSeconds();

            MakeCaretVisible();

            Point pA = GetCharacterPosition(CursorPosition);
            Point pB = GetCharacterPosition(m_CursorEnd);

            //m_SelectionBounds.X = Math.Min(pA.X, pB.X);
            //m_SelectionBounds.Y = TextY - 1;
            //m_SelectionBounds.Width = Math.Max(pA.X, pB.X) - m_SelectionBounds.X;
            //m_SelectionBounds.Height = TextHeight + 2;

            m_CaretBounds.X = pA.X;
            m_CaretBounds.Y = (pA.Y + 1);

            m_CaretBounds.Y += m_ScrollControl.VerticalScroll;

            m_CaretBounds.Width = 1;
            m_CaretBounds.Height = Font.Size + 2;

            Redraw();
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            base.Render(skin);

            if (ShouldDrawBackground)
                skin.DrawTextBox(this);

            if (!HasFocus) return;

            int VerticalOffset = 2 - m_ScrollControl.VerticalScroll;
            int VerticalSize = Font.Size + 6;

            // Draw selection.. if selected..
            if (m_CursorPos != m_CursorEnd)
            {
                if (StartPoint.Y == EndPoint.Y)
                {
                    Point pA = GetCharacterPosition(StartPoint);
                    Point pB = GetCharacterPosition(EndPoint);

                    Rectangle SelectionBounds = new Rectangle();
                    SelectionBounds.X = Math.Min(pA.X, pB.X);
                    SelectionBounds.Y = pA.Y - VerticalOffset;
                    SelectionBounds.Width = Math.Max(pA.X, pB.X) - SelectionBounds.X;
                    SelectionBounds.Height = VerticalSize;

                    skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
                    skin.Renderer.DrawFilledRect(SelectionBounds);
                }
                else {
                    /* Start */
                    Point pA = GetCharacterPosition(StartPoint);
                    Point pB = GetCharacterPosition(new Point(m_TextLines[StartPoint.Y].Length, StartPoint.Y));

                    Rectangle SelectionBounds = new Rectangle();
                    SelectionBounds.X = Math.Min(pA.X, pB.X);
                    SelectionBounds.Y = pA.Y - VerticalOffset;
                    SelectionBounds.Width = Math.Max(pA.X, pB.X) - SelectionBounds.X;
                    SelectionBounds.Height = VerticalSize;

                    skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
                    skin.Renderer.DrawFilledRect(SelectionBounds);

                    /* Middle */
                    for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
                    {
                        pA = GetCharacterPosition(new Point(0, StartPoint.Y + i));
                        pB = GetCharacterPosition(new Point(m_TextLines[StartPoint.Y + i].Length, StartPoint.Y + i));

                        SelectionBounds = new Rectangle();
                        SelectionBounds.X = Math.Min(pA.X, pB.X);
                        SelectionBounds.Y = pA.Y - VerticalOffset;
                        SelectionBounds.Width = Math.Max(pA.X, pB.X) - SelectionBounds.X;
                        SelectionBounds.Height = VerticalSize;

                        skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
                        skin.Renderer.DrawFilledRect(SelectionBounds);
                    }

                    /* End */
                    pA = GetCharacterPosition(new Point(0, EndPoint.Y));
                    pB = GetCharacterPosition(EndPoint);

                    SelectionBounds = new Rectangle();
                    SelectionBounds.X = Math.Min(pA.X, pB.X);
                    SelectionBounds.Y = pA.Y - VerticalOffset;
                    SelectionBounds.Width = Math.Max(pA.X, pB.X) - SelectionBounds.X;
                    SelectionBounds.Height = VerticalSize;

                    skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
                    skin.Renderer.DrawFilledRect(SelectionBounds);
                }
            }

            // Draw caret
            float time = Platform.Neutral.GetTimeInSeconds() - m_LastInputTime;

            if ((time % 1.0f) <= 0.5f)
            {
                skin.Renderer.DrawColor = Color.Black;
                skin.Renderer.DrawFilledRect(m_CaretBounds);
            }
        }

        private readonly ScrollControl m_ScrollControl;

        private Point m_CursorEnd;
        private Point m_CursorPos;
        private float m_LastInputTime;
        private bool m_SelectAll;
        private List<string> m_TextLines = new List<string>();

        private Point EndPoint
        {
            get
            {
                if (CursorPosition.Y == m_CursorEnd.Y)
                {
                    return CursorPosition.X > CursorEnd.X ? CursorPosition : CursorEnd;
                }
                else {
                    return CursorPosition.Y > CursorEnd.Y ? CursorPosition : CursorEnd;
                }
            }
        }

        private Point StartPoint
        {
            get
            {
                if (CursorPosition.Y == m_CursorEnd.Y)
                {
                    return CursorPosition.X < CursorEnd.X ? CursorPosition : CursorEnd;
                }
                else {
                    return CursorPosition.Y < CursorEnd.Y ? CursorPosition : CursorEnd;
                }
            }
        }

        //        if (CursorPosition.X > StartPos.X) {
        //            m_CursorPos.X = CursorPosition.X - length;
        //        }
        private Point GetCharacterPosition(Point CursorPosition)
        {
            if (m_TextLines.Count == 0)
            {
                return new Point(0, 0);
            }
            string CurrLine = m_TextLines[CursorPosition.Y].Substring(0, Math.Min(CursorPosition.X, m_TextLines[CursorPosition.Y].Length));

            string sub = "";
            for (int i = 0; i < CursorPosition.Y; i++)
            {
                sub += m_TextLines[i] + "\n";
            }

            Point p = new Point(Skin.Renderer.MeasureText(Font, CurrLine).X, Skin.Renderer.MeasureText(Font, sub).Y);

            return new Point(p.X + m_Text.X, p.Y + m_Text.Y + TextPadding.Top);
        }

        /// <summary>
        /// Refreshes the cursor location and selected area when the inner panel scrolls
        /// </summary>
        /// <param name="control">The inner panel the text is embedded in</param>
        private void ScrollChanged(ControlBase control, EventArgs args)
        {
            RefreshCursorBounds();
        }

        //[halfofastaple] TODO Implement this and use it. The end user can work around not having it, but it is terribly convenient.
        //	See the delete key handler for help. Eventually, the delete key should use this.
        ///// <summary>
        ///// Deletes text.
        ///// </summary>
        ///// <param name="startPos">Starting cursor position.</param>
        ///// <param name="length">Length in characters.</param>
        //public void DeleteText(Point StartPos, int length) {
        //    /* Single Line Delete */
        //    if (StartPos.X + length <= m_TextLines[StartPos.Y].Length) {
        //        string str = m_TextLines[StartPos.Y];
        //        str = str.Remove(StartPos.X, length);
        //        m_TextLines[StartPos.Y] = str;
    }
}