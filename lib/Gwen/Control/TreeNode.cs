using System;
using System.Linq;
using Gwen.ControlInternal;
using System.Collections.Generic;

namespace Gwen.Controls
{
    /// <summary>
    /// Tree control node.
    /// </summary>
    public class TreeNode : ControlBase
    {
        public const int TreeIndentation = 14;

        protected TreeControl m_TreeControl;
        protected Button m_ToggleButton;
        protected Button m_Title;
        private bool m_Root;
        private bool m_Selected;
        private bool m_Selectable;

        /// <summary>
        /// Indicates whether this is a root node.
        /// </summary>
        public bool IsRoot { get { return m_Root; } set { m_Root = value; } }

        /// <summary>
        /// Parent tree control.
        /// </summary>
        public TreeControl TreeControl { get { return m_TreeControl; } set { m_TreeControl = value; } }

        /// <summary>
        /// Determines whether the node is selectable.
        /// </summary>
        public bool IsSelectable { get { return m_Selectable; } set { m_Selectable = value; } }

        /// <summary>
        /// Indicates whether the node is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return m_Selected; }
            set
            {
                if (!IsSelectable)
                    return;
                if (IsSelected == value)
                    return;

                m_Selected = value;

                if (m_Title != null)
                    m_Title.ToggleState = value;

                if (SelectionChanged != null)
					SelectionChanged.Invoke(this, EventArgs.Empty);

                // propagate to root parent (tree)
                if (m_TreeControl != null && m_TreeControl.SelectionChanged != null)
					m_TreeControl.SelectionChanged.Invoke(this, EventArgs.Empty);

                if (value)
                {
                    if (Selected != null)
						Selected.Invoke(this, EventArgs.Empty);

                    if (m_TreeControl != null && m_TreeControl.Selected != null)
						m_TreeControl.Selected.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (Unselected != null)
						Unselected.Invoke(this, EventArgs.Empty);

                    if (m_TreeControl != null && m_TreeControl.Unselected != null)
						m_TreeControl.Unselected.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Node's label.
        /// </summary>
        public string Text { get { return m_Title.Text; } set { m_Title.Text = value; } }

        /// <summary>
        /// Invoked when the node label has been pressed.
        /// </summary>
		public event GwenEventHandler<EventArgs> LabelPressed;

        /// <summary>
        /// Invoked when the node's selected state has changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> SelectionChanged;

        /// <summary>
        /// Invoked when the node has been selected.
        /// </summary>
		public event GwenEventHandler<EventArgs> Selected;

        /// <summary>
        /// Invoked when the node has been unselected.
        /// </summary>
		public event GwenEventHandler<EventArgs> Unselected;

        /// <summary>
        /// Invoked when the node has been expanded.
        /// </summary>
		public event GwenEventHandler<EventArgs> Expanded;

        /// <summary>
        /// Invoked when the node has been collapsed.
        /// </summary>
		public event GwenEventHandler<EventArgs> Collapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public TreeNode(ControlBase parent)
            : base(parent)
        {
            m_ToggleButton = new TreeToggleButton(this);
            m_ToggleButton.SetBounds(0, 0, 15, 15);
            m_ToggleButton.Toggled += OnToggleButtonPress;

            m_Title = new TreeNodeLabel(this);
            m_Title.Dock = Pos.Top;
            m_Title.Margin = new Margin(16, 0, 0, 0);
            m_Title.DoubleClicked += OnDoubleClickName;
            m_Title.Clicked += OnClickName;

            m_InnerPanel = new ControlBase(this);
            m_InnerPanel.Dock = Pos.Top;
            m_InnerPanel.Height = 100;
            m_InnerPanel.Margin = new Margin(TreeIndentation, 1, 0, 0);
            m_InnerPanel.Hide();

			m_Root = parent is TreeControl;
            m_Selected = false;
            m_Selectable = true;

			Dock = Pos.Top;
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            int bottom = 0;
            if (m_InnerPanel.Children.Count > 0)
            {
                bottom = m_InnerPanel.Children.Last().Y + m_InnerPanel.Y;
            }

            skin.DrawTreeNode(this, m_InnerPanel.IsVisible, IsSelected, m_Title.Height, m_Title.TextRight,
                (int)(m_ToggleButton.Y + m_ToggleButton.Height * 0.5f), bottom, m_TreeControl == Parent); // IsRoot

            //[halfofastaple] HACK - The treenodes are taking two passes until their height is set correctly,
            //  this means that the height is being read incorrectly by the parent, causing
            //  the TreeNode bug where nodes get hidden when expanding and collapsing.
            //  The hack is to constantly invalide TreeNodes, which isn't bad, but there is
            //  definitely a better solution (possibly: Make it set the height from childmost
            //  first and work it's way up?) that invalidates and draws properly in 1 loop.
            this.Invalidate();
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            if (m_ToggleButton != null)
            {
                if (m_Title != null)
                {
                    m_ToggleButton.SetPosition(0, (m_Title.Height - m_ToggleButton.Height)*0.5f);
                }

                if (m_InnerPanel.Children.Count == 0)
                {
                    m_ToggleButton.Hide();
                    m_ToggleButton.ToggleState = false;
                    m_InnerPanel.Hide();
                }
                else
                {
                    m_ToggleButton.Show();
                    m_InnerPanel.SizeToChildren(false, true);
                }
            }

            base.Layout(skin);
        }

        /// <summary>
        /// Function invoked after layout.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void PostLayout(Skin.SkinBase skin)
        {
            if (SizeToChildren(false, true))
            {
                InvalidateParent();
            }
        }

        /// <summary>
        /// Adds a new child node.
        /// </summary>
        /// <param name="label">Node's label.</param>
        /// <returns>Newly created control.</returns>
        public TreeNode AddNode(string label)
        {
            TreeNode node = new TreeNode(this);
            node.Text = label;

            return node;
        }

        /// <summary>
        /// Opens the node.
        /// </summary>
        public void Open()
        {
            m_InnerPanel.Show();
            if (m_ToggleButton != null)
                m_ToggleButton.ToggleState = true;

            if (Expanded != null)
				Expanded.Invoke(this, EventArgs.Empty);
            if (m_TreeControl != null && m_TreeControl.Expanded != null)
				m_TreeControl.Expanded.Invoke(this, EventArgs.Empty);

            Invalidate();
        }

        /// <summary>
        /// Closes the node.
        /// </summary>
        public void Close()
        {
            m_InnerPanel.Hide();
            if (m_ToggleButton != null)
                m_ToggleButton.ToggleState = false;

            if (Collapsed != null)
				Collapsed.Invoke(this, EventArgs.Empty);
            if (m_TreeControl != null && m_TreeControl.Collapsed != null)
				m_TreeControl.Collapsed.Invoke(this, EventArgs.Empty);

            Invalidate();
        }

        /// <summary>
        /// Opens the node and all child nodes.
        /// </summary>
        public void ExpandAll()
        {
            Open();
            foreach (ControlBase child in Children)
            {
                TreeNode node = child as TreeNode;
                if (node == null)
                    continue;
                node.ExpandAll();
            }
        }

        /// <summary>
        /// Clears the selection on the node and all child nodes.
        /// </summary>
        public void UnselectAll()
        {
            IsSelected = false;
            if (m_Title != null)
                m_Title.ToggleState = false;

            foreach (ControlBase child in Children)
            {
                TreeNode node = child as TreeNode;
                if (node == null)
                    continue;
                node.UnselectAll();
            }
        }

        /// <summary>
        /// Handler for the toggle button.
        /// </summary>
        /// <param name="control">Event source.</param>
		protected virtual void OnToggleButtonPress(ControlBase control, EventArgs args)
        {
            if (m_ToggleButton.ToggleState)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Handler for label double click.
        /// </summary>
        /// <param name="control">Event source.</param>
		protected virtual void OnDoubleClickName(ControlBase control, EventArgs args)
        {
            if (!m_ToggleButton.IsVisible)
                return;
            m_ToggleButton.Toggle();
        }

        /// <summary>
        /// Handler for label click.
        /// </summary>
        /// <param name="control">Event source.</param>
		protected virtual void OnClickName(ControlBase control, EventArgs args)
        {
            if (LabelPressed != null)
                LabelPressed.Invoke(this, EventArgs.Empty);
            IsSelected = !IsSelected;
        }

        public void SetImage(string textureName) 
        {
            m_Title.SetImage(textureName);
        }

		protected override void OnChildAdded(ControlBase child) {
			TreeNode node = child as TreeNode;
			if (node != null) {
				node.TreeControl = m_TreeControl;

				if (m_TreeControl != null) {
					m_TreeControl.OnNodeAdded(node);
				}
			}

			base.OnChildAdded(child);
		}

		public override event GwenEventHandler<ClickedEventArgs> Clicked
        { 
            add {
                m_Title.Clicked += delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
            }
            remove {
				m_Title.Clicked -= delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
            }
        }

		public override event GwenEventHandler<ClickedEventArgs> DoubleClicked 
        { 
            add {
				if (value != null) {
					m_Title.DoubleClicked += delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
				}
            }
            remove {
				m_Title.DoubleClicked -= delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
            }
        }

		public override event GwenEventHandler<ClickedEventArgs> RightClicked {
			add {
				m_Title.RightClicked += delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
			}
			remove {
				m_Title.RightClicked -= delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
			}
		}

		public override event GwenEventHandler<ClickedEventArgs> DoubleRightClicked {
			add {
				if (value != null) {
					m_Title.DoubleRightClicked += delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
				}
			}
			remove {
				m_Title.DoubleRightClicked -= delegate(ControlBase sender, ClickedEventArgs args) { value(this, args); };
			}
		}

		public IEnumerable<TreeNode> SelectedChildren
		{
			get {
				List<TreeNode> Trees = new List<TreeNode>();

				foreach (ControlBase child in Children) {
					TreeNode node = child as TreeNode;
					if (node == null)
						continue;
					Trees.AddRange(node.SelectedChildren);
				}

				if (this.IsSelected) {
					Trees.Add(this);
				}

				return Trees;
			}
		}
    }
}
