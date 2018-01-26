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
using OpenTK;
using linerider.Utils;

namespace linerider
{
    public static class Models
    {
        #region Fields
        public static int SledTexture;
        public static int BrokenSledTexture;
        public static int BodyTexture;
        public static int BodyDeadTexture;
        public static int ArmTexture;
        public static int LegTexture;
        public static readonly DoubleRect SledRect = new DoubleRect(-1.375, -4.625, 35.839, 17.9195);
        public static readonly DoubleRect BrokenSledRect = new DoubleRect(-1.375 + 0.646, -4.625, 34.954, 17.477);
        public static readonly DoubleRect BodyRect = new DoubleRect(-0.052, -6.29, 27.888, 13.944);
        public static readonly DoubleRect ArmRect = new DoubleRect(-1.314, -2.461, 15.640, 7.82);
        public static readonly DoubleRect LegRect = new DoubleRect(-1.307, -4.026, 16.040, 8.02);
        #endregion Fields

        #region Methods

        public static void LoadModels()
        {
            SledTexture = Drawing.StaticRenderer.LoadTexture(GameResources.sled_img);
            BrokenSledTexture = Drawing.StaticRenderer.LoadTexture(GameResources.brokensled_img);
            BodyTexture = Drawing.StaticRenderer.LoadTexture(GameResources.bosh_img);
            BodyDeadTexture = Drawing.StaticRenderer.LoadTexture(GameResources.boshdead_img);
            ArmTexture = Drawing.StaticRenderer.LoadTexture(GameResources.arm_img);
            LegTexture = Drawing.StaticRenderer.LoadTexture(GameResources.leg_img);

        }

        #endregion Methods
    }
}