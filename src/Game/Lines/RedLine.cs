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
namespace linerider.Game
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
        public override LineType Type
        {
            get
            {
                return LineType.Red;
            }
        }
        public override System.Drawing.Color Color => Utils.Constants.RedLineColor;
        protected RedLine() : base()
        {
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
            if (base.Interact(ref p))
            {
                p = p.Replace(p.Location,p.Previous + _acc);
                return true;
            }
            return false;
        }
        public override GameLine Clone()
        {
            LineTrigger trigger = null;
            if (Trigger != null)
            {
                trigger = new LineTrigger()
                {
                    ZoomTrigger = Trigger.ZoomTrigger,
                    ZoomFrames = Trigger.ZoomFrames,
                    ZoomTarget = Trigger.ZoomTarget
                };
            }
            return new RedLine()
            {
                ID = ID,
                Difference = Difference,
                DiffNormal = DiffNormal,
                Distance = Distance,
                DotScalar = DotScalar,
                Extension = Extension,
                ExtensionRatio = ExtensionRatio,
                inv = inv,
                Position = Position,
                Position2 = Position2,
                Trigger = trigger,
                _acc = _acc,
                _multiplier = _multiplier
            };
        }
    }
}