using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using linerider.Game;
using linerider.Rendering;
using OpenTK;
using linerider.Utils;
using System.Diagnostics;

namespace linerider.Game
{
    public class SoftCamera : ClampCamera
    {
        protected override Vector2d StepCamera(CameraBoundingBox box, ref Vector2d prev, int frame)
        {
            const double push = 0.8;
            const double pull = 0.01;

            CameraEntry entry = _frames[frame];
            var ret = box.Clamp(prev + entry.CameraOffset);
            Angle a = Angle.FromVector(ret);
            double length = ret.Length;
            double prevlength = prev.Length;
            var edge = a.MovePoint(
                Vector2d.Zero,
                Math.Max(box.Bounds.Width, box.Bounds.Height));
            double maxlength = box.Clamp(edge).Length;
            double lengthratio = length / maxlength;
            double prevratio = prevlength / maxlength;
            double diffratio = Math.Abs(lengthratio - prevratio);
            if (length > prevlength)
            {
                var dr = lengthratio - prevratio;
                var damper = lengthratio - (dr / 2);

                var delta = length - prevlength;
                delta *= Math.Max(0.05, push * (1 - Math.Max(0, damper)));
                length = prevlength + delta;
            }

            length -= length * pull;
            return box.Clamp(a.MovePoint(Vector2d.Zero, length));
        }
    }
}
