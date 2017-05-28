using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwen.Controls {
	public class ItemSelectedEventArgs : EventArgs {
		public ControlBase SelectedItem { get; private set; }

		internal ItemSelectedEventArgs(ControlBase selecteditem) {
			this.SelectedItem = selecteditem;
		}
	}
}
