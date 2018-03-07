//
//  GameRenderer.cs
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

using linerider.Tools;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using linerider.Game;
using linerider.Utils;
using linerider.Drawing;

namespace linerider.Rendering
{
    public static class GameRenderer
    {
        public static MainWindow Game;
        private static LineVAO _linevao = null;

        public static void DrawTrackLine(StandardLine line, Color color, bool drawwell, bool drawcolor)
        {
            var lv = new AutoArray<LineVertex>(24);
            var verts = new AutoArray<GenericVertex>(30);
            if (drawcolor)
            {
                if (line is RedLine redline)
                {
                    verts.AddRange(LineAccelRenderer.GetAccelDecor(redline));
                }
                lv.AddRange(LineColorRenderer.CreateDecorationLine(line, line.Color));
            }
            lv.AddRange(
                LineRenderer.CreateTrackLine(
                    line.Start,
                    line.End,
                    line.Width * 2,
                    Utility.ColorToRGBA_LE(color)));
            if (drawwell)
            {
                verts.AddRange(WellRenderer.GetWell(line));
            }
            var vao = GetLineVAO();
            vao.Scale = Game.Track.Zoom;
            foreach (var v in lv.unsafe_array)
            {
                vao.AddVertex(v);
            }
            GameDrawingMatrix.Enter();
            using (new GLEnableCap(EnableCap.Blend))
            {
                if (verts.Count != 0)
                {
                    GenericVAO gvao = new GenericVAO();
                    foreach (var v in verts.unsafe_array)
                    {
                        gvao.AddVertex(v);
                    }
                    gvao.Draw(PrimitiveType.Triangles);
                }
                vao.Draw(PrimitiveType.Triangles);
            }
            GameDrawingMatrix.Exit();
        }
        private static LineVAO GetLineVAO()
        {
            if (_linevao == null)
                _linevao = new LineVAO();
            _linevao.Clear();
            return _linevao;
        }
        public static void RenderRoundedLine(Vector2d position, Vector2d position2, Color color, float thickness, bool knobs = false, bool redknobs = false)
        {
            using (new GLEnableCap(EnableCap.Blend))
            {
                using (new GLEnableCap(EnableCap.Texture2D))
                {
                    GameDrawingMatrix.Enter();
                    var vao = GetLineVAO();
                    vao.Scale = GameDrawingMatrix.Scale;
                    vao.AddLine(position, position2, color, thickness);
                    vao.knobstate = knobs ? (redknobs ? 2 : 1) : 0;
                    vao.Draw(PrimitiveType.Triangles);
                    GameDrawingMatrix.Exit();
                }
            }
        }
        public static void DbgDrawCamera()
        {
            GL.PushMatrix();
            var center = new Vector2(Game.RenderSize.Width / 2, Game.RenderSize.Height / 2);
            var rect = Game.Track.Camera.getclamp(1, Game.RenderSize.Width, Game.RenderSize.Height);

            rect.Width *= Game.Track.Zoom;
            rect.Height *= Game.Track.Zoom;
            var circle = StaticRenderer.GenerateEllipse((float)rect.Width, (float)rect.Height, 100);

            var clamprect = new DoubleRect(center.X, center.Y, 0, 0);
            clamprect.Left -= rect.Width / 2;
            clamprect.Top -= rect.Height / 2;
            clamprect.Width = rect.Width;
            clamprect.Height = rect.Height;
            if (!Settings.SmoothCamera)
            {
                GL.Begin(PrimitiveType.LineStrip);
                GL.Color3(0, 0, 0);
                GL.Vertex2(clamprect.Left, clamprect.Top);
                GL.Vertex2(clamprect.Right, clamprect.Top);
                GL.Vertex2(clamprect.Right, clamprect.Bottom);
                GL.Vertex2(clamprect.Left, clamprect.Bottom);
                GL.Vertex2(clamprect.Left, clamprect.Top);
                GL.End();
                GL.PopMatrix();
                return;
            }
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color3(0, 0, 0);
            for (int i = 0; i < circle.Length; i++)
            {
                var pos = (Vector2d)center + (Vector2d)circle[i];
                var square = clamprect.Clamp(pos);
                var oval = clamprect.EllipseClamp(pos);
                pos = (Vector2d.Lerp(square, oval, CameraBoundingBox.roundness));
                GL.Vertex2(pos);
            }
            GL.End();
            // visualize example points being clamped
            GL.Begin(PrimitiveType.Lines);
            circle = StaticRenderer.GenerateEllipse((float)rect.Width / 1.5f, (float)rect.Height / 1.5f, 20);
            for (int i = 0; i < circle.Length; i++)
            {
                var pos = (Vector2d)center + (Vector2d)circle[i];
                var square = clamprect.Clamp(pos);
                var oval = clamprect.EllipseClamp(pos);
                pos = (Vector2d.Lerp(square, oval, CameraBoundingBox.roundness));
                if (pos != (Vector2d)center + (Vector2d)circle[i])
                {
                    GL.Vertex2(pos);
                    GL.Vertex2((Vector2d)center + (Vector2d)circle[i]);
                }
            }
            GL.End();
            GL.PopMatrix();
            //visualize rider center
            GameDrawingMatrix.Enter();
            center = (Vector2)Game.Track.Timeline.GetFrame(Game.Track.Offset).CalculateCenter();
            var circ = StaticRenderer.GenerateCircle(center.X, center.Y, 10, 360);
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color3(0, 0, 255);
            for (int i = 0; i < circ.Length; i++)
            {
                GL.Vertex2((Vector2)circ[i]);
            }
            GL.End();
            GameDrawingMatrix.Exit();
        }
        public static void DbgDrawGrid()
        {
            bool fastgrid = false;
            bool renderext = true;
            bool renderridersquare = true;
            int sqsize = fastgrid ? EditorGrid.CellSize : SimulationGrid.CellSize;
            GL.PushMatrix();
            GL.Scale(Game.Track.Zoom, Game.Track.Zoom, 0);
            GL.Translate(new Vector3d(Game.ScreenTranslation));
            GL.Begin(PrimitiveType.Quads);
            for (var x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
            {
                for (var y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
                {
                    var yv = new Vector2d(x + (Game.ScreenPosition.X - (Game.ScreenPosition.X % sqsize)), y + (Game.ScreenPosition.Y - (Game.ScreenPosition.Y % sqsize)));

                    if (!fastgrid)
                    {
                        var gridpos = new GridPoint((int)Math.Floor(yv.X / sqsize), (int)Math.Floor(yv.Y / sqsize));

                        if (Game.Track.GridCheck(yv.X, yv.Y))
                        {
                            if (Game.Track.RenderRider.PhysicsBounds.ContainsPoint(gridpos))
                                GL.Color3(Color.LightSlateGray);
                            else
                                GL.Color3(Color.Yellow);
                            GL.Vertex2(yv);
                            yv.Y += sqsize;
                            GL.Vertex2(yv);
                            yv.X += sqsize;
                            GL.Vertex2(yv);
                            yv.Y -= sqsize;
                            GL.Vertex2(yv);
                        }
                        if (renderridersquare)
                        {
                            if (Game.Track.RenderRider.PhysicsBounds.ContainsPoint(gridpos))
                            {
                                GL.Color3(Color.LightGray);
                                GL.Vertex2(yv);
                                yv.Y += sqsize;
                                GL.Vertex2(yv);
                                yv.X += sqsize;
                                GL.Vertex2(yv);
                                yv.Y -= sqsize;
                                GL.Vertex2(yv);
                            }
                        }
                    }
                    else if (Game.Track.FastGridCheck(yv.X, yv.Y))
                    {
                        GL.Color3(Color.Yellow);
                        GL.Vertex2(yv);
                        yv.Y += sqsize;
                        GL.Vertex2(yv);
                        yv.X += sqsize;
                        GL.Vertex2(yv);
                        yv.Y -= sqsize;
                        GL.Vertex2(yv);
                    }
                }
            }

            GL.End();
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.Red);
            for (var x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
            {
                var yv = new Vector2d(x + (Game.ScreenPosition.X - (Game.ScreenPosition.X % sqsize)), Game.ScreenPosition.Y);
                GL.Vertex2(yv);
                yv.Y += Game.RenderSize.Height / Game.Track.Zoom;
                GL.Vertex2(yv);
            }
            for (var y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
            {
                var yv = new Vector2d(Game.ScreenPosition.X, y + (Game.ScreenPosition.Y - (Game.ScreenPosition.Y % sqsize)));
                GL.Vertex2(yv);
                yv.X += Game.RenderSize.Width / Game.Track.Zoom;
                GL.Vertex2(yv);
            }
            GL.End();
            GL.PopMatrix();
            if (renderext)
            {
                using (var trk = Game.Track.CreateTrackReader())
                {
                    foreach (var v in trk.GetLinesInRect(Game.Track.Camera.GetViewport(), false))
                    {
                        if (v is StandardLine std)
                        {
                            if (std.Extension != StandardLine.Ext.None)
                            {
                                var d = std.Difference * std.ExtensionRatio;
                                if (std.Extension.HasFlag(StandardLine.Ext.Left))
                                {
                                    RenderRoundedLine(std.Position - d, std.Position, Color.Red, 1);
                                }
                                if (std.Extension.HasFlag(StandardLine.Ext.Right))
                                {
                                    RenderRoundedLine(std.Position2 + d, std.Position2, Color.Red, 1);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}