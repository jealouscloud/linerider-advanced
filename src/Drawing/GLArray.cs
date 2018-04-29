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
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK;
using linerider.Utils;
using linerider.Rendering;

namespace linerider.Drawing
{
    public abstract class GLArray<T>
    where T : struct
    {
        public AutoArray<T> Array = new AutoArray<T>(500);
        public GLArray()
        {
        }
        public void AddVertex(T vertex)
        {
            Array.Add(vertex);
        }
        public void Draw(PrimitiveType primitive)
        {
            BeginDraw();
            InternalDraw(primitive);
            EndDraw();
        }
        public void Clear()
        {
            Array.Clear();
        }
        protected virtual void BeginDraw()
        {
        }
        protected virtual void InternalDraw(PrimitiveType primitive)
        {
        }
        protected virtual void EndDraw()
        {
        }
    }
}