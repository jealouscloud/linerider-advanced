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
    public class ClampCamera : ICamera
    {
        protected override Vector2d StepCamera(CameraBoundingBox box, ref Vector2d prev, int frame)
        {
            var entry = _frames[frame];
            return box.Clamp(prev + entry.CameraOffset);
        }
    }
}
