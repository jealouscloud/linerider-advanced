//
//  ImageButton.cs
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

using System.Drawing;
using Gwen;
using Gwen.Controls;

namespace linerider.UI
{
    public class ImageButton : Button
    {
        #region Fields

        public byte Alpha = 255;
        private Texture tx1;
        private Texture tx2;

        #endregion Fields

        #region Constructors

        public ImageButton(ControlBase canvas) : base(canvas) { }

        #endregion Constructors

        #region Methods

        public override void Dispose()
        {
            if (tx1 != null)
                tx1.Dispose();
            if (tx2 != null)
                tx2.Dispose();
            base.Dispose();
        }
        public void Nightmode(bool on)
        {
            if (on && tx2 != null)
            {
                m_texture = tx2;
            }
            else
            {
                m_texture = tx1;
            }
            Invalidate();
        }
        public void SetImage(Bitmap bmp, Bitmap bmp2)
        {
            if (m_texture != null)
                m_texture.Dispose();

            Texture tx = new Texture(Skin.Renderer);

            Gwen.Renderer.OpenTK.LoadTextureInternal(tx, bmp);
            m_texture = tx;
            tx1 = tx;
            if (bmp2 != null)
            {
                tx2 = new Texture(Skin.Renderer);
                Gwen.Renderer.OpenTK.LoadTextureInternal(tx2, bmp2);
            }
        }

        public override void SetImage(string textureName, bool center = false)
        {
            if (m_texture != null)
                m_texture.Dispose();
            m_texture = new Texture(Skin.Renderer);
            m_texture.Load(textureName);
        }

        protected override void Render(Gwen.Skin.SkinBase skin)
        {
            skin.Renderer.DrawColor = Color.FromArgb(IsDepressed ? 64 : (IsHovered ? 128 : Alpha), 255, 255, 255);

            skin.Renderer.DrawTexturedRect(m_texture, RenderBounds);
        }

        #endregion Methods

        private Texture m_texture;
    }
}