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
        private AutoArray<Vector2d> _poscache = new AutoArray<Vector2d>();
        private float _cachezoom = 0;
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
                var cache = frame / 40;
                if (cache < _poscache.Count)
                {
                    _poscache.RemoveRange(cache, _poscache.Count - cache);
                }
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
            if (_zoom != _cachezoom)
            {
                _poscache.Clear();
                _cachezoom = _zoom;
            }
            var framenumber = Settings.SmoothCamera ? frame + 1 : frame;
            EnsureFrame(framenumber);
            var ret = Calculate(framenumber);
            return Clamp(ret, _frames[frame]);
        }
        private Vector2d Calculate(int frame)
        {
            Vector2d ret = _frames[0].RiderCenter;
            int start = 0;
            var cached = ((_poscache.Count - 1) * 40);
            if (_poscache.Count != 0)
            {
                ret = _poscache[_poscache.Count - 1];
                start = cached;
            }
            for (int i = start; i <= frame; i++)
            {
                if (i % 40 == 0 && (i != cached))
                {
                    _poscache.Add(ret);
                }
                ret = Clamp(ret, _frames[i]);
            }
            return ret;
        }
        private Vector2d Clamp(Vector2d pos, CameraEntry entry)
        {
            CameraBoundingBox box = new CameraBoundingBox()
            {
                RiderPosition = entry.RiderCenter
            };
            var smooth = box.SmoothClamp(pos, entry.ppf);
            var legacy = box.Clamp(pos);
            return Settings.SmoothCamera ? smooth : legacy;
        }
        private void EnsureFrame(int frame)
        {
            //ensure timeline has the frames for us
            //also timeline might invalidate our prev frames when calling
            //so we do this at the top so it doesnt invalidate while calculating
            _timeline.GetFrame(frame);
            if (frame >= _frames.Count)
            {
                //todo handle properly
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
    }
}
