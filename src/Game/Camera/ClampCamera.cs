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
            Vector2d ret;
            EnsureFrame(frame);
            ret = Calculate(frame);
            _cacheframe = frame;
            _cachepos = ret;
            return Clamp(ret, frame);
        }
        private Vector2d Calculate(int frame)
        {
            if (_cacheframe != -1 &&
                _cacheframe <= frame &&
                (frame - _cacheframe) < 80)
            {
                return ClampFrames(
                    _cacheframe,
                    (frame - _cacheframe),
                    _cachepos);
            }
            Vector2d framecenter = _frames[frame].RiderCenter;
            var box = new CameraBoundingBox(framecenter);
            if (Settings.RoundLegacyCamera)
                box.SetupSmooth(_frames[frame].ppf, _zoom);
            else
                box.SetupLegacy(_zoom);
            var framebounds = box.Bounds;
            int calcstart = 0;
            var ret = framecenter;
            var xfound = false;
            var yfound = false;
            for (int i = frame; i >= 0; i--)
            {
                var current = _frames[i].RiderCenter;
                var cmp = framecenter - current;
                // todo, i have no idea why this works.
                // seriously, ive tried so much stuff, but for the ellipse
                // camera i can't seem to make it work any better than with
                // width or height * 2
                if (Math.Abs(cmp.X) >= framebounds.Width * 2)
                {
                    xfound = true;
                }
                if (Math.Abs(cmp.Y) >= framebounds.Height * 2)
                {
                    yfound = true;
                }
                if (xfound && yfound)
                {
                    calcstart = i;
                    break;
                }
            }
            var calccount = (frame - calcstart);
            return ClampFrames(calcstart, calccount, _frames[calcstart].RiderCenter);
        }
        private Vector2d ClampFrames(int frame, int count, Vector2d start)
        {
            Vector2d ret = start;
            for (int i = frame; i < frame + count; i++)
            {
                ret = Clamp(ret, i);
            }
            if (count > 1)
                Debug.WriteLine("Calculating " + count + " for legacy camera");
            return ret;
        }
        private Vector2d Clamp(Vector2d pos, int frame)
        {
            var entry = _frames[frame];
            CameraBoundingBox box = new CameraBoundingBox(entry.RiderCenter);
            if (Settings.RoundLegacyCamera)
                box.SetupSmooth(entry.ppf, _zoom);
            else
                box.SetupLegacy(_zoom);
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
                for (int i = 0; i < diff; i++)
                {
                    var rider = frames[i];
                    var center = rider.CalculateCenter();
                    var entry = new CameraEntry(center, center, rider.CalculateMomentum());
                    _frames.Add(entry);
                }
            }
        }
        protected override double GetPPF(int frame)
        {
            EnsureFrame(frame);
            return _frames[frame].ppf;
        }
    }
}
