using System;
using Gwen;
using Gwen.Controls;
namespace linerider.UI
{
    public static class GwenHelper
    {
        public static CheckProperty AddPropertyCheckbox(PropertyTable prop, string label, bool value)
        {
            var check = new CheckProperty(null);
            prop.Add(label, check);
            check.IsChecked = value;
            return check;
        }
        public static Checkbox AddCheckbox(ControlBase parent, string text, bool val, ControlBase.GwenEventHandler<EventArgs> checkedchanged, Dock dock = Dock.Top)
        {
            Checkbox check = new Checkbox(parent)
            {
                Dock = dock,
                Text = text,
                IsChecked = val,
            };
            check.CheckChanged += checkedchanged;
            return check;
        }
        public static Panel CreateHeaderPanel(ControlBase parent, string headertext)
        {
            var canvas = (GameCanvas)parent.GetCanvas();
            Panel panel = new Panel(parent)
            {
                Dock = Dock.Top,
                Children =
                {
                    new Label(parent)
                    {
                        Dock = Dock.Top,
                        Text = headertext,
                        Alignment = Pos.Left | Pos.CenterV,
                        Font = canvas.Fonts.DefaultBold,
                        Margin = new Margin(-10, 5, 0, 5)
                    }
                },
                AutoSizeToContents = true,
                Margin = new Margin(0, 0, 0, 10),
                Padding = new Padding(10, 0, 0, 0),
                ShouldDrawBackground = false
            };
            return panel;
        }
        public static ControlBase CreateLabeledControl(ControlBase parent, string label, ControlBase control)
        {
            control.Dock = Dock.Right;
            ControlBase container = new ControlBase(parent)
            {
                Children =
                {
                    new Label(null)
                    {
                        Text = label,
                        Dock = Dock.Left,
                        Alignment = Pos.Left | Pos.CenterV,
                        Margin = new Margin(0,0,10,0)
                    },
                    control
                },
                AutoSizeToContents = true,
                Dock = Dock.Top,
                Margin = new Margin(0, 1, 0, 1)
            };
            return container;
        }
        public static ComboBox CreateLabeledCombobox(ControlBase parent, string label)
        {
            var combobox = new ComboBox(null)
            {
                Dock = Dock.Right,
                Width = 100
            };
            ControlBase container = new ControlBase(parent)
            {
                Children =
                {
                    new Label(null)
                    {
                        Text = label,
                        Dock = Dock.Left,
                        Alignment = Pos.Left | Pos.CenterV,
                        Margin = new Margin(0,0,10,0)
                    },
                    combobox
                },
                AutoSizeToContents = true,
                Dock = Dock.Top,
                Margin = new Margin(0, 1, 0, 1)
            };
            return combobox;
        }
    }

}