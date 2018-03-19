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
        private AutoArray<CameraEntry> _frames = new AutoArray<CameraEntry>();
        private float _cachezoom = 0;
        private Vector2d _cachepos = Vector2d.Zero;
        private int _cacheframe = -1;
        public ClampCamera()
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
                if (_cacheframe <= frame)
                    _cacheframe = -1;
            }
            if (frame == 1)
            {
                Rider firstframe = _timeline.GetFrame(0);
                var entry = new CameraEntry(firstframe.CalculateCenter());
                _frames[0] = entry;
            }
        }
        public override Vector2d GetFrameCamera(int frame)
        {
            if (_zoom != _cachezoom)
            {
                _cachezoom = _zoom;
                _cacheframe = -1;
                _cachepos = Vector2d.Zero;
            }
            EnsureFrame(frame);
            var offset = Calculate(frame);
            _cacheframe = frame;
            _cachepos = offset;
            return Clamp(_frames[frame].RiderCenter + offset, frame);
        }
        private Vector2d Calculate(int frame)
        {
            Vector2d next = Vector2d.Zero;
            if (_cacheframe != -1 &&
                _cacheframe <= frame &&
                (frame - _cacheframe) < 80)
            {
                if (_cacheframe == frame)
                    return _cachepos;
                next = ClampFrames(
                    _cacheframe + 1,
                    frame,
                    _cachepos);
                return next;
            }
            Vector2d framecenter = _frames[frame].RiderCenter;
            var box = CameraBoundingBox.Create(framecenter, _zoom);
            var framebounds = box.Bounds;
            int calcstart = FindStart(frame);
            return ClampFrames(calcstart, frame, Vector2d.Zero);
        }
        private Vector2d ClampFrames(int framestart, int frameend, Vector2d origin)
        {
            var box = CameraBoundingBox.Create(_frames[0].RiderCenter, _zoom);
            var width = box.Bounds.Width / 2;
            var height = box.Bounds.Height / 2;
            DoubleRect rect = new DoubleRect(-width, -height, width * 2, height * 2);
            Vector2d start = origin;
            for (int i = framestart; i <= frameend; i++)
            {
                start = rect.EllipseClamp(start + _frames[i].CameraOffset);
            }
            return start;
        }
        private int FindStart(int frame)
        {
            var box = CameraBoundingBox.Create(_frames[0].RiderCenter, _zoom);
            var width = box.Bounds.Width / 2;
            var height = box.Bounds.Height / 2;
            DoubleRect rect = new DoubleRect(-width, -height, width * 2, height * 2);
            Vector2d start = Vector2d.Zero;
            Angle firstbind = null;
            Vector2d first = Vector2d.Zero;
            for (int i = frame; i >= 0; i--)
            {
                var pos = start + _frames[i].CameraOffset;
                start = rect.EllipseClamp(pos);
                if (start != pos)
                {
                    if (firstbind == null)
                    {
                        firstbind = Angle.FromVector(start);
                        first = _frames[i].RiderCenter;
                    }
                    else
                    {
                        var cmp = _frames[i].RiderCenter - first;
                        cmp.X = Math.Abs(cmp.X);
                        cmp.Y = Math.Abs(cmp.Y);
                        if (cmp.X >= rect.Width &&
                            cmp.Y >= rect.Height)
                        {
                            var a2 = Angle.FromVector(start);
                            if (a2.Difference(firstbind) >= 90)
                            {
                                return i;
                            }
                        }
                    }
                }
            }
            return 0;
        }
        private Vector2d Clamp(Vector2d pos, int frame)
        {
            var entry = _frames[frame];
            CameraBoundingBox box = CameraBoundingBox.Create(entry.RiderCenter, 0);
            return box.Clamp(pos);
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
            var center = rider.CalculateCenter();
            var offset = prev.RiderCenter - center;
            return new CameraEntry(center, offset, rider.CalculateMomentum());
        }
        protected override double GetPPF(int frame)
        {
            EnsureFrame(frame);
            return _frames[frame].ppf;
        }
    }
}
