using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
using linerider.IO;

namespace linerider.UI
{
    public class GameMenuWindow : DialogBase
    {
        protected override Margin PanelMargin
        {
            get
            {
                return Margin.Ten;
            }
        }
        public GameMenuWindow(GameCanvas parent, Editor editor) : base(parent, editor)
        {
            Title = "Game Menu";
            MinimumSize = new Size(200, MinimumSize.Height);
            AutoSizeToContents = true;

            DisableResizing();
            MakeModal(true);
            Setup();
        }
        private void Setup()
        {
            var btnmargin = new Margin(10, 5, 10, 5);
            Button save = new Button(this)
            {
                Text = "Save Track",
                Dock = Dock.Top,
                Margin = new Margin(10, 5, 10, 5)
            };
            Button load = new Button(this)
            {
                Text = "Load Track",
                Dock = Dock.Top,
                Margin = btnmargin
            };
            Button props = new Button(this)
            {
                Text = "Preferences",
                Dock = Dock.Top,
                Margin = btnmargin
            };
            Button trackprops = new Button(this)
            {
                Text = "Track Properties",
                Dock = Dock.Top,
                Margin = btnmargin
            };
            props.Clicked += (o, e) =>
            {
                _canvas.ShowPreferencesDialog(); 
                Close();
            };
            trackprops.Clicked += (o, e) =>
            {
                _canvas.ShowTrackPropertiesDialog(); 
                Close();
            };
            load.Clicked += (o, e) =>
            {
                _canvas.ShowLoadDialog(); 
                Close();
            };
            save.Clicked += (o, e) =>
            {
                _canvas.ShowSaveDialog(); 
                Close();
            };
        }
    }
}
