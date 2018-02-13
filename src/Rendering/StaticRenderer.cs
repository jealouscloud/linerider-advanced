//
//  StaticRenderer.cs
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
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Color = System.Drawing.Color;
using linerider.Tools;
using linerider.UI;
using linerider.Utils;
using linerider.Drawing;
namespace linerider.Rendering
{
    public static class StaticRenderer
    {
        #region Fields

        /// <summary>
        /// Circle Texture for fast rendering.
        /// 1000 px large.
        /// </summary>
        public static int CircleTex;

        #endregion Fields

        #region Methods
        public static Vector2[] Arc(float cx, float cy, float r, float start_angle, float end_angle)
        {
            List<Vector2> ret = new List<Vector2>();
            for (var i = start_angle; i < end_angle; i += 0.05f)
            {
                ret.Add(new Vector2(cx + (float)Math.Cos(i) * r, cy + (float)Math.Sin(i) * r));
            }
            return ret.ToArray();
        }

        internal static Vector2 CalculateLine(Vector2 position, Angle angle, double length)
        {
            var ret = position;
            var radians = angle.Radians;
            var sin = Math.Sin(radians);
            var cos = Math.Cos(radians);
            ret.X = (float)(ret.X + length * cos);
            ret.Y = (float)(ret.Y + length * sin);
            return ret;
        }

        internal static Vector2d CalculateLine(Vector2d position, Angle angle, double length)
        {
            var ret = position;
            var radians = angle.Radians;
            var sin = Math.Sin(radians);
            var cos = Math.Cos(radians);
            ret.X = ret.X + length * cos;
            ret.Y = ret.Y + length * sin;
            return ret;
        }

        public static Vector2[] DrawArc(float cx, float cy, float r, float start_angle, float arc_angle, int num_segments)
        {
            Vector2[] ret = new Vector2[num_segments];
            float theta = arc_angle / (float)(num_segments - 1);//theta is now calculated from the arc angle instead, the - 1 bit comes from the fact that the arc is open

            float tangetial_factor = (float)Math.Tan(theta);

            float radial_factor = (float)Math.Cos(theta);

            float x = r * (float)Math.Cos(start_angle);//we now start at the start angle
            float y = r * (float)Math.Sin(start_angle);

            for (int ii = 0; ii < num_segments; ii++)
            {
                ret[ii] = new Vector2((float)x + cx, (float)y + cy);

                float tx = -y;
                float ty = x;

                x += tx * tangetial_factor;
                y += ty * tangetial_factor;

                x *= radial_factor;
                y *= radial_factor;
            }
            return ret;
        }

        public static void AddCircleVerts(float cx, float cy, float r, int num_segments)
        {
            var circle = GenerateCircle(cx, cy, r, num_segments);
            foreach (var v in circle)
            {
                GL.Vertex2(v);
            }
            GL.Vertex2(circle[0]);
        }
        public static void RenderCircle(Vector2d viewpos, Vector2d pos, float radius, Color c)
        {
            RectangleF rect = new RectangleF((float)viewpos.X + (float)pos.X, (float)viewpos.Y + (float)pos.Y, radius * 2, radius * 2);
            rect.X -= radius;
            rect.Y -= radius;

            GL.BindTexture(TextureTarget.Texture2D, CircleTex);
            GL.Color4(c);
            float x = rect.Location.X;
            float y = rect.Location.Y;
            float xx = x + rect.Size.Width;
            float yy = y + rect.Size.Height;
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x, y);
            GL.TexCoord2(1, 0); GL.Vertex2(xx, y);
            GL.TexCoord2(1, 1); GL.Vertex2(xx, yy);
            GL.TexCoord2(0, 1); GL.Vertex2(x, yy);
            GL.End();
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public static void DrawConnectedLines(Vector2[] lines, Color color, float thickness)
        {
            GL.Color4(color);
            GL.Begin(PrimitiveType.Triangles);
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var line = GenerateThickLine(lines[i], lines[i + 1], thickness);
                GL.Vertex2(line[0]);
                GL.Vertex2(line[1]);
                GL.Vertex2(line[2]);

                GL.Vertex2(line[0]);
                GL.Vertex2(line[3]);
                GL.Vertex2(line[2]);
            }
            GL.End();
        }
        public static void DrawTexture(int tex, DoubleRect rect, float alpha = 1, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            VAO buf = new VAO(false, false, 6);
            buf.Texture = tex;
            var tr = new Vector2d(rect.Right, rect.Top);
            var tl = new Vector2d(rect.Left, rect.Top);
            var bl = new Vector2d(rect.Left, rect.Bottom);
            var br = new Vector2d(rect.Right, rect.Bottom);
            var c = Color.FromArgb((int)Math.Min(255, (alpha * 255)), 255, 255, 255);
            buf.AddVertex(new GenericVertex((Vector2)tl, c, u1, v1));
            buf.AddVertex(new GenericVertex((Vector2)tr, c, u2, v1));
            buf.AddVertex(new GenericVertex((Vector2)br, c, u2, v2));
            buf.AddVertex(new GenericVertex((Vector2)bl, c, u1, v2));
            buf.Draw(PrimitiveType.Quads);
        }
        public static void DrawTexture(int tex, RectangleF rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            DrawTexture(tex, new DoubleRect(rect.Left, rect.Top, rect.Width, rect.Height), 1, u1, v1, u2, v2);
        }
        public static List<GenericVertex> FastCircle(Vector2d p, float radius, Color co)
        {
            return FastCircle((Vector2)p, radius, co);
        }

