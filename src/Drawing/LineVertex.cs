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
    /// A vertex meant for the simulation line shader
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LineVertex
    {
        public static readonly int Size = Marshal.SizeOf(typeof(LineVertex));
        public Vector2 Position;
        public int color;
        // shorts for alignment or something
        // who knows
        /// <summary>
        /// 0 or 1
        /// </summary>
        public byte u;
        /// <summary>
        /// 0 or 1
        /// </summary>
        public byte v;
        public byte selectionflags;
        public byte reserved;
        public float ratio;
        public float scale;
    }
}