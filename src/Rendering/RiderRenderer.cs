using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using linerider.Drawing;
using linerider.Utils;
using linerider.Game;
using System.Diagnostics;

namespace linerider.Rendering
{
    public class RiderRenderer
    {
        private AutoArray<RiderVertex> Array = new AutoArray<RiderVertex>(500);
        public float Scale = 1.0f;
        private Shader _shader;
        private GLBuffer<RiderVertex> _vbo;
        private LineVAO _lines = new LineVAO();
        private LineVAO _momentumvao = new LineVAO();
        public RiderRenderer()
        {
            _shader = Shaders.RiderShader;
            _vbo = new GLBuffer<RiderVertex>(BufferTarget.ArrayBuffer);
        }
        public void DrawMomentum(Rider rider, float opacity)
        {
            var color = Constants.MomentumVectorColor;
            color = ChangeOpacity(color, opacity);
            for (int i = 0; i < rider.Body.Length; i++)
            {
                var anchor = rider.Body[i];
                var vec1 = anchor.Location;
                var vec2 = vec1 + (anchor.Momentum);
                var line = Line.FromAngle(
                    vec1,
                    Angle.FromVector(anchor.Momentum),
                    2);
                _momentumvao.AddLine(
                    GameDrawingMatrix.ScreenCoordD(line.Position),
                    GameDrawingMatrix.ScreenCoordD(line.Position2),
                    color,
                    GameDrawingMatrix.Scale / 2.5f);
            }
        }
        public void DrawContacts(Rider rider, List<int> diagnosis, float opacity)
        {
            if (diagnosis == null)
                diagnosis = new List<int>();
            var bones = RiderConstants.Bones;
            for (var i = 0; i < bones.Length; i++)
            {
                var constraintcolor = bones[i].OnlyRepel
                ? Constants.ConstraintRepelColor
                : Constants.ConstraintColor;

                constraintcolor = ChangeOpacity(constraintcolor, opacity);

                if (bones[i].Breakable)
                {
                    continue;
                }
                else if (bones[i].OnlyRepel)
                {
                    _lines.AddLine(
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[i].joint1].Location),
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[i].joint2].Location),
                        constraintcolor,
                        GameDrawingMatrix.Scale / 4);
                }
                else if (i <= 3)
                {
                    _lines.AddLine(
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[i].joint1].Location),
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[i].joint2].Location),
                        constraintcolor,
                        GameDrawingMatrix.Scale / 4);
                }
            }
            if (!rider.Crashed && diagnosis.Count != 0)
            {
                Color firstbreakcolor = Constants.ConstraintFirstBreakColor;
                Color breakcolor = Constants.ConstraintBreakColor;
                breakcolor = ChangeOpacity(breakcolor, opacity / 2);
                firstbreakcolor = ChangeOpacity(firstbreakcolor, opacity);
                for (int i = 1; i < diagnosis.Count; i++)
                {
                    var broken = diagnosis[i];
                    if (broken >= 0)
                    {
                        _lines.AddLine(
                            GameDrawingMatrix.ScreenCoordD(rider.Body[bones[broken].joint1].Location),
                            GameDrawingMatrix.ScreenCoordD(rider.Body[bones[broken].joint2].Location),
                            breakcolor,
                            GameDrawingMatrix.Scale  / 4);
                    }
                }
                //the first break is most important so we give it a better color, assuming its not just a fakie death
                if (diagnosis[0] > 0)
                {
                    _lines.AddLine(
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[diagnosis[0]].joint1].Location),
                        GameDrawingMatrix.ScreenCoordD(rider.Body[bones[diagnosis[0]].joint2].Location),
                        firstbreakcolor,
                        GameDrawingMatrix.Scale / 4);
                }
            }
            for (var i = 0; i < rider.Body.Length; i++)
            {
                Color c = Constants.ContactPointColor;
                if (
                    ((i == RiderConstants.SledTL || i == RiderConstants.SledBL) && diagnosis.Contains(-1)) ||
                    ((i == RiderConstants.BodyButt || i == RiderConstants.BodyShoulder) && diagnosis.Contains(-2)))
                {
                    c = Constants.ContactPointFakieColor;
                }
                c = ChangeOpacity(c, opacity);
                _lines.AddLine(
                    GameDrawingMatrix.ScreenCoordD(rider.Body[i].Location),
                    GameDrawingMatrix.ScreenCoordD(rider.Body[i].Location),
                    c,
                    GameDrawingMatrix.Scale / 4);
            }
        }
        public void DrawRider(float opacity, Rider rider, bool scarf = false)
        {
            if (scarf)
            {
                DrawScarf(rider.GetScarfLines(), opacity);
            }
            var points = rider.Body;
            DrawTexture(
                 Tex.Leg,
                 Models.LegRect,
                 Models.LegUV,
                 points[RiderConstants.BodyButt].Location,
                 points[RiderConstants.BodyFootRight].Location,
                 opacity);

            DrawTexture(
                Tex.Arm,
                Models.ArmRect,
                Models.ArmUV,
                points[RiderConstants.BodyShoulder].Location,
                points[RiderConstants.BodyHandRight].Location,
                opacity);
            if (!rider.Crashed)
                DrawLine(
                    points[RiderConstants.BodyHandRight].Location,
                    points[RiderConstants.SledTR].Location,
                    ChangeOpacity(Color.Black, opacity),
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
                        Tex.BrokenSled,
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
                        Tex.BrokenSled,
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
                    opacity);
            }

            DrawTexture(
                Tex.Leg,
                Models.LegRect,
                    Models.LegUV,
                points[RiderConstants.BodyButt].Location,
                points[RiderConstants.BodyFootLeft].Location,
                opacity);
            if (!rider.Crashed)
            {
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
                    Tex.BodyDead,
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
                    ChangeOpacity(Color.Black, opacity),
                    0.1f);

            DrawTexture(
                Tex.Arm,
                Models.ArmRect,
                Models.ArmUV,
                points[RiderConstants.BodyShoulder].Location,
                points[RiderConstants.BodyHandLeft].Location,
                opacity);
        }
        public void Clear()
        {
            Array.UnsafeSetCount(0);
            _lines.Clear();
            _momentumvao.Clear();
        }
        protected unsafe void BeginDraw()
        {
            _vbo.Bind();
            _shader.Use();
            EnsureBufferSize(Array.Count);
            var in_vertex = _shader.GetAttrib("in_vertex");
            var in_texcoord = _shader.GetAttrib("in_texcoord");
            var in_unit = _shader.GetAttrib("in_unit");
            var in_color = _shader.GetAttrib("in_color");
            GL.EnableVertexAttribArray(in_vertex);
            GL.EnableVertexAttribArray(in_texcoord);
            GL.EnableVertexAttribArray(in_unit);
            GL.EnableVertexAttribArray(in_color);
            _vbo.SetData(Array.unsafe_array, 0, 0, Array.Count);
            int offset = 0;
            GL.VertexAttribPointer(in_vertex, 2, VertexAttribPointerType.Float, false, RiderVertex.Size, offset);
            offset += 8;
            GL.VertexAttribPointer(in_texcoord, 2, VertexAttribPointerType.Float, false, RiderVertex.Size, offset);
            offset += 8;
            GL.VertexAttribPointer(in_unit, 1, VertexAttribPointerType.Float, false, RiderVertex.Size, offset);
            offset += 4;
            GL.VertexAttribPointer(in_color, 4, VertexAttribPointerType.UnsignedByte, true, RiderVertex.Size, offset);
            offset += 4;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Models.BodyTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, Models.BodyDeadTexture);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, Models.ArmTexture);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, Models.LegTexture);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, Models.SledTexture);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, Models.BrokenSledTexture);

            GL.Uniform1(_shader.GetUniform("u_bodytex"), 0);
            GL.Uniform1(_shader.GetUniform("u_bodydeadtex"), 1);
            GL.Uniform1(_shader.GetUniform("u_armtex"), 2);
            GL.Uniform1(_shader.GetUniform("u_legtex"), 3);
            GL.Uniform1(_shader.GetUniform("u_sledtex"), 4);
            GL.Uniform1(_shader.GetUniform("u_brokensledtex"), 5);

        }
        public void DrawLines()
        {
            if (_lines.Array.Count != 0)
            {
                _lines.Scale = 1;
                _lines.Draw(PrimitiveType.Triangles);
            }
        }
        public void Draw()
        {
            BeginDraw();
            using (new GLEnableCap(EnableCap.Blend))
            {
                GL.DrawArrays(PrimitiveType.Triangles, 0, Array.Count);
            }
            EndDraw();
            if (_momentumvao.Array.Count != 0)
            {
                _momentumvao.Scale = 1;
                _momentumvao.Draw(PrimitiveType.Triangles);
            }
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

            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            //end on unit 0
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            _vbo.Unbind();
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
            var transform = new Vector2d[] {
            (t[0] + origin),// 0 tl
            (t[1] + origin),// 1 tr
            (t[2] + origin),  // 2 br
            (t[3] + origin),  // 3 bl
            };
            var texrect = GameDrawingMatrix.ScreenCoords(transform);
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
                new RiderVertex(texrect[0],  new Vector2(uv.Left, uv.Top),tex,colors[0]),
                new RiderVertex(texrect[1],  new Vector2(uv.Right, uv.Top),tex,colors[1]),
                new RiderVertex(texrect[2],  new Vector2(uv.Right, uv.Bottom),tex,colors[2]),
                new RiderVertex(texrect[3],  new Vector2(uv.Left, uv.Bottom),tex,colors[3])
            };
            Array.Add(verts[0]);
            Array.Add(verts[1]);
            Array.Add(verts[2]);

            Array.Add(verts[3]);
            Array.Add(verts[2]);
            Array.Add(verts[0]);
        }
        private void DrawLine(Vector2d p1, Vector2d p2, Color Color, float size)
        {
            DrawLine(p1, p2, Utility.ColorToRGBA_LE(Color), size);
        }
        private Vector2[] DrawLine(Vector2d p1, Vector2d p2, int color, float size)
        {
            var calc = Utility.GetThickLine(p1, p2, Angle.FromLine(p1, p2), size);
            var t = GameDrawingMatrix.ScreenCoords(calc);
            var verts = new RiderVertex[] {
                RiderVertex.NoTexture(t[0],color),
                RiderVertex.NoTexture(t[1],color),
                RiderVertex.NoTexture(t[2],color),
                RiderVertex.NoTexture(t[3],color)
            };
            Array.Add(verts[0]);
            Array.Add(verts[1]);
            Array.Add(verts[2]);

            Array.Add(verts[3]);
            Array.Add(verts[2]);
            Array.Add(verts[0]);
            return t;
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
        private void EnsureBufferSize(int size)
        {
            if (_vbo.BufferSize < size)
            {
                _vbo.SetSize(size * 2, BufferUsageHint.StreamDraw);
            }
        }
        private Color ChangeOpacity(Color c, float opacity)
        {
            return Color.FromArgb((int)(opacity * c.A), c);
        }
        private enum Tex
        {
            None = 0,
            Body = 1,
            BodyDead = 2,
            Arm = 3,
            Leg = 4,
            Sled = 5,
            BrokenSled = 6,
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
            public RiderVertex(Vector2 position, Vector2 uv, Tex unit, Color Color)
            {
                Position = position;
                tex_coord = uv;
                texture_unit = (float)unit;
                color = Utility.ColorToRGBA_LE(Color);
            }
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