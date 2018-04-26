//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Drawing;
using Gwen;
using Gwen.Controls;
using linerider.Tools;

namespace linerider.UI
{
    public class Toolbar : WidgetContainer
    {
        private ImageButton _pencilbtn;
        private ImageButton _linebtn;
        private ImageButton _eraserbtn;
        private ImageButton _selectbtn;
        private ImageButton _handbtn;
        private ImageButton _start;
        private ImageButton _pause;
        private ImageButton _stop;
        private ImageButton _flag;
        private ImageButton _menu;
        private ColorSwatch _swatch;
        private ControlBase _buttoncontainer;
        private Editor _editor;
        private GameCanvas _canvas;
        public Toolbar(ControlBase parent, Editor editor) : base(parent)
        {
            _canvas = (GameCanvas)parent.GetCanvas();
            MouseInputEnabled = false;
            AutoSizeToContents = true;
            ShouldDrawBackground = true;
            _editor = editor;
            MakeButtons();
            SetupEvents();
            OnThink += Think;
        }
        private void Think(object sender, EventArgs e)
        {
            var rec = IO.TrackRecorder.Recording;
            _swatch.IsHidden = rec;
        }
        private void MakeButtons()
        {
            _buttoncontainer = new ControlBase(this)
            {
                Dock = Dock.Top,
                AutoSizeToContents = true,
            };
            _swatch = new ColorSwatch(this);
            _swatch.Dock = Dock.Top;
            _pencilbtn = CreateTool(GameResources.pencil_icon, "Pencil Tool (Q)");
            _linebtn = CreateTool(GameResources.line_icon, "Line Tool (W)");
            _eraserbtn = CreateTool(GameResources.eraser_icon, "Eraser Tool (E)");
            _selectbtn = CreateTool(GameResources.movetool_icon, "Line Adjustment Tool (R)");
            _handbtn = CreateTool(GameResources.pantool_icon, "Hand Tool (Space) (T)");
            _start = CreateTool(GameResources.play_icon, "Start (Y)");
            _pause = CreateTool(GameResources.pause, "Pause (Space)");
            _stop = CreateTool(GameResources.stop_icon, "Stop (U)");
            _flag = CreateTool(GameResources.flag_icon, "Flag (I)");
            _menu = CreateTool(GameResources.menu_icon, "");
        }
        private void SetupEvents()
        {
            _pencilbtn.Clicked += (o, e) => CurrentTools.SetTool(CurrentTools.PencilTool);
            _linebtn.Clicked += (o, e) => CurrentTools.SetTool(CurrentTools.LineTool);
            _eraserbtn.Clicked += (o, e) => CurrentTools.SetTool(CurrentTools.EraserTool);
            _selectbtn.Clicked += (o, e) => CurrentTools.SetTool(CurrentTools.MoveTool);
            _handbtn.Clicked += (o, e) => CurrentTools.SetTool(CurrentTools.HandTool);
            _flag.Clicked += (o, e) =>
            {
                _editor.Flag(_editor.Offset);
            };
            // _pause.IsHidden = true;
            _start.Clicked += (o, e) =>
            {
                CurrentTools.StopTools();
                if (UI.InputUtils.Check(Hotkey.PlayButtonIgnoreFlag))
                {
                    _editor.StartIgnoreFlag();
                }
                else
                {
                    if (_editor.Paused)
                        _editor.TogglePause();
                    else
                        _editor.StartFromFlag();
                }
                _editor.Scheduler.DefaultSpeed();
                _pause.IsHidden = false;
                _start.IsHidden = true; ;
            };
            _stop.Clicked += (o, e) =>
            {
                CurrentTools.StopTools();
                _editor.Stop();
            };
            _pause.Clicked += (o, e) =>
            {
                CurrentTools.StopTools();
                _editor.TogglePause();
                _pause.IsHidden = true;
                _start.IsHidden = false;
            };
            _menu.Clicked += (o, e) =>
            {
                Menu menu = new Menu(_canvas);
                menu.AddItem("Save").Clicked += (o2, e2) => { _canvas.ShowSaveDialog(); };
                menu.AddItem("Load").Clicked += (o2, e2) => { _canvas.ShowLoadDialog(); };
                menu.AddItem("New").Clicked += (o2, e2) =>
                {
                    if (_editor.TrackChanges != 0)
                    {
                        var save = MessageBox.Show(
                            _canvas,
                            "You are creating a new track.\nDo you want to save your current changes?",
                            "Create New Track",
                            MessageBox.ButtonType.YesNoCancel);
                        save.RenameButtonsYN("Save", "Discard", "Cancel");
                        save.Dismissed += (o3, result) =>
                          {
                              switch (result)
                              {
                                  case DialogResult.Yes:
                                      _canvas.ShowSaveDialog();
                                      break;
                                  case DialogResult.No:
                                      NewTrack();
                                      break;
                              }
                          };
                    }
                    else
                    {
                        var msg = MessageBox.Show(
                            _canvas,
                            "Are you sure you want to create a new track?",
                            "Create New Track",
                            MessageBox.ButtonType.OkCancel);
                        msg.RenameButtons("Create");
                        msg.Dismissed += (o3, result) =>
                          {
                              if (result == DialogResult.OK)
                              {
                                  NewTrack();
                              }
                          };
                    }
                };
                menu.AddItem("Preferences").Clicked += (o2, e2) => _canvas.ShowPreferencesDialog();
                menu.AddItem("Track Properties").Clicked += (o2, e2) => _canvas.ShowTrackPropertiesDialog();
                menu.AddItem("Export Video").Clicked += (o2, e2) => _canvas.ShowExportVideoWindow();
                var canvaspos = LocalPosToCanvas(new Point(_menu.X, _menu.Y));
                menu.SetPosition(canvaspos.X, canvaspos.Y + 32);
                menu.Show();
            };
        }
        private void NewTrack()
        {
            _editor.Stop();
            _editor.ChangeTrack(new Track() { Name = Utils.Constants.DefaultTrackName });
            Settings.LastSelectedTrack = "";
            Settings.Save();
            _editor.Invalidate();
        }
        protected override void PostLayout()
        {
            var w = Width;
            if (Parent.Width > w)
            {
                X = (Parent.Width / 2) - w / 2;
            }
            else
            {
                X = 0;
            }
            base.PostLayout();
        }
        public override void Think()
        {
            bool showplay = !_editor.Playing;
            _pause.IsHidden = showplay;
            _start.IsHidden = !showplay;
            base.Think();
        }
        private ImageButton CreateTool(Bitmap image, string tooltip)
        {
            ImageButton btn = new ImageButton(_buttoncontainer);
            btn.Dock = Dock.Left;
            btn.SetImage(image);
            btn.SetSize(32, 32);
            btn.Tooltip = tooltip;
            return btn;
        }
    }
}