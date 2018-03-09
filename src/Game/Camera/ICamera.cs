//
//  Camera.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
    public abstract class ICamera
    {
        private float _blend = 1;
        private Vector2d _center = Vector2d.Zero;
        private Stack<Vector2d> _savestack = new Stack<Vector2d>();
        private Vector2d _cachedcenter = Vector2d.Zero;
        private Vector2d _cachedprevcenter = Vector2d.Zero;
        protected Timeline _timeline;
        protected int _currentframe = 0;
        protected float _zoom = 1;
        public abstract void InvalidateFrame(int frame);
        public abstract Vector2d GetFrameCamera(int frame);
        public void SetTimeline(Timeline timeline)
        {
            if (timeline == null)
                throw new Exception("Attempt to set null timeline for camera");
            if (_timeline != timeline)
            {
                _timeline = timeline;
                InvalidateFrame(1);
            }
        }
        public Vector2d GetCenter()
        {
            if (_center == Vector2d.Zero)
            {
                if (_cachedcenter == Vector2d.Zero)
                {
                    if (_blend != 1)
                    {
                        _cachedcenter = GetFrameCamera(_currentframe);
                        _cachedprevcenter = GetFrameCamera(Math.Max(0, _currentframe - 1));
                    }
                    else
                    {
                        _cachedcenter = GetFrameCamera(_currentframe);
                        _cachedprevcenter = _cachedcenter;
                    }
                }
                return Vector2d.Lerp(_cachedprevcenter, _cachedcenter, _blend);
            }
            return _center;
        }
        public void SetFrame(int frame)
        {
            _center = Vector2d.Zero;
            if (_currentframe != frame)
            {
                _currentframe = frame;
                _cachedcenter = Vector2d.Zero;
            }
        }
        public virtual void BeginFrame(float blend, float zoom)
        {
            if (_blend != blend)
            {
                _blend = blend;
                _cachedcenter = Vector2d.Zero;
            }
            _zoom = zoom;
        }
        public void SetFrameCenter(Vector2d center)
        {
            _center = center;
            _cachedcenter = Vector2d.Zero;
        }
        public void Push()
        {
            _savestack.Push(GetCenter());
        }

        public void Pop()
        {
            SetFrameCenter(_savestack.Pop());
        }
        public DoubleRect GetViewport(
            float zoom,
            int maxwidth,
            int maxheight)
        {
            var center = GetCenter();
            Vector2d size = new Vector2d(maxwidth / zoom, maxheight / zoom);
            var origin = center - (size / 2);
            return new DoubleRect(origin, size);
        }
        public DoubleRect getclamp(float zoom, int width, int height)
        {
            var ret = GetViewport(zoom, width, height);
            var pos = ret.Vector + (ret.Size / 2);
            var b = new CameraBoundingBox() { RiderPosition = pos };
            CameraBoundingBox box = new CameraBoundingBox();
            box.RiderPosition = _timeline.GetFrame(_currentframe).CalculateCenter();
            if (Settings.SmoothCamera)
            {
                var scale = box.GetSmoothCamRatio((float)_timeline.GetFrame(_currentframe).CalculateMomentum().Length);
                return b.GetBox(scale);
            }
            else
            {
                return box.GetBox(CameraBoundingBox.legacyratio);
            }
        }
    }
}
