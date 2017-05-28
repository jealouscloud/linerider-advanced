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
        public RedLine(Vector2d p1, Vector2d p2, bool inv = false) : base(p1, p2, inv) { }
        public override void CalculateConstants()
        {
            base.CalculateConstants();
            _acc = Perpendicular * (ConstAcc * _multiplier);
            _acc = inv ? _acc.PerpendicularRight : _acc.PerpendicularLeft;
        }
        public override bool Interact(DynamicObject obj)
        {
            if (!((obj.Momentum.X * Perpendicular.X) + (obj.Momentum.Y * Perpendicular.Y) > 0)) return false;

            var fp = Position;
            var diffx = obj.Position.X - fp.X;
            var diffy = obj.Position.Y - fp.Y;
            var cmpy = Perpendicular.X * diffx + Perpendicular.Y * diffy;
            if (!(cmpy > 0) || !(cmpy < Zone)) return false;

            double cmpx = (diffx * diff.X + diffy * diff.Y) * invSqrDis;

            if (!(cmpx >= Limleft) || !(cmpx <= Limright)) return false;

            obj.Position = new Vector2d(obj.Position.X - (cmpy * Perpendicular.X),
                obj.Position.Y - (cmpy * Perpendicular.Y));

            obj.Prev.X = obj.Prev.X +
                         Perpendicular.Y * obj.Friction * cmpy * (obj.Prev.X < obj.Position.X ? 1 : -1) + _acc.X;
            obj.Prev.Y = obj.Prev.Y -
                         Perpendicular.X * obj.Friction * cmpy * (obj.Prev.Y < obj.Position.Y ? -1 : 1) + _acc.Y;
            if (Trigger != null)
            {
                var track = game.Track;
                if (track.Playing && !track.ActiveTriggers.Contains(Trigger))
                {
                    track.ActiveTriggers.Add(Trigger);
                }
            }
            return true;
        }
    }
}