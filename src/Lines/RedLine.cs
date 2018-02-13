//
//  RedLine.cs
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

using OpenTK;
using linerider.Game;
namespace linerider
{
    public class RedLine : StandardLine
    {
        private Vector2d _acc;
        private const double ConstAcc = 0.1;
        private int _multiplier = 1;
        public int Multiplier
        {
            get
            {
                return _multiplier;
            }
            set
            {
                _multiplier = value;
                CalculateConstants();
            }
        }
        protected RedLine(RedLine rl) : base(rl)
        {
            _multiplier = rl._multiplier;
            _acc = rl._acc;
        }
        public RedLine(Vector2d p1, Vector2d p2, bool inv = false) : base(p1, p2, inv) { }
        public override void CalculateConstants()
        {
            base.CalculateConstants();
            _acc = DiffNormal * (ConstAcc * _multiplier);
            _acc = inv ? _acc.PerpendicularRight : _acc.PerpendicularLeft;
        }
        public override bool Interact(ref SimulationPoint p)
        {
            if (Vector2d.Dot(p.Momentum, DiffNormal) > 0)
            {
                var startDelta = p.Location - this.Position;
                var doty = Vector2d.Dot(DiffNormal, startDelta);
                if (doty > 0 && doty < Zone)
                {
                    var dotx = Vector2d.Dot(startDelta, diff) * DotScalar;
                    if (dotx <= limit_right && dotx >= limit_left)
                    {
                        var pos = p.Location - doty * DiffNormal;
                        var friction = DiffNormal.Yx * p.Friction * doty;
                        if (p.Previous.X >= pos.X)
                            friction.X = -friction.X;
                        if (p.Previous.Y >= pos.Y)
                            friction.Y = -friction.Y;
                        p = p.Replace(pos, p.Previous + friction + _acc);
                        return true;
                    }
                }
            }
            return false;
        }
        public override StandardLine Clone()
        {
            return new RedLine(this);
        }
    }
}