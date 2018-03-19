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
    public class Camera : ICamera
    {
        private AutoArray<CameraEntry> _frames = new AutoArray<CameraEntry>();
        public Camera()
        {
            _frames.Add(new CameraEntry(Vector2d.Zero));
        }
        public override void InvalidateFrame(int frame)
        {
            if (frame <= 0)
                throw new Exception("Cannot invalidate frame 0 for camera");
            if (frame < _frames.Count)
            {
                _frames.RemoveRange(
                    frame,
                    _frames.Count - frame);
            }
            if (frame == 1)
            {
                var firstframe = _timeline.GetFrame(0);
                var entry = new CameraEntry(firstframe.CalculateCenter());
                _frames[0] = entry;
            }
        }
        public override Vector2d GetFrameCamera(int frame)
        {
            EnsureFrame(frame);
            return Clamp(CameraMotionReducer(frame), _frames[frame].RiderCenter, GetPPF(frame));
        }
        protected override double GetPPF(int frame)
        {
            return GetAvgMomentum(frame);
        }
        private Vector2d GetFrame(int frame)
        {
            EnsureFrame(frame);
            return _frames[frame].CameraOffset;
        }
        private void EnsureFrame(int frame)
        {
            //ensure timeline has the frames for us
            //also timeline might invalidate our prev frames when calling
            //so we do this at the top so it doesnt invalidate while calculating
            _timeline.GetFrame(frame);
            if (frame >= _frames.Count)
            {
                var diff = frame - (_frames.Count - 1);
                var frames = _timeline.GetFrames(_frames.Count, diff);
                var camoffset = _frames[_frames.Count - 1];
                for (int i = 0; i < diff; i++)
                {
                    camoffset = GetNext(camoffset, frames[i]);
                    _frames.Add(camoffset);
                }
            }
        }
        private CameraEntry GetNext(CameraEntry prev, Rider rider)
        {
            // todo i really wish this cared about angles
            // it shouldnt center if the next step is going towards the center 
            // of the previous
            // something like that
            var center = rider.CalculateCenter();
            var prevoffs = prev.CameraOffset;
            var offset = (center - prev.CameraOffset);
            offset = Vector2d.Lerp(prevoffs, center, 0.15);
            return new CameraEntry(center, offset, rider.CalculateMomentum());
        }
        private Vector2d Clamp(Vector2d pos, Vector2d center, double ppf)
        {
            CameraBoundingBox box = new CameraBoundingBox(center);
            if (Settings.SmoothCamera)
                box.SetupSmooth(ppf, _zoom);
            else
                box.SetupLegacy(_zoom);
            return box.Clamp(pos);
        }
        private Vector2d Clamp(Vector2d pos, CameraEntry entry)
        {
            return Clamp(pos, entry.RiderCenter, entry.ppf);
        }
        private double GetAvgMomentum(int frame)
        {
            int count = 10;
            count = Math.Min(frame, count);
            EnsureFrame(frame + count);
            if (count == 0)
                return _frames[frame].ppf;
            int math = 0;
            double cam = 0;
            for (int i = count; i >= 0; i--)
            {
                var f = _frames[frame + i];
                cam += f.ppf;
                math++;
            }
            return cam / (math);
        }
        /// <summary>
        /// reduces the amount of movement the camera has to do to capture the
        /// rider. It does so predictively
        /// </summary>
        /// <returns>The reduced position in game coords</returns>
        private Vector2d CameraMotionReducer(int frame)
        {
            int count = 40;
            count = Math.Min(frame, count);
            if (count == 0)
                return GetFrame(frame);
            EnsureFrame(frame + count);
            Vector2d center = Vector2d.Zero;
            int math = 0;
            //unknown magic values
            for (int i = count; i >= 0; i--)
            {
                var f = _frames[frame + i];
                center += Clamp(f.CameraOffset, _frames[frame].RiderCenter, GetAvgMomentum(frame + i));
                math++;
            }
            return center / (math);
        }
        /// <summary>
        /// reduces the amount of change the rider can move within the camera
        /// </summary>
        /// <returns>The reduced position in game coords</returns>
        private Vector2d RelativeMotionReducer(int frame)
        {
            //currently an avg of rider centers
            int count = 20;
            count = Math.Min(frame, count);
            if (count == 0)
                return CameraMotionReducer(frame);
            Vector2d center = Vector2d.Zero;
            int math = 0;
            EnsureFrame(frame + count);
            var min = -Math.Min(20, frame);
            var framedata = _frames[frame];
            for (int i = count; i >= min; i--)
            {
                var f = _frames[frame + i];
                center += Clamp(CameraMotionReducer(frame + i), f) - f.RiderCenter;
                math++;
            }
            center /= math;
            return framedata.RiderCenter + center;
        }
        //blue
        public Vector2d GetSmoothedCameraOffset()
        {
            return RelativeMotionReducer(_currentframe);
        }
        //red
        public Vector2d GetSmoothPosition()
        {
            return CameraMotionReducer(_currentframe);
        }
    }
}
