//
//  nsvg.cs
//
// Copyright(c) 2013-14 Mikko Mononen memon @inside.org
//
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
// claim that you wrote the original software.If you use this software
// in a product, an acknowledgment in the product documentation would be
// appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//
//
//
// A port of code from https://github.com/memononen/nanosvg
using NGraphics;
using OpenTK;
using System;
using System.Collections.Generic;

namespace linerider.Drawing
{
    internal class nsvg
    {
        private static double sqr(double x)
        {
            return x * x;
        }

        private static double nsvg__vmag(double x, double y)
        { return Math.Sqrt(x * x + y * y); }

        private static double nsvg__vecrat(double ux, double uy, double vx, double vy)
        {
            return (ux * vx + uy * vy) / (nsvg__vmag(ux, uy) * nsvg__vmag(vx, vy));
        }

        private static double nsvg__vecang(double ux, double uy, double vx, double vy)
        {
            double r = nsvg__vecrat(ux, uy, vx, vy);
            if (r < -1.0f) r = -1.0f;
            if (r > 1.0f) r = 1.0f;
            return ((ux * vy < uy * vx) ? -1.0 : 1.0) * Math.Acos(r);
        }

        private static void nsvg__xformPoint(out double dx, out double dy, double x, double y, double[] t)
        {
            dx = x * t[0] + y * t[2] + t[4];
            dy = x * t[1] + y * t[3] + t[5];
        }

        private static void nsvg__xformVec(out double dx, out double dy, double x, double y, double[] t)
        {
            dx = x * t[0] + y * t[2];
            dy = x * t[1] + y * t[3];
        }

        public static List<Vector2> nsvg__pathArcTo(Vector2d start, ArcTo a2)
        {
            List<Vector2> ret = new List<Vector2>();
            Vector2d end = start;
            // Ported from canvg (https://code.google.com/p/canvg/)
            double rx, ry, rotx;
            double x1, y1, x2, y2, cx, cy, dx, dy, d;
            double x1p, y1p, cxp, cyp, s, sa, sb;
            double ux, uy, vx, vy, a1, da;
            double x, y, tanx, tany, a, px = 0, py = 0, ptanx = 0, ptany = 0;
            double[] t = new double[6];
            double sinrx, cosrx;
            bool fa, fs;
            int ndivs;
            double hda, kappa;

            rx = Math.Abs(a2.Radius.Width);
            ry = Math.Abs(a2.Radius.Height);
            rotx = a2.xrotation / 180.0f * Math.PI;      // x rotation engle
            fa = a2.LargeArc;
            fs = a2.SweepClockwise;
            x1 = start.X;                          // start point
            y1 = start.Y;

            x2 = a2.EndPoint.X;
            y2 = a2.EndPoint.Y;

            dx = x1 - x2;
            dy = y1 - y2;
            d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6f || rx < 1e-6f || ry < 1e-6f)
            {
                // The arc degenerates to a line
                //nsvg__lineTo(p, x2, y2);
                end.X = x2;
                end.Y = y2;
                ret.Add((Vector2)end);
                return ret;
            }

            sinrx = Math.Sin(rotx);
            cosrx = Math.Cos(rotx);

            // Convert to center point parameterization.
            // http://www.w3.org/TR/SVG11/implnote.html#ArcImplementationNotes
            // 1) Compute x1', y1'
            x1p = cosrx * dx / 2.0f + sinrx * dy / 2.0f;
            y1p = -sinrx * dx / 2.0f + cosrx * dy / 2.0f;
            d = sqr(x1p) / sqr(rx) + sqr(y1p) / sqr(ry);
            if (d > 1)
            {
                d = Math.Sqrt(d);
                rx *= d;
                ry *= d;
            }
            // 2) Compute cx', cy'
            s = 0.0f;
            sa = sqr(rx) * sqr(ry) - sqr(rx) * sqr(y1p) - sqr(ry) * sqr(x1p);
            sb = sqr(rx) * sqr(y1p) + sqr(ry) * sqr(x1p);
            if (sa < 0.0f) sa = 0.0f;
            if (sb > 0.0f)
                s = Math.Sqrt(sa / sb);
            if (fa == fs)
                s = -s;
            cxp = s * rx * y1p / ry;
            cyp = s * -ry * x1p / rx;

            // 3) Compute cx,cy from cx',cy'
            cx = (x1 + x2) / 2.0f + cosrx * cxp - sinrx * cyp;
            cy = (y1 + y2) / 2.0f + sinrx * cxp + cosrx * cyp;

            // 4) Calculate theta1, and delta theta.
            ux = (x1p - cxp) / rx;
            uy = (y1p - cyp) / ry;
            vx = (-x1p - cxp) / rx;
            vy = (-y1p - cyp) / ry;
            a1 = nsvg__vecang(1.0, 0.0, ux, uy);  // Initial angle
            da = nsvg__vecang(ux, uy, vx, vy);      // Delta angle

            //  	if (nsvg__vecrat(ux,uy,vx,vy) <= -1.0f) da = Math.PI;
            //  	if (nsvg__vecrat(ux,uy,vx,vy) >= 1.0f) da = 0;

            if (fa)
            {
                // Choose large arc
                if (da > 0.0f)
                    da = da - 2 * Math.PI;
                else
                    da = 2 * Math.PI + da;
            }

            // Approximate the arc using cubic spline segments.
            t[0] = cosrx;
            t[1] = sinrx;
            t[2] = -sinrx;
            t[3] = cosrx;
            t[4] = cx;
            t[5] = cy;

            // Split arc into max 90 degree segments.
            // The loop assumes an iteration per end point (including start and end), this +1.
            ndivs = (int)(Math.Abs(da) / (Math.PI * 0.5f) + 1.0f);
            hda = (da / (double)ndivs) / 2.0f;
            kappa = Math.Abs(4.0f / 3.0f * (1.0f - Math.Cos(hda)) / Math.Sin(hda));
            if (da < 0.0f)
                kappa = -kappa;

            var last = (Vector2)start;
            for (int i = 0; i <= ndivs; i++)
            {
                a = a1 + da * (i / (double)ndivs);
                dx = Math.Cos(a);
                dy = Math.Sin(a);
                nsvg__xformPoint(out x, out y, dx * rx, dy * ry, t); // position
                nsvg__xformVec(out tanx, out tany, -dy * rx * kappa, dx * ry * kappa, t); // tangent
                if (i > 0)
                {
                    BezierCurveCubic b = new BezierCurveCubic((Vector2)last, new Vector2((float)x, (float)y),
                        (Vector2)new Vector2d(px + ptanx, py + ptany),
                        (Vector2)new Vector2d(x - tanx, y - tany));
                    Vector2 old = b.CalculatePoint(0f);
                    ret.Add(old);
                    var precision = 0.05f;
                    for (float p = precision; p < 1f + precision; p += precision)
                    {
                        Vector2 j = b.CalculatePoint(p);
                        ret.Add(j);
                    }
                    last = b.CalculatePoint(1.0f);
                }
                px = x;
                py = y;
                ptanx = tanx;
                ptany = tany;
            }

            end.X = x2;
            end.Y = y2;
            ret.Add((Vector2)end);
            return ret;
        }
    }
}