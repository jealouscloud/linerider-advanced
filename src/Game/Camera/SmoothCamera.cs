using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using linerider.Game;
using linerider.Rendering;
using OpenTK;
using linerider.Utils;
namespace linerider.Game
{
    public class SmoothCamera : ClampCamera
    {
        protected override Vector2d StepCamera(CameraBoundingBox box, ref Vector2d prev, int frame)
        {
            const int forwardcount = 40;
            EnsureFrame(frame + forwardcount);
            var entry = _frames[frame];
            Vector2d offset = box.Clamp(prev + entry.CameraOffset);
            var framebox = CameraBoundingBox.Create(
                _frames[frame].RiderCenter,
                _zoom);

            Vector2d center = Vector2d.Zero;
            int math = 0;
            for (int i = 0; i < forwardcount; i++)
            {
                var f = _frames[frame + i];
                offset = box.Clamp(offset + f.CameraOffset);
                center += framebox.Clamp(f.RiderCenter + offset);
                math++;
            }
            // force the rider to center at the beginning
            // it looks awkward to predict heavily at the start.
            if (frame < forwardcount)
            {
                return Vector2d.Lerp(
                    _frames[frame].RiderCenter,
                    center / math,
                    frame / (float)forwardcount) - entry.RiderCenter;
            }
            return (center / math) - entry.RiderCenter;
        }
    }
}
