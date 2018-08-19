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
    public unsafe class LineVAO : GLArray<LineVertex>
    {
        private Shader _shader;
        public float Scale = 1.0f;
        public int knobstate = 0;
        public ErrorCode Error
        {
            get
            {
                return GL.GetError();
            }
        }
        public LineVAO()
        {
            _shader = Shaders.LineShader;
        }
        public void AddLine(
            Vector2d start,
            Vector2d end,
            Color color,
            float size)
        {
            var d = end - start;
            var rad = Angle.FromVector(d);
            var line = LineRenderer.CreateTrackLine(start, end, size, Utility.ColorToRGBA_LE(color));
            foreach (var v in line)
            {
                Array.Add(v);
            }
        }
        protected override void BeginDraw()
        {
            _shader.Use();
            var in_vertex = _shader.GetAttrib("in_vertex");
            var in_color = _shader.GetAttrib("in_color");
            var in_circle = _shader.GetAttrib("in_circle");
            var in_linesize = _shader.GetAttrib("in_linesize");
            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_circle);
            GL.EnableVertexAttribArray(in_linesize);
            GL.EnableVertexAttribArray(in_color);
            fixed (float* ptr1 = &Array.unsafe_array[0].Position.X)
            fixed (byte* ptr2 = &Array.unsafe_array[0].u)
            fixed (float* ptr3 = &Array.unsafe_array[0].ratio)
            fixed (int* ptr4 = &Array.unsafe_array[0].color)
            {
                GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, LineVertex.Size, (IntPtr)ptr1);
                GL.VertexAttribPointer(in_circle, 2, VertexAttribPointerType.Byte, false, LineVertex.Size, (IntPtr)ptr2);
                GL.VertexAttribPointer(in_linesize, 2, VertexAttribPointerType.Float, false, LineVertex.Size, (IntPtr)ptr3);
                GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, LineVertex.Size, (IntPtr)ptr4);
            }
            var u_color = _shader.GetUniform("u_color");
            var u_scale = _shader.GetUniform("u_scale");
            var u_knobstate = _shader.GetUniform("u_knobstate");
            var u_alphachannel = _shader.GetUniform("u_alphachannel");
            GL.Uniform4(u_color, 0f, 0f, 0f, 0f);
            GL.Uniform1(_shader.GetUniform("u_overlay"), 0);
            GL.Uniform1(u_alphachannel, 1);
            GL.Uniform1(u_scale, Scale);
            GL.Uniform1(u_knobstate, (int)knobstate);
        }
        protected override void InternalDraw(PrimitiveType primitive)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            using (new GLEnableCap(EnableCap.Blend))
            {
                GL.DrawArrays(primitive, 0, Array.Count);
            }
        }
        protected override void EndDraw()
        {
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_vertex"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_color"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_circle"));
            GL.DisableVertexAttribArray(_shader.GetAttrib("in_linesize"));
            _shader.Stop();
        }
    }
}