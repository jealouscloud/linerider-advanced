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
using linerider.Lines;

namespace linerider.Rendering
{
    public static class GameRenderer
    {
        public static MainWindow Game;
        private static LineVAO _linevao = null;
        public static void DrawMomentum(Rider rider, LineVAO vao)
        {
            for (int i = 0; i < rider.Body.Length; i++)
            {
                var anchor = rider.Body[i];
                var vec1 = anchor.Location;
                var vec2 = vec1 + (anchor.Momentum);
                vao.AddLine(vec1, vec2, Color.Red, 1f / 2);
            }
        }
        public static void DrawContactPoints(Rider rider, List<int> diagnosis, LineVAO vao)
        {
            if (diagnosis == null)
                diagnosis = new List<int>();
            var bones = RiderConstants.Bones;
            for (var i = 0; i < bones.Length; i++)
            {
                var c = Color.FromArgb(unchecked((int)0xFFCC72B7));
                if (bones[i].Breakable)
                {
                    continue;
                }
                else if (bones[i].OnlyRepel)
                {
                    c = Color.CornflowerBlue;
                    vao.AddLine(
                        rider.Body[bones[i].joint1].Location,
                        rider.Body[bones[i].joint2].Location,
                        c,
                        1f / 4);
                }
                else if (i <= 3)
                {
                    vao.AddLine(
                        rider.Body[bones[i].joint1].Location,
                        rider.Body[bones[i].joint2].Location,
                        c,
                        1f / 4);
                }
            }
            if (!rider.Crashed && diagnosis.Count != 0)
            {
                Color firstbreakcolor = Color.FromArgb(unchecked((int)0xFFFF8C00));
                Color breakcolor = Color.FromArgb(unchecked((int)0xff909090)); ;
                for (int i = 1; i < diagnosis.Count; i++)
                {
                    var broken = diagnosis[i];
                    if (broken >= 0)
                    {
                        vao.AddLine(
                        rider.Body[bones[broken].joint1].Location,
                        rider.Body[bones[broken].joint2].Location,
                        breakcolor,
                        1f / 4);
                    }
                }
                //the first break is most important so we give it a better color, assuming its not just a fakie death
                if (diagnosis[0] > 0)
                {
                    vao.AddLine(
                    rider.Body[bones[diagnosis[0]].joint1].Location,
                    rider.Body[bones[diagnosis[0]].joint2].Location,
                    firstbreakcolor,
                    1f / 4);
                }
            }
            for (var i = 0; i < rider.Body.Length; i++)
            {
                Color c = Color.Cyan;
                if (
                    ((i == RiderConstants.SledTL || i == RiderConstants.SledBL) && diagnosis.Contains(-1)) ||
                    ((i == RiderConstants.BodyButt || i == RiderConstants.BodyShoulder) && diagnosis.Contains(-2)))
                {
                    c = Color.Blue;
                }
                vao.AddLine(
                    rider.Body[i].Location,
                    rider.Body[i].Location,
                    c,
                    1f / 4);
            }
        }

        public static void DrawTrackLine(StandardLine line, Color color, bool drawwell, bool drawcolor, bool drawknobs, bool redknobs = false)
        {
            var thickness = 2;
            Color color2;
            var type = line.Type;
            switch (type)
            {
                case LineType.Blue:
                    color2 = Color.FromArgb(0, 0x66, 0xFF);
                    break;

                case LineType.Red:
                    color2 = Color.FromArgb(0xCC, 0, 0);
                    break;

                default:
                    throw new Exception("Rendering Invalid Line");
            }
            if (drawcolor)
            {
                var loc3 = line.DiffNormal.X > 0 ? (Math.Ceiling(line.DiffNormal.X)) : (Math.Floor(line.DiffNormal.X));
                var loc4 = line.DiffNormal.Y > 0 ? (Math.Ceiling(line.DiffNormal.Y)) : (Math.Floor(line.DiffNormal.Y));
                if (type == LineType.Red)
                {
                    var redline = line as RedLine;
                    GameDrawingMatrix.Enter();
                    GL.Color3(color2);
                    GL.Begin(PrimitiveType.Triangles);
                    var basepos = line.Position2;
                    for (int ix = 0; ix < redline.Multiplier; ix++)
                    {
                        var angle = MathHelper.RadiansToDegrees(Math.Atan2(line.Difference.Y, line.Difference.X));
                        Turtle t = new Turtle(line.Position2);
                        var basex = 8 + (ix * 2);
                        t.Move(angle, -basex);
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                        t.Move(90, line.inv ? -8 : 8);
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                        t.Point = line.Position2;
                        t.Move(angle, -(ix * 2));
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                    }
                    GL.End();
                    GameDrawingMatrix.Exit();
                }
                RenderRoundedLine(new Vector2d(line.Position.X + loc3, line.Position.Y + loc4),
                    new Vector2d(line.Position2.X + loc3, line.Position2.Y + loc4), color2, thickness);
            }
            RenderRoundedLine(line.Position, line.Position2, color, thickness, drawknobs, redknobs);
            if (drawwell)
            {
                using (new GLEnableCap(EnableCap.Blend))
                {
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                    GameDrawingMatrix.Enter();
                    GL.Begin(PrimitiveType.Quads);
                    GL.Color4(new Color4(0, 0, 0, 40));
                    var rect = Utility.GetThickLine((Vector2)line.Start, (Vector2)line.End, Angle.FromLine(line.Start,line.End), (float)(StandardLine.Zone * 2));

                    GL.Vertex2(line.Start);
                    GL.Vertex2(line.End);
                    GL.Vertex2(rect[3]);
                    GL.Vertex2(rect[0]);
                    GL.End();
                    GL.PopMatrix();
                }
            }
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