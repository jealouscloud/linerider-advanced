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

namespace linerider.Game
{
    public class GameTrigger
    {
        public const int TriggerTypes = 1;
        public int Start;
        public int End;
        public TriggerType TriggerType;
        public float ZoomTarget = 4;

        public bool CompareTo(GameTrigger other)
        {
            if (other == null)
                return false;
            return TriggerType == other.TriggerType &&
            Start == other.Start &&
            End == other.End &&
            ZoomTarget == other.ZoomTarget;
        }
        public bool ActivateZoom(int hitdelta, ref float currentzoom)
        {
            bool handled = false;
            if (TriggerType == TriggerType.Zoom)
            {
                int zoomframes = End - Start;
                if (currentzoom != ZoomTarget)
                {
                    if (hitdelta >= 0 && hitdelta < zoomframes)
                    {
                        var diff = ZoomTarget - currentzoom;
                        currentzoom = currentzoom + (diff / (zoomframes - hitdelta));
                        handled = true;
                    }
                    else
                    {
                        currentzoom = ZoomTarget;
                    }
                }
            }
            return handled;
        }
        public GameTrigger Clone()
        {
            return new GameTrigger()
            {
                Start = Start,
                End = End,
                TriggerType = TriggerType,
                ZoomTarget = ZoomTarget
            };
        }
    }
}