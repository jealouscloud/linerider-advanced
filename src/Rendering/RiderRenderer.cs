using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
using linerider.Lines;
using linerider.Game;
using System.Diagnostics;

namespace linerider.Rendering
{
    public class RiderRenderer : GameService
    {
        private AutoArray<RiderVertex> Array = new AutoArray<RiderVertex>(500);
        public float Scale = 1.0f;
        private Shader _shader;
        public RiderRenderer()
        {
            _shader = Shaders.RiderShader;
        }
        public void Clear()
        {
            Array.Empty();
        }
        public void DrawRider(float opacity, Rider rider, bool scarf = false)
        {
            if (scarf)
            {
                DrawScarf(rider.GetScarfLines(), opacity);
            }
            var points = rider.Body;
            DrawTexture(
                 Tex.Limb,
                 Models.LegRect,
                 Models.LegUV,
                 points[RiderConstants.BodyButt].Location,
                 points[RiderConstants.BodyFootRight].Location, opacity);

            DrawTexture(
                Tex.Limb,
                Models.ArmRect,
                Models.ArmUV,
                points[RiderConstants.BodyShoulder].Location,
                points[RiderConstants.BodyHandRight].Location, opacity);
            if (!rider.Crashed)
                DrawLine(
                    points[RiderConstants.BodyHandRight].Location,
                    points[RiderConstants.SledTR].Location,
                    Color.Black,
                    0.1f);

            if (rider.SledBroken)
            {
                var nose = points[RiderConstants.SledTR].Location - points[RiderConstants.SledTL].Location;
                var tail = points[RiderConstants.SledBL].Location - points[RiderConstants.SledTL].Location;
                if ((nose.X * tail.Y) - (nose.Y * tail.X) < 0)
                {
                    var olduv = Models.BrokenSledUV;
                    //we're upside down
                    DrawTexture(
                        Tex.Sled,
                        Models.BrokenSledRect,
                        FloatRect.FromLRTB(
                            olduv.Left,
                            olduv.Right,
                            olduv.Bottom,
                            olduv.Top),
                        points[RiderConstants.SledBL].Location,
                        points[RiderConstants.SledBR].Location, opacity);
                }
                else
                {
                    DrawTexture(
                        Tex.Sled,
                        Models.BrokenSledRect,
                        Models.BrokenSledUV,
                        points[RiderConstants.SledTL].Location,
                        points[RiderConstants.SledTR].Location,
                        opacity);
                }
            }
            else
            {
                DrawTexture(
                    Tex.Sled,
                    Models.SledRect,
                    Models.SledUV,
                    points[RiderConstants.SledTL].Location,
                    points[RiderConstants.SledTR].Location,
                    opacity,
                    true);
            }

            DrawTexture(
                Tex.Limb,
                Models.LegRect,
                    Models.LegUV,
                points[RiderConstants.BodyButt].Location,
                points[RiderConstants.BodyFootLeft].Location,
                opacity);
            if (!rider.Crashed)
            {
                var uv = Models.BodyUV;
                DrawTexture(
                    Tex.Body,
                    Models.BodyRect,
                    Models.BodyUV,
                    points[RiderConstants.BodyButt].Location,
                    points[RiderConstants.BodyShoulder].Location,
                    opacity);
            }
            else
            {
                DrawTexture(
                    Tex.Body,
                    Models.BodyRect,
                    Models.DeadBodyUV,
                    points[RiderConstants.BodyButt].Location,
                    points[RiderConstants.BodyShoulder].Location,
                    opacity);
            }
            if (!rider.Crashed)
                DrawLine(
                    points[RiderConstants.BodyHandLeft].Location,
                    points[RiderConstants.SledTR].Location,
                    Color.Black,
                    0.1f);

            DrawTexture(
                Tex.Limb,
                Models.ArmRect,
                Models.ArmUV,
                points[RiderConstants.BodyShoulder].Location,
                points[RiderConstants.BodyHandLeft].Location,
                opacity);
        }
        protected unsafe void BeginDraw()
        {
            _shader.Use();
            var in_vertex = _shader.GetAttrib("in_vertex");
            var in_texcoord = _shader.GetAttrib("in_texcoord");
            var in_unit = _shader.GetAttrib("in_unit");
            var in_color = _shader.GetAttrib("in_color");
            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_texcoord);
            GL.EnableVertexAttribArray(in_unit);
            GL.EnableVertexAttribArray(in_color);
            fixed (float* ptr1 = &Array.unsafe_array[0].Position.X)
            fixed (float* ptr2 = &Array.unsafe_array[0].tex_coord.X)
            fixed (float* ptr3 = &Array.unsafe_array[0].texture_unit)
            fixed (int* ptr4 = &Array.unsafe_array[0].color)
            {
                GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, RiderVertex.Size, (IntPtr)ptr1);
                GL.VertexAttribPointer(in_texcoord, 2, VertexAttribPointerType.Float, false, RiderVertex.Size, (IntPtr)ptr2);
                GL.VertexAttribPointer(in_unit, 1, VertexAttribPointerType.Float, false, RiderVertex.Size, (IntPtr)ptr3);
                GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, RiderVertex.Size, (IntPtr)ptr4);
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Models.BodyTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, Models.LimbsTexture);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, Models.SledTexture);

            GL.Uniform1(_shader.GetUniform("u_bodytex"), 0);
            GL.Uniform1(_shader.GetUniform("u_limbtex"), 1);
            GL.Uniform1(_shader.GetUniform("u_sledtex"), 2);

        }
        public void Draw()
        {
            GameDrawingMatrix.Enter();
            BeginDraw();
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            using (new GLEnableCap(EnableCap.Blend))
            {
                GL.DrawArrays(PrimitiveType.Triangles, 0, Array.Count);
            }
            EndDraw();
            GameDrawingMatrix.Exit();
        }
        protected void EndDraw()
        {
            var in_vertex = _shader.GetAttrib("in_vertex");
            var in_texcoord = _shader.GetAttrib("in_texcoord");
            var in_unit = _shader.GetAttrib("in_unit");
            var in_color = _shader.GetAttrib("in_color");
            GL.DisableVertexAttribArray(in_vertex);
            GL.DisableVertexAttribArray(in_texcoord);
            GL.DisableVertexAttribArray(in_unit);
            GL.DisableVertexAttribArray(in_color);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            //end on unit 0
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            _shader.Stop();
        }
        private void DrawTexture(
            Tex tex,
            DoubleRect rect,
            FloatRect uv,
            Vector2d origin,
            Vector2d rotationAnchor,
            float opacity,
            bool pretty = false
            )
        {
            var angle = Angle.FromLine(origin, rotationAnchor);
            var t = Utility.RotateRect(rect, Vector2d.Zero, angle);
            var texrect = new Vector2[] {
            (Vector2)(t[0] + origin),// 0 tl
            (Vector2)(t[1] + origin),// 1 tr
            (Vector2)(t[2] + origin),  // 2 br
            (Vector2)(t[3] + origin),  // 3 bl
            };
            var color = Color.FromArgb((int)(255f * opacity), Color.White);
            Color[] colors = new Color[] { color, color, color, color };
            if (pretty)
            {
                var random = new Random((Environment.TickCount / 100) % 255);
                for (int i = 0; i < colors.Length; i++)
                {
                    bool redness = random.Next() % 2 == 0;
                    bool blueness = random.Next() % 2 == 0;
                    var random1 = Math.Max(200, random.Next() % 255);
                    int red = Math.Min(255, redness ? (random1 * 2) : (random1 / 2));
                    int green = Math.Min(255, (blueness && redness) ? (random1) : (random1 / 2));
                    int blue = Math.Min(255, blueness ? (random1 * 2) : (random1 / 2));

                    colors[i] = Color.FromArgb((int)(255f * opacity), red, green, blue);
                }
            }
            var u1 = uv.Left;
            var v1 = uv.Top;
            var u2 = uv.Right;
            var v2 = uv.Bottom;
            var verts = new RiderVertex[] {
                new RiderVertex()
                {
                    Position = (Vector2)texrect[0],
                    tex_coord = new Vector2(uv.Left, uv.Top),
                    texture_unit = (float)tex,
                    color = Utility.ColorToRGBA_LE(colors[0])
                },
                new RiderVertex()
                {
                    Position = (Vector2)texrect[1],
                    tex_coord = new Vector2(uv.Right, uv.Top),
                    texture_unit = (float)tex,
                    color = Utility.ColorToRGBA_LE(colors[1])
                },
                new RiderVertex()
                {
                    Position = (Vector2)texrect[2],
                    tex_coord = new Vector2(uv.Right, uv.Bottom),
                    texture_unit = (float)tex,
                    color = Utility.ColorToRGBA_LE(colors[2])
                },
                new RiderVertex()
                {
                    Position = (Vector2)texrect[3],
                    tex_coord = new Vector2(uv.Left, uv.Bottom),
                    texture_unit = (float)tex,
                    color = Utility.ColorToRGBA_LE(colors[3])
                },
            };
            Array.Add(verts[0]);
            Array.Add(verts[1]);
            Array.Add(verts[2]);

            Array.Add(verts[3]);
            Array.Add(verts[2]);
            Array.Add(verts[0]);
        }
        private Vector2[] DrawLine(Vector2d p1, Vector2d p2, Color Color, float size)
        {
            return DrawLine(p1, p2, Utility.ColorToRGBA_LE(Color), size);
        }
        private Vector2[] DrawLine(Vector2d p1, Vector2d p2, int color, float size)
        {
            var t = Utility.GetThickLine(p1, p2, Angle.FromLine(p1, p2), size);
            var texrect = new Vector2[] {
                (Vector2)(t[0]),
                (Vector2)(t[1]),
                (Vector2)(t[2]),
                (Vector2)(t[3]),
                };
            var verts = new RiderVertex[] {
                RiderVertex.NoTexture(texrect[0],color),
                RiderVertex.NoTexture(texrect[1],color),
                RiderVertex.NoTexture(texrect[2],color),
                RiderVertex.NoTexture(texrect[3],color)
            };
            Array.Add(verts[0]);
            Array.Add(verts[1]);
            Array.Add(verts[2]);

            Array.Add(verts[3]);
            Array.Add(verts[2]);
            Array.Add(verts[0]);
            return texrect;
        }
        private void DrawScarf(Line[] lines, float opacity)
        {
            var c = Utility.ColorToRGBA_LE(0xD10101, (byte)(255 * opacity));
            var alt = Utility.ColorToRGBA_LE(0xff6464, (byte)(255 * opacity));
            List<Vector2> altvectors = new List<Vector2>();
            for (int i = 0; i < lines.Length; i += 2)
            {
                var verts = DrawLine(lines[i].Position, lines[i].Position2, c, 2);

                if (i != 0)
                {
                    altvectors.Add(verts[0]);
                    altvectors.Add(verts[1]);
                }
                altvectors.Add(verts[2]);
                altvectors.Add(verts[3]);
            }
            for (int i = 0; i < altvectors.Count - 4; i += 4)
            {
                var verts = new RiderVertex[] {
                    RiderVertex.NoTexture(altvectors[i + 0],alt),
                    RiderVertex.NoTexture(altvectors[i + 1],alt),
                    RiderVertex.NoTexture(altvectors[i + 2],alt),
                    RiderVertex.NoTexture(altvectors[i + 3],alt)
                };
                Array.Add(verts[0]);
                Array.Add(verts[1]);
                Array.Add(verts[2]);

                Array.Add(verts[3]);
                Array.Add(verts[2]);
                Array.Add(verts[0]);
            }
        }
        private enum Tex
        {
            None = 0,
            Body = 1,
            Limb = 2,
            Sled = 3,
        }
        /// <summary>
        /// A vertex meant for the simulation line shader
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RiderVertex
        {
            public static readonly int Size = Marshal.SizeOf(typeof(RiderVertex));
            public Vector2 Position;
            public Vector2 tex_coord;
            public float texture_unit;
            public int color;
            public static RiderVertex NoTexture(Vector2 position, int color)
            {
                RiderVertex ret = new RiderVertex();
                ret.color = color;
                ret.Position = (Vector2)position;
                ret.texture_unit = (float)Tex.None;
                return ret;
            }
        }
    }
}