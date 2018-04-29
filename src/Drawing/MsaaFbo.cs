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

//#define nomsaa
using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
namespace linerider.Drawing
{
    public class MsaaFbo : GameService
    {
        private static int MSAA = 0;
        public readonly int Framebuffer;
        private int _renderbuffer;
        private int _stencilbuffer;
        public int Width;
        public int Height;
        public MsaaFbo()
        {
            Framebuffer = SafeFrameBuffer.GenFramebuffer();
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
            _renderbuffer = SafeFrameBuffer.GenRenderbuffer();
            _stencilbuffer = SafeFrameBuffer.GenRenderbuffer();

            MSAA = Math.Min(8, GL.GetInteger(GetPName.MaxSamples));
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        private void ErrorCheck()
        {
            var err = GL.GetError();
            if (err != ErrorCode.NoError)
            {
                System.Diagnostics.Debug.WriteLine("GL Error: " + err);
            }
            else
            {
                var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status != FramebufferErrorCode.FramebufferComplete)
                {
                    System.Diagnostics.Debug.WriteLine("Framebuffer Error: " + status);
                }
            }
        }
        public void Use(int width, int height)
        {
#if nomsaa
            return;
#else
            if (width <= 0 || height <= 0)
                return;
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
            if (width != Width || height != Height)
            {
                Width = width;
                Height = height;

                SafeFrameBuffer.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderbuffer);
                SafeFrameBuffer.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer,MSAA, RenderbufferStorage.Rgba8, width, height);
                
                SafeFrameBuffer.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _stencilbuffer);
                SafeFrameBuffer.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, MSAA,RenderbufferStorage.Depth24Stencil8,width,height);

                SafeFrameBuffer.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, _stencilbuffer);
                
                SafeFrameBuffer.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _renderbuffer);
                SafeFrameBuffer.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                ErrorCheck();
            }
            ErrorCheck();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
#endif
        }
        public void End()
        {
#if nomsaa
            return;
#else
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);//default
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Framebuffer);
            SafeFrameBuffer.BlitFramebuffer(0, 0, Width, Height,
                               0, 0, Width, Height,
                               ClearBufferMask.ColorBufferBit,
                               BlitFramebufferFilter.Linear); 
            ErrorCheck();
            SafeFrameBuffer.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
#endif
        }
    }
}
