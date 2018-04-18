using System;
namespace linerider.Tools
{
    public static class CurrentTools
    {
        public static PencilTool PencilTool { get; private set; }
        public static EraserTool EraserTool { get; private set; }
        public static LineTool LineTool { get; private set; }
        public static MoveTool MoveTool { get; private set; }
        public static HandTool HandTool { get; private set; }
        private static Tool _selected;
        public static Tool SelectedTool
        {
            get
            {
                if (_quickpan)
                {
                    return HandTool;
                }
                return _selected;
            }
        }
        private static bool _quickpan = false;
        public static bool QuickPan
        {
            get
            {
                return _quickpan;
            }
            set
            {
                if (value != _quickpan)
                {
                    if (value == false)
                    {
                        HandTool.Stop();
                    }
                    else
                    {
                        SelectedTool.Stop();
                    }
                    _quickpan = value;
                }
            }
        }
        public static void Init()
        {
            PencilTool = new PencilTool();
            EraserTool = new EraserTool();
            LineTool = new LineTool();
            MoveTool = new MoveTool();
            HandTool = new HandTool();
            _selected = PencilTool;
        }
        public static void StopTools()
        {
            if (_quickpan)
                HandTool.Stop();
            else
                SelectedTool?.Stop();
        }
        public static void SetTool(Tool tool)
        {
            if (SelectedTool != null && tool != SelectedTool)
            {
                SelectedTool.Stop();
                SelectedTool.OnChangingTool();
            }
            if (tool == CurrentTools.HandTool)
            {
                _selected = HandTool;
                HandTool.Stop();
                _quickpan = false;
            }
            else if (tool == CurrentTools.LineTool)
            {
                _selected = LineTool;
            }
            else if (tool == CurrentTools.PencilTool)
            {
                _selected = PencilTool;
            }
            else if (tool == CurrentTools.EraserTool)
            {
                if (SelectedTool == EraserTool)
                    EraserTool.Swatch.Selected = LineType.All;
                _selected = EraserTool;
            }
            else if (tool == CurrentTools.MoveTool)
            {
                _selected = MoveTool;
            }
        }
    }
}
