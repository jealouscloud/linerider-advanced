using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;

namespace linerider.UI
{
    public abstract class DialogBase : WindowControl
    {
        protected GameCanvas _canvas;
        protected Editor _editor;
        public DialogBase(GameCanvas parent, Editor editor) : base(parent)
        {
            DeleteOnClose = true;
            _canvas = parent;
            _editor = editor;
        }
    }
}
