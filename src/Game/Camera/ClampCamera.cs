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
        private const int cacherate = 40;
        private AutoArray<CameraEntry> _frames = new AutoArray<CameraEntry>();
        private AutoArray<Vector2d> _framecache = new AutoArray<Vector2d>();
        private float _cachezoom = 0;
        private Vector2d _prevcamera = Vector2d.Zero;
        private int _prevframe = -1;
        public ClampCamera()
        {
            _frames.Add(new CameraEntry(Vector2d.Zero));
            _framecache.Add(Vector2d.Zero);
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
                if (_prevframe <= frame)
                    _prevframe = -1;
            }
            var cachepos = (frame / cacherate);
            if (frame % cacherate != 0)
                cachepos++;

            if (cachepos < _frames.Count)
            {
                _framecache.RemoveRange(cachepos, _framecache.Count - cachepos);
            }
            if (frame == 1)
            {
                Rider firstframe = _timeline.GetFrame(0);
                var entry = new CameraEntry(firstframe.CalculateCenter());
                _frames[0] = entry;
                _framecache[0] = entry.RiderCenter;
            }
        }
        public override Vector2d GetFrameCamera(int frame)
        {
            if (_zoom != _cachezoom)
            {
                _cachezoom = _zoom;
                _prevframe = -1;
                _framecache.UnsafeSetCount(1);
            }
            EnsureFrame(frame);
            var offset = Calculate(frame);
            _prevframe = frame;
            _prevcamera = offset;
            return _frames[frame].RiderCenter + offset;
        }
        private Vector2d Calculate(int frame)
        {
            var box = CameraBoundingBox.Create(Vector2d.Zero, _zoom);
            if (_prevframe != -1 &&
                _prevframe <= frame &&
                (frame - _prevframe) <= 1)
            {
                if (frame == _prevframe)
                    return _prevcamera;
                if (frame % cacherate != 0)
                    return box.Clamp(_prevcamera + _frames[frame].CameraOffset);
            }
            int cachepos = Math.Min(frame / cacherate, _framecache.Count - 1);
            int framestart = cachepos * cacherate;
            Vector2d start = _framecache[cachepos];

            for (int i = framestart; i <= frame; i++)
            {
                if (i % cacherate == 0 && i / cacherate == _framecache.Count)
                {
                    _framecache.Add(start);
                }
                start = box.Clamp(start + _frames[i].CameraOffset);
            }
            // Debug.WriteLine("Calculating " + framestart + "-" + (frame) + " for legacy camera");
            return start;
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
            return new CameraEntry(center, offset, Vector2d.Zero);
        }
    }
}
