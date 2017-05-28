//
//  Models.cs
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

namespace linerider
{
    public static class Models
    {
        #region Fields

        public static Drawing.VBO Arm;
        public static Drawing.VBO Bosh;
        public static Drawing.VBO BoshDead;
        public static Drawing.VBO BrokenSled;
        public static Drawing.VBO Leg;
        public static Drawing.VBO Sled;
        #endregion Fields

        #region Methods

        public static void LoadModels()
        {
            Bosh = LoadGraphic(GameResources.bosh);
            BoshDead = LoadGraphic(GameResources.boshdead);
            Arm = LoadGraphic(GameResources.arm);
            Leg = LoadGraphic(GameResources.leg);
            Sled = LoadGraphic(GameResources.sled);
            BrokenSled = LoadGraphic(GameResources.brokensled);
        }
        private static Drawing.VBO LoadGraphic(byte[] graphics)
        {
            using (var s = new System.IO.MemoryStream(graphics))
            using (var sr = new System.IO.StreamReader(s, System.Text.Encoding.ASCII))
            {
                var svg = NGraphics.Graphic.LoadSvg(sr);
                var vbo = new Drawing.VBO(false,true);
                vbo.Texture = linerider.Drawing.StaticRenderer.CircleTex;
                foreach (var c in svg.Children)
                {
                    if (c is NGraphics.Path)
                    {
                        linerider.Drawing.GameRenderer.canvas.DrawPath((NGraphics.Path)c,vbo);
                    }
                    else
                    {
                        throw new System.Exception("Unsupported SVG");
                    }
                }
                svg.Size.Height /= 2;
                svg.Size.Width /=2;
                return vbo;
            }
        }

        #endregion Methods
    }
}