        public static List<GenericVertex> FastCircle(Vector2 p, float radius, Color co)
        {
            List<GenericVertex> ret = new List<GenericVertex>();
            ret.Add(new GenericVertex(p.X - radius, p.Y - radius, co, 0, 1));
            ret.Add(new GenericVertex(p.X + radius, p.Y - radius, co, 1, 1));
            ret.Add(new GenericVertex(p.X + radius, p.Y + radius, co, 1, 0));

            ret.Add(new GenericVertex(p.X - radius, p.Y + radius, co, 0, 0));
            ret.Add(new GenericVertex(p.X - radius, p.Y - radius, co, 0, 1));
            ret.Add(new GenericVertex(p.X + radius, p.Y + radius, co, 1, 0));
            return ret;
        }

        public static Vector2d[] GenerateCircle(double cx, double cy, double r, int num_segments)
        {
            Vector2d[] ret = new Vector2d[num_segments + 1];
            var theta = 2 * 3.1415926 / num_segments;
            var tangetialFactor = Math.Tan(theta); //calculate the tangential factor
            var radialFactor = Math.Cos(theta); //calculate the radial factor
            double x = r; //we start at angle = 0
            double y = 0;
            for (var ii = 0; ii < num_segments; ii++)
            {
                ret[ii] = new Vector2d(x + cx, y + cy);
                //calculate the tangential vector
                //remember, the radial vector is (x, y)
                //to get the tangential vector we flip those coordinates and negate one of them
                var tx = -y;
                var ty = x;
                //add the tangential vector
                x += tx * tangetialFactor;
                y += ty * tangetialFactor;
                //correct using the radial factor
                x *= radialFactor;
                y *= radialFactor;
            }
            ret[ret.Length - 1] = ret[0];
            return ret;
        }
        public static int LoadTexture(Bitmap bmp)
        {
            var lock_format = System.Drawing.Imaging.PixelFormat.Undefined;
            switch (bmp.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    lock_format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    lock_format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                    break;

                default:
                    throw new Exception("Failed to load texture");
            }

            int glTex;

            // Create the opengl texture
            GL.GenTextures(1, out glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToBorder);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, lock_format);

            switch (lock_format)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    break;

