//
//  Line.cs
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
using System;
using linerider.Game;
using linerider.Lines;
using linerider.Utils;
using System.Drawing;
namespace linerider
{
    public class Line : GameService
    {
        public static readonly Color RedLineColor = Color.FromArgb(0xCC, 0, 0);
        public static readonly Color BlueLineColor = Color.FromArgb(0, 0x66, 0xFF);
        public static Color SceneryLineColor = Color.FromArgb(0, 0xCC, 0);
        public Vector2d Position { get; set; }
        public Vector2d Position2 { get; set; }
        public Vector2d diff;
        public int ID = -1;

        public override string ToString()
        {
            return "ID: " + ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }

        protected Line()
        {
        }

        public Line(Vector2d p1, Vector2d p2)
        {
            Position = p1;
            Position2 = p2;
        }

        public virtual void CalculateConstants()
        {
        }
        public Color GetColor()
        {
            switch (GetLineType())
            {
                case LineType.Blue:
                    return BlueLineColor;
                case LineType.Red:
                    return RedLineColor;
                case LineType.Scenery:
                    return SceneryLineColor;
                default:
                    throw new Exception("Unable to get the color for this line, its type is unknown");
            }
        }
        public virtual LineState GetState()
        {
            return new LineState() { Pos1 = Position, Pos2 = Position2, Parent = this, Inverted = false };
        }
        public virtual bool Interact(ref SimulationPoint p)
        {
            return false;
        }
        public static double GetLineRadius(Line line)
        {
            if (line is SceneryLine)
            {
                return 1 * ((SceneryLine)line).Width;
            }
            return 1;
        }

        public static bool DoesLineIntersectRect(Line l1, FloatRect rect)
        {
            Vector2 ps1 = (Vector2)l1.Position;
            Vector2 pe1 = (Vector2)l1.Position2;
            if (rect.Contains(ps1.X, ps1.Y) || rect.Contains(pe1.X, pe1.Y))
                return true;
            Vector2 tl = new Vector2(rect.Left, rect.Top);
            Vector2 tr = tl;
            tr.X += rect.Width;
            Vector2 bl = tl;
            bl.Y += rect.Height;
            Vector2 br = bl;
            bl.X += rect.Width;
            return Intersects(ps1, pe1, tl, bl) ||
                Intersects(ps1, pe1, tl, tr) ||
                Intersects(ps1, pe1, tr, br) ||
                Intersects(ps1, pe1, bl, br);
        }

        public static bool DoLinesIntersect(Line l1, Line l2)
        {
            return Intersects(l1.Position, l1.Position2, l2.Position, l2.Position2);
        }

        public static bool Intersects(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            Vector2 p;
            return Intersects(a1, a2, b1, b2, out p);
        }
        public static bool Intersects(Vector2d a1, Vector2d a2, Vector2d b1, Vector2d b2)
        {
            Vector2d p;
            return Intersects(a1, a2, b1, b2, out p);
        }

        public static bool Intersects(Vector2d a1, Vector2d a2, Vector2d b1, Vector2d b2, out Vector2d intersection)
        {
            intersection = new Vector2d(0, 0);

            Vector2d b = a2 - a1;
            Vector2d d = b2 - b1;
            double bDotDPerp = b.X * d.Y - b.Y * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (Math.Abs(bDotDPerp) < double.Epsilon)
                return false;

            Vector2d c = b1 - a1;
            double t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return false;

            double u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return false;

            intersection = a1 + t * b;

            return true;
        }
        public static bool Intersects(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            intersection = new Vector2(0, 0);

            Vector2 b = a2 - a1;
            Vector2 d = b2 - b1;
            float bDotDPerp = b.X * d.Y - b.Y * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (Math.Abs(bDotDPerp) < float.Epsilon)
                return false;

            Vector2 c = b1 - a1;
            float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return false;

            float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return false;

            intersection = a1 + t * b;

            return true;
        }

        public LineType GetLineType()
        {
            if (this is RedLine)
                return LineType.Red;
            if (this is SceneryLine)
                return LineType.Scenery;
            if (this is StandardLine)
                return LineType.Blue;
            return LineType.All;
        }
    }
}