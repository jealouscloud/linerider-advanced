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
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace linerider.UI
{
    public class LoadingSprite : Sprite
    {
        public LoadingSprite(ControlBase canvas)
            : base(canvas)
        {
            IsTabable = false;
            KeyboardInputEnabled = false;
            MouseInputEnabled = false;
        }
        protected override void Render(Gwen.Skin.SkinBase skin)
        {
            ((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            var rotation = (Environment.TickCount % 1000) / 1000f;
            var trans = new Vector3d(X + (Width / 2), Y + (Height / 2), 0);
            GL.PushMatrix();
            GL.Translate(trans);
            GL.Rotate(360 * rotation, Vector3d.UnitZ);
            GL.Translate(-trans);
            skin.Renderer.DrawColor = Color.FromArgb(Alpha, 255, 255, 255);
            skin.Renderer.DrawTexturedRect(m_texture, RenderBounds);
            ((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            GL.PopMatrix();
        }
    }
}