                default:
                    // invalid
                    break;
            }

            bmp.UnlockBits(data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            return glTex;
        }
        public static void InitializeCircles()
        {
            CircleTex = LoadTexture(GameResources.circletex);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public static Vector2[] GenerateEllipse(float radiusX, float radiusY, int segments)
        {
            Vector2[] ret = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float rad = MathHelper.DegreesToRadians(i);
                ret[i] = new Vector2((float)(Math.Cos(rad) * radiusX), (float)(Math.Sin(rad) * radiusY));
            }
            ret[segments - 1] = ret[0];
            return ret;
        }

        public static Vector2[] GenerateThickLine(Vector2 p, Vector2 p1, float radians, float width)
        {
            Vector2[] ret = new Vector2[4];
            var angle = Angle.FromRadians(radians);
            angle.Radians += 1.5708f; //90 degrees as const, radians so no conversion between degrees for a radians only calculation
            var t = CalculateLine(Vector2.Zero, angle, width / 2);
            ret[0] = p + t;
            ret[1] = p1 + t;
            ret[2] = p1 - t;
            ret[3] = p - t;
            return ret;
        }
        public static Vector2[] GenerateThickLine(Vector2 p, Vector2 p1, float width)
        {
            Vector2[] ret = new Vector2[4];
            var angle = Angle.FromLine(p, p1);
            angle.Radians += 1.5708f; //90 degrees as const, radians so no conversion between degrees for a radians only calculation
            var t = CalculateLine(Vector2.Zero, angle, width / 2);
            ret[0] = p + t;
            ret[1] = p1 + t;
            ret[2] = p1 - t;
            ret[3] = p - t;
            return ret;
        }
        public static Vector2d[] GenerateThickLine(Vector2d p, Vector2d p1, double width)
        {
            Vector2d[] ret = new Vector2d[4];
            var angle = Angle.FromLine(p, p1);
            angle.Radians += 1.5708f; //90 degrees as const, radians so no conversion between degrees for a radians only calculation
            var t = CalculateLine(Vector2d.Zero, angle, width / 2);
            ret[0] = p + t;//bl
            ret[1] = p1 + t;//br
            ret[2] = p1 - t;//tr
            ret[3] = p - t;//tl
            return ret;
        }
        public static List<GenericVertex> GenerateRoundedLine(Vector2 p, Vector2 p1, float width, Color c)
        {
            List<GenericVertex> ret = new List<GenericVertex>();
            ret.AddRange(FastCircle(p, width / 2, c));
            ret.AddRange(FastCircle(p1, width / 2, c));
            var thickline = GenerateThickLine(p, p1, width);

            var v1 = new GenericVertex(thickline[0].X, thickline[0].Y, c);
            var v2 = new GenericVertex(thickline[1].X, thickline[1].Y, c);
            var v3 = new GenericVertex(thickline[2].X, thickline[2].Y, c);
            var v4 = new GenericVertex(thickline[3].X, thickline[3].Y, c);
            ret.Add(v1);
            ret.Add(v2);
            ret.Add(v3);

            ret.Add(v1);
            ret.Add(v4);
            ret.Add(v3);

            return ret;
        }

        public static void RenderLine(Vector2d position, Vector2d position2, Color color, float thickness)
        {
            DrawConnectedLines(new Vector2[] { (Vector2)position, (Vector2)position2 }, color, thickness);
        }

        public static void RenderRect(FloatRect rect, Color color)
        {
            RenderRect(new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), color);
        }

        public static void RenderRect(RectangleF rect, Color color)
        {
            GL.Enable(EnableCap.Blend);
            GL.Begin(PrimitiveType.Quads);
            GL.Color4(color);
            GL.Vertex2(new Vector2(rect.Left, rect.Top));
            GL.Vertex2(new Vector2(rect.Left + rect.Width, rect.Top));
            GL.Vertex2(new Vector2(rect.Left + rect.Width, rect.Top + rect.Height));
            GL.Vertex2(new Vector2(rect.Left, rect.Top + rect.Height));
            GL.End();
        }

        /// <summary>
        /// Rotates one point around another
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The centre point of rotation.</param>
        /// <param name="angleInRadians">angle in radians</param>
        /// <returns>Rotated point</returns>
        public static Vector2 RotatePoint(Vector2 pointToRotate, Vector2 centerPoint, double angleInRadians)
        {
            var cosTheta = Math.Cos(angleInRadians);
            var sinTheta = Math.Sin(angleInRadians);
            return (Vector2)new Vector2d
            {
                X =
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                     sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                     cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        #endregion Methods
    }
}