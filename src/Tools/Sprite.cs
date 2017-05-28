//
//  Sprite.cs
//
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

namespace linerider.Tools
{
    public class Sprite : ControlBase
    {
        public Texture m_texture;
        public byte Alpha = 255;
        public float Rotation = 0;
        public Point RotationPoint = new Point(0, 0);
        public Sprite(ControlBase canvas)
            : base(canvas)
        {
        }

        public void SetImage(Bitmap bmp)
        {
            if (m_texture != null)
                m_texture.Dispose();

            Texture tx = new Texture(Skin.Renderer);
            Gwen.Renderer.OpenTK.LoadTextureInternal(tx, bmp);
            m_texture = tx;
        }
        protected override void Render(Gwen.Skin.SkinBase skin)
        {
            base.Render(skin);
            ((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            var trans = new Vector3d(X + RotationPoint.X, Y + RotationPoint.Y, 0);
            GL.PushMatrix();
            GL.Translate(trans);
            GL.Rotate(360 * Rotation, Vector3d.UnitZ);
            GL.Translate(-trans);
            skin.Renderer.DrawColor = Color.FromArgb(Alpha, 255, 255, 255);
            skin.Renderer.DrawTexturedRect(m_texture, RenderBounds);
            ((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            GL.PopMatrix();
        }
        public override void Dispose()
        {
            if (m_texture != null)
                m_texture.Dispose();
            base.Dispose();
        }
    }
}