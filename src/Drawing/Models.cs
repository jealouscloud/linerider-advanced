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
        const float half_uv = 0.499023438f;
        public static int SledTexture;
        public static int BodyTexture;
        public static int LimbsTexture;
        public static readonly DoubleRect SledRect = new DoubleRect(-0.6875, -2.3125, 17.9195, 8.95975);
        public static readonly DoubleRect BrokenSledRect = new DoubleRect(-0.3645, -2.3125, 17.477, 8.7385);
        public static readonly DoubleRect BodyRect = new DoubleRect(0.026, -3.145, 13.944, 6.972);
        public static readonly DoubleRect ArmRect = new DoubleRect(-0.657, -1.2305, 7.82, 3.91);
        public static readonly DoubleRect LegRect = new DoubleRect(-0.6535, -2.013, 8.02, 4.01);

        public static readonly FloatRect BodyUV = new FloatRect(0, 0, 1, half_uv);
        public static readonly FloatRect DeadBodyUV = new FloatRect(0, 0.5f, 1, 0.5f);

        public static readonly FloatRect SledUV = new FloatRect(0, 0, 1, half_uv);
        public static readonly FloatRect BrokenSledUV = new FloatRect(0, 0.5f, 1, 0.5f);

        public static readonly FloatRect ArmUV = new FloatRect(0, 0, 1, half_uv);
        public static readonly FloatRect LegUV = new FloatRect(0, 0.5f, 1, 0.5f);

        public static void LoadModels()
        {
            SledTexture = Rendering.StaticRenderer.LoadTexture(GameResources.sled_img);
            BodyTexture = Rendering.StaticRenderer.LoadTexture(GameResources.body_img);
            LimbsTexture = Rendering.StaticRenderer.LoadTexture(GameResources.limbs_img);
        }
    }
}