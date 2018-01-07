using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Gwen.Renderer
{
    public class OpenTK : RendererBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vertex
        {
            public short x, y;
            public float u, v;
            public byte r, g, b, a;
        }

        #region Properties

        public int DrawCallCount { get { return m_DrawCallCount; } }

        public override Color DrawColor
        {
            get { return m_Color; }
            set
            {
                m_Color = value;
            }
        }

        public int VertexCount { get { return m_VertNum; } }

        #endregion Properties
        #region Fields
        private const int MaxVerts = 1024;
        static private int m_LastTextureID;
        private readonly int m_VertexSize;
        private readonly Vertex[] m_Vertices;
        private bool m_ClipEnabled;
        private Color m_Color;

        // only used for text measurement
        private int m_DrawCallCount;

        private float m_PrevAlphaRef;
        private int m_PrevBlendSrc, m_PrevBlendDst, m_PrevAlphaFunc;
        private bool m_RestoreRenderState;
        private StringFormat m_StringFormat;
        private bool m_TextureEnabled;
        private int m_VertNum;
        private bool m_WasBlendEnabled, m_WasTexture2DEnabled, m_WasDepthTestEnabled;

        #endregion Fields
        #region Constructors

        public OpenTK(bool restoreRenderState = true)
            : base()
        {
            m_Vertices = new Vertex[MaxVerts];
            m_VertexSize = Marshal.SizeOf(m_Vertices[0]);
            m_StringFormat = new StringFormat(StringFormat.GenericTypographic);
            m_StringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            m_RestoreRenderState = restoreRenderState;
        }

        #endregion Constructors

        #region Methods

        public static void LoadTextureInternal(Texture t, Bitmap bmp)
        {
            // todo: convert to proper format
            PixelFormat lock_format = PixelFormat.Undefined;
            switch (bmp.PixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                    lock_format = PixelFormat.Format32bppArgb;
                    break;

                case PixelFormat.Format24bppRgb:
                    lock_format = PixelFormat.Format32bppArgb;
                    break;

                default:
                    t.Failed = true;
                    return;
            }

            int glTex;

            // Create the opengl texture
            GL.GenTextures(1, out glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);
            m_LastTextureID = glTex;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Sort out our GWEN texture
            t.RendererData = glTex;
            t.Width = bmp.Width;
            t.Height = bmp.Height;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, lock_format);

            switch (lock_format)
            {
                case PixelFormat.Format32bppArgb:
                    byte[] etc = new byte[100 * 4];
                    Marshal.Copy(data.Scan0, etc, 0, 400);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, t.Width, t.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    break;

                default:
                    // invalid
                    break;
            }

            bmp.UnlockBits(data);
        }

        public override void Begin()
        {
            if (m_RestoreRenderState)
            {
                // Get previous parameter values before changing them.
                GL.GetInteger(GetPName.BlendSrc, out m_PrevBlendSrc);
                GL.GetInteger(GetPName.BlendDst, out m_PrevBlendDst);
                GL.GetInteger(GetPName.AlphaTestFunc, out m_PrevAlphaFunc);
                GL.GetFloat(GetPName.AlphaTestRef, out m_PrevAlphaRef);

                m_WasBlendEnabled = GL.IsEnabled(EnableCap.Blend);
                m_WasTexture2DEnabled = GL.IsEnabled(EnableCap.Texture2D);
                m_WasDepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            }

            // Set default values and enable/disable caps.
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.AlphaFunc(AlphaFunction.Greater, 1.0f);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);

            m_VertNum = 0;
            m_DrawCallCount = 0;
            m_ClipEnabled = false;
            m_TextureEnabled = false;
            m_LastTextureID = -1;

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void DrawFilledRect(Rectangle rect)
        {
            if (m_TextureEnabled)
            {
                Flush();
                GL.Disable(EnableCap.Texture2D);
                m_TextureEnabled = false;
            }

            rect = Translate(rect);

            DrawRect(rect);
        }

        public override void DrawTexturedRect(Texture t, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            // Missing image, not loaded properly?
            if (null == t.RendererData)
            {
                DrawMissingImage(rect);
                return;
            }

            int tex = (int)t.RendererData;
            rect = Translate(rect);

            bool differentTexture = (tex != m_LastTextureID);
            if (!m_TextureEnabled || differentTexture)
            {
                Flush();
            }

            if (!m_TextureEnabled)
            {
                GL.Enable(EnableCap.Texture2D);
                m_TextureEnabled = true;
            }

            if (differentTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, tex);
                m_LastTextureID = tex;
            }

            DrawRect(rect, u1, v1, u2, v2);
        }

        public void DrawTexturedRect(int t, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            int tex = t;
            rect = Translate(rect);

            bool differentTexture = (tex != m_LastTextureID);
            if (!m_TextureEnabled || differentTexture)
            {
                Flush();
            }

            if (!m_TextureEnabled)
            {
                GL.Enable(EnableCap.Texture2D);
                m_TextureEnabled = true;
            }

            if (differentTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, tex);
                m_LastTextureID = tex;
            }

            DrawRect(rect, u1, v1, u2, v2);
        }

        public override void End()
        {
            Flush();

            if (m_RestoreRenderState)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                m_LastTextureID = 0;

                // Restore the previous parameter values.
                GL.BlendFunc((BlendingFactorSrc)m_PrevBlendSrc, (BlendingFactorDest)m_PrevBlendDst);
                GL.AlphaFunc((AlphaFunction)m_PrevAlphaFunc, m_PrevAlphaRef);

                if (!m_WasBlendEnabled)
                    GL.Disable(EnableCap.Blend);

                if (m_WasTexture2DEnabled && !m_TextureEnabled)
                    GL.Enable(EnableCap.Texture2D);

                if (m_WasDepthTestEnabled)
                    GL.Enable(EnableCap.DepthTest);
            }

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
        }

        public override void EndClip()
        {
            m_ClipEnabled = false;
        }

        public unsafe void Flush()
        {
            if (m_VertNum == 0) return;

            fixed (short* ptr1 = &m_Vertices[0].x)
            fixed (byte* ptr2 = &m_Vertices[0].r)
            fixed (float* ptr3 = &m_Vertices[0].u)
            {
                GL.VertexPointer(2, VertexPointerType.Short, m_VertexSize, (IntPtr)ptr1);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, m_VertexSize, (IntPtr)ptr2);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, m_VertexSize, (IntPtr)ptr3);

                GL.DrawArrays(PrimitiveType.Quads, 0, m_VertNum);
            }

            m_DrawCallCount++;
            m_VertNum = 0;
        }


        public override void FreeFont(Font font)
        {
            Debug.Print(String.Format("FreeFont {0}", font.FaceName));
            if (font.RendererData == null)
                return;
            if (font is BitmapFont)
            {
                var f = font.RendererData as Texture;
                if (f == null)
                    throw new InvalidOperationException("Freeing empty font");
                FreeTexture(f);
                return;
            }
            Debug.Print(String.Format("FreeFont {0} - actual free", font.FaceName));
            System.Drawing.Font sysFont = font.RendererData as System.Drawing.Font;
            if (sysFont == null)
                throw new InvalidOperationException("Freeing empty font");

            sysFont.Dispose();
            font.RendererData = null;
        }

        public override void FreeTexture(Texture t)
        {
            if (t.RendererData == null)
                return;
            int tex = (int)t.RendererData;
            if (tex == 0)
                return;
            GL.DeleteTextures(1, ref tex);
            t.RendererData = null;
        }

        public override bool LoadFont(Font font)
        {
            Debug.Print(String.Format("LoadFont {0}", font.FaceName));
            if (font is BitmapFont)
            {
                return true;
            }
            return false;
        }

        public override void LoadTexture(Texture t)
        {
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(t.Name);
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override void LoadTextureRaw(Texture t, byte[] pixelData)
        {
            Bitmap bmp;
            try
            {
                unsafe
                {
                    fixed (byte* ptr = &pixelData[0])
                        bmp = new Bitmap(t.Width, t.Height, 4 * t.Width, PixelFormat.Format32bppArgb, (IntPtr)ptr);
                }
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            int glTex;

            // Create the opengl texture
            GL.GenTextures(1, out glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Sort out our GWEN texture
            t.RendererData = glTex;

            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, t.Width, t.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);

            bmp.UnlockBits(data);
            bmp.Dispose();

            //[halfofastaple] Must rebind previous texture, to ensure creating a texture doesn't mess with the render flow.
            // Setting m_LastTextureID isn't working, for some reason (even if you always rebind the texture,
            // even if the previous one was the same), we are probably making draw calls where we shouldn't be?
            // Eventually the bug needs to be fixed (color picker in a window causes graphical errors), but for now,
            // this is fine.
            GL.BindTexture(TextureTarget.Texture2D, m_LastTextureID);
        }

        public override void LoadTextureStream(Texture t, System.IO.Stream data)
        {
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(data);
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override Point MeasureText(Font font, string text)
        {
            //Debug.Print(String.Format("MeasureText '{0}'", text));
            if (font is BitmapFont)
            {
                var data = (BitmapFont)font;
                var ret = data.fontdata.MeasureText(text);
                return new Point((int)ret.Width, (int)ret.Height);
            }
            else
            {
                throw new Exception("Unsupported font");
            }
        }

        public override unsafe Color PixelColor(Texture texture, uint x, uint y, Color defaultColor)
        {
            if (texture.RendererData == null)
                return defaultColor;

            int tex = (int)texture.RendererData;
            if (tex == 0)
                return defaultColor;

            Color pixel;

            GL.BindTexture(TextureTarget.Texture2D, tex);
            m_LastTextureID = tex;

            long offset = 4 * (x + y * texture.Width);
            byte[] data = new byte[4 * texture.Width * texture.Height];
            fixed (byte* ptr = &data[0])
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                pixel = Color.FromArgb(data[offset + 3], data[offset + 0], data[offset + 1], data[offset + 2]);
            }

            //[???] Retrieving the entire texture for a single pixel read
            // is kind of a waste - maybe cache this pointer in the texture
            // data and then release later on? It's never called during runtime
            // - only during initialization.

            //[halfofastaple] RE: It's not really a waste if it's only done once on load.
            // Despite, it's worth looking into, just in case a user
            // wishes to hack their code together and use this function at
            // runtime
            return pixel;
        }

        public override void RenderText(Font font, Point position, string text)
        {
            //Debug.Print(String.Format("RenderText {0}", font.FaceName));

            if (font is BitmapFont)
            {
                var data = (BitmapFont)font;
                {
                    var vertices = data.fontdata.GenerateText(position.X, position.Y, text);
                    for (int i = 0; i < vertices.Count; i += 4)
                    {
                        var tl = vertices[i];
                        var br = vertices[i + 2];
                        DrawTexturedRect(data.texture, Rectangle.FromLTRB(tl.x, tl.y, br.x, br.y), tl.u, tl.v, br.u, br.v);
                    }
                }
                return;
            }
            else
            {
                throw new Exception("Drawing unsupported font");
            }
        }

        public override void StartClip()
        {
            m_ClipEnabled = true;
        }

        private void DrawRect(Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            if (m_VertNum + 4 >= MaxVerts)
            {
                Flush();
            }

            if (m_ClipEnabled)
            {
                // cpu scissors test

                if (rect.Y < ClipRegion.Y)
                {
                    int oldHeight = rect.Height;
                    int delta = ClipRegion.Y - rect.Y;
                    rect.Y = ClipRegion.Y;
                    rect.Height -= delta;

                    if (rect.Height <= 0)
                    {
                        return;
                    }

                    float dv = (float)delta / (float)oldHeight;

                    v1 += dv * (v2 - v1);
                }

                if ((rect.Y + rect.Height) > (ClipRegion.Y + ClipRegion.Height))
                {
                    int oldHeight = rect.Height;
                    int delta = (rect.Y + rect.Height) - (ClipRegion.Y + ClipRegion.Height);

                    rect.Height -= delta;

                    if (rect.Height <= 0)
                    {
                        return;
                    }

                    float dv = (float)delta / (float)oldHeight;

                    v2 -= dv * (v2 - v1);
                }

                if (rect.X < ClipRegion.X)
                {
                    int oldWidth = rect.Width;
                    int delta = ClipRegion.X - rect.X;
                    rect.X = ClipRegion.X;
                    rect.Width -= delta;

                    if (rect.Width <= 0)
                    {
                        return;
                    }

                    float du = (float)delta / (float)oldWidth;

                    u1 += du * (u2 - u1);
                }

                if ((rect.X + rect.Width) > (ClipRegion.X + ClipRegion.Width))
                {
                    int oldWidth = rect.Width;
                    int delta = (rect.X + rect.Width) - (ClipRegion.X + ClipRegion.Width);

                    rect.Width -= delta;

                    if (rect.Width <= 0)
                    {
                        return;
                    }

                    float du = (float)delta / (float)oldWidth;

                    u2 -= du * (u2 - u1);
                }
            }

            int vertexIndex = m_VertNum;
            m_Vertices[vertexIndex].x = (short)rect.X;
            m_Vertices[vertexIndex].y = (short)rect.Y;
            m_Vertices[vertexIndex].u = u1;
            m_Vertices[vertexIndex].v = v1;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)(rect.X + rect.Width);
            m_Vertices[vertexIndex].y = (short)rect.Y;
            m_Vertices[vertexIndex].u = u2;
            m_Vertices[vertexIndex].v = v1;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)(rect.X + rect.Width);
            m_Vertices[vertexIndex].y = (short)(rect.Y + rect.Height);
            m_Vertices[vertexIndex].u = u2;
            m_Vertices[vertexIndex].v = v2;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)rect.X;
            m_Vertices[vertexIndex].y = (short)(rect.Y + rect.Height);
            m_Vertices[vertexIndex].u = u1;
            m_Vertices[vertexIndex].v = v2;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            m_VertNum += 4;
        }
        #endregion Methods
    }
}