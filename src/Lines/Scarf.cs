using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using linerider.Tools;
using OpenTK;
namespace linerider.Lines
{
    public struct Scarf
    {
        public const float RestInterval = 2;
        public ScarfObject[] _anchors;
        private Vector2d last;
        public Scarf(Vector2d start)
        {
            last = start;
            _anchors = new ScarfObject[6];
            for (int i = 0; i < _anchors.Length; i++)
            {
                _anchors[i] = new ScarfObject(start - new Vector2d(RestInterval * (i + 1), 0), 0.9);
                _anchors[i].Prev = _anchors[i].Position;
            }
        }
        private Vector2d Repel(Vector2d a, Vector2d b, float mindistance)
        {
            var diff = (a - b);
            var dist = diff.Length;
            if (dist < mindistance)
            {
                var ratio = (dist - mindistance) / dist;
                if (diff.Length == 0)
                    ratio = 0;
                diff = diff * ratio;
                return b + diff;
            }
            return b;
        }
        private Vector2d Bind(Vector2d a, Vector2d b)
        {
            var diff = (a - b);
            var dist = diff.Length;
            var ratio = (dist - RestInterval) / dist;
            if (diff.Length == 0)
                ratio = 0;
            diff = diff * ratio;
            return b + diff;
        }
        private void ActivateJoint(Vector2d start, ref Vector2d center, ref Vector2d end)
        {
            center = Bind(start, center);
            end = Repel(start, end, RestInterval);
        }
        private Vector2d SnapToOrigin(Vector2d anchor, Vector2d origin, int anchorid)
        {
            var momentum = (origin - last);
            var momentum_normal = momentum / momentum.Length;
            return origin - (momentum_normal * ((anchorid + 1) * RestInterval));
        }
        public void Step(Vector2d origin)
        {
            Vector2d kinetics = Vector2d.Zero;
            for (int i = 0; i < _anchors.Length; i++)
            {
                _anchors[i].Tick();
            }
            var speed = Math.Min(2, Math.Abs((origin - last).Length / 25));
            var points = GetAnchors(origin);
            var momentum = origin - last;
            var momentum_unit = momentum.Normalized();
            for (int i = 1; i < points.Length; i++)
            {
                var start = points[i - 1];
                var center = points[i];
                
                var center_position = Vector2d.Dot((origin - center).Normalized().PerpendicularLeft, momentum_unit);//dot to find if left of plane
                var dot = 0.0;
                if (center_position > 0)
                    dot = Math.Max(0, Vector2d.Dot((center - start).Normalized(), momentum_unit.PerpendicularLeft));
                else if (center_position < 0)
                    dot = Math.Max(0, Vector2d.Dot((center - start).Normalized(), momentum_unit.PerpendicularRight));

                if (dot > 0)
                {
                    var diff = SnapToOrigin(center, origin, i) - center;
                    if (diff.Length < 1)
                    {
                        diff = (diff / diff.Length) * 2;
                    }
                    diff *= MathHelper.Clamp(speed*dot,0.01,1);
                    points[i] += diff;
                    //_anchors[i - 1].Prev -= diff/2;
                    kinetics -= diff/2;
                }
            }
            for (int i = 1; i < points.Length; i++)
            {
                if (i + 1 != points.Length)
                {
                    var center = points[i] + mathishard(kinetics,7-i);
                    var end = points[i + 1];
                    ActivateJoint(points[i - 1], ref center, ref end);
                    points[i] = center;
                    points[i + 1] = end;
                }
                else
                {
                    points[i] = Bind(points[i - 1], points[i]);
                }
                _anchors[i - 1].Position = points[i];
            }
            last = origin;
        }
        private Vector2d mathishard(Vector2d input, int times)
        {
            for (int i = 0; i < times; i++)
            {
                input /= 2;
            }
            return input;
        }
        public Vector2d[] GetAnchors(Vector2d origin)
        {
            var ret = new Vector2d[_anchors.Length + 1];
            ret[0] = origin;
            for (int i = 0; i < _anchors.Length; i++)
            {
                ret[i + 1] = _anchors[i].Position;
            }
            return ret;
        }
        public Scarf Clone()
        {
            Scarf ret = new Scarf();
            ret.last = last;
            ret._anchors = new ScarfObject[this._anchors.Length];
            for (int i = 0; i < ret._anchors.Length; i++)
            {
                var obj = new ScarfObject(_anchors[i].Position, _anchors[i].Friction);
                obj.Prev = _anchors[i].Prev;
                obj.Momentum = _anchors[i].Momentum;
                ret._anchors[i] = obj;
            }
            return ret;
        }
    }
}
