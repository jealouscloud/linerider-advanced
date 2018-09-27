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
    public class PredictiveCamera : ClampCamera
    {
        public override Vector2d GetFrameCamera(int frame)
        {
            base.GetFrameCamera(frame);

            var box = CameraBoundingBox.Create(_frames[frame].RiderCenter, _zoom);
            return box.Clamp(CameraMotionReducer(frame));
        }
        /// <summary>
        /// reduces the amount of movement the camera has to do to capture the
        /// rider. It does so predictively
        /// </summary>
        /// <returns>The reduced position in game coords</returns>
        private Vector2d CameraMotionReducer(int frame)
        {
            const int forwardcount = 40;
            EnsureFrame(frame + forwardcount);
            Vector2d offset = CalculateOffset(frame);
            var box = CameraBoundingBox.Create(Vector2d.Zero, _zoom);
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
                    frame / (float)forwardcount);
            }
            return center / math;
        }
        protected override Vector2d StepCamera(CameraBoundingBox box, ref Vector2d prev, int frame)
        {
            var entry = _frames[frame];
            return box.Clamp(prev + entry.CameraOffset);
        }
    }
}
