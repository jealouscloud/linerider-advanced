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

using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using System.Drawing;
namespace linerider.Drawing
{
    /// <summary>
    /// A generic vertex with values for pos, color, and texture uv
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GenericVertex
    {
        public static readonly int Size = Marshal.SizeOf(typeof(GenericVertex));
        public Vector2 Position;
        public byte r, g, b, a;
        public float u, v;

        public GenericVertex(float x, float y, Color color, float _u = 0.5f, float _v = 0.5f)
        {
            Position = new Vector2(x, y);
            r = color.R;
            g = color.G;
            b = color.B;
            a = color.A;
            u = _u;
            v = _v;
        }

        public GenericVertex(Vector2 pos, Color color, float _u = 0.5f, float _v = 0.5f)
        {
            Position = pos;
            u = _u;
            v = _v;
            r = color.R;
            g = color.G;
            b = color.B;
            a = color.A;
        }
		public GenericVertex SetColor(Color color)
		{
			var ret = this;
			ret.r = color.R;
			ret.g = color.G;
			ret.b = color.B;
			ret.a = color.A;
			return ret;
		}
    }
}