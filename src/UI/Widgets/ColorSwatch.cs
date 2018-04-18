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

using System.Drawing;
using Gwen;
using Gwen.Controls;
using linerider.Tools;
using linerider.Utils;
namespace linerider.UI
{
    public class ColorSwatch : WidgetContainer
    {
        private SwatchColorButton _btnblue;
        private SwatchColorButton _btnred;
        private SwatchColorButton _btngreem;
        private Texture _swatchtexture;
        public ColorSwatch(ControlBase canvas) : base(canvas)
        {
            AutoSizeToContents = true;
            ShouldDrawBackground = false;
            Setup();
        }
        public override void Think()
        {
            base.Think();
            var hide = !Tools.CurrentTools.SelectedTool.ShowSwatch;
            foreach (var child in Children)
            {
                child.IsHidden = hide;
            }
        }
        private SwatchColorButton AddButton(LineType lineType)
        {
            var ret = new SwatchColorButton(this, lineType)
            {
                Dock = Dock.Left,
                Margin = Margin.One
            };
            ret.SetImage(_swatchtexture);
            return ret;
        }
        private void Setup()
        {
            _swatchtexture = Skin.Renderer.CreateTexture(GameResources.swatch);
            _btnblue = AddButton(LineType.Blue);
            _btnred = AddButton(LineType.Red);
            _btngreem = AddButton(LineType.Scenery);
        }
        public override void Dispose()
        {
            base.Dispose();
            _swatchtexture.Dispose();
        }
    }
}