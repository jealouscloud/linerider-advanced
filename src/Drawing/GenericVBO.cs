using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using System.Drawing;
using linerider.Rendering;

namespace linerider.Drawing
{
    public class GenericVBO : VBO<GenericVertex>
    {
        private List<byte> alphas = new List<byte>();

        public int Texture = 0;
        public bool Opacity = false; private float _opacity = 1.0f;
        public float GetOpacity
        {
            get
            {
                return _opacity;
            }
        }
        private GLEnableCap _texcap = null;
        private GLEnableCap _blendcap = null;
        public GenericVBO(bool indexed, bool useopacity) : base(indexed, GenericVertex.Size)
        {
        }
        public void SetOpacity(float opacity)
        {
            if (!Opacity)
                throw new InvalidOperationException("Opacity isnt supported in this vbo");
            if (_opacity == opacity)
                return;
            if (alphas == null || alphas.Count == 0)
            {
                alphas = new List<byte>(vCount);
                for (int i = 0; i < vCount; i++)
                {
                    alphas.Add(vertices[i].a);
                }
            }
            for (int i = 0; i < vCount; i++)
            {
                var v = vertices[i];
                v.a = (byte)(Math.Min(255, alphas[i] * opacity));
                vertices[i] = v;
            }
            _opacity = opacity;
            UpdateVertices();
        }
        public override void Clear()
        {
            if (Opacity)
                alphas.Clear();
            base.Clear();

        }

        public override int AddVertex(GenericVertex v)
        {
            if (Opacity)
                alphas.Add(v.a);
            return base.AddVertex(v);
        }

        protected override void BeginDraw()
        {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.VertexPointer(2, VertexPointerType.Float, GenericVertex.Size, 0);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, GenericVertex.Size, 8);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, GenericVertex.Size, 12);
            if (Texture != 0)
            {
                _texcap = new GLEnableCap(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, Texture);
            }
            _blendcap = new GLEnableCap(EnableCap.Blend);
            base.BeginDraw();
        }
        protected override void EndDraw()
        {

            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            if (Texture != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            _texcap?.Dispose();
            _blendcap?.Dispose();
            _texcap = null;
            _blendcap = null;
            base.EndDraw();
        }
    }
}