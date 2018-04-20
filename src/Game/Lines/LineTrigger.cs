//
//  LineTrigger.cs
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

namespace linerider.Game
{
    public class LineTrigger
    {
        public bool ZoomTrigger = false;
        public float ZoomTarget = 4;
        public int ZoomFrames = 40;
        public LineTrigger()
        {
        }

        public bool Activate(int hitdelta, ref float currentzoom)
        {
            bool handled = false;
            if (ZoomTrigger)
            {
                if (currentzoom != ZoomTarget)
                {
                    if (hitdelta >= 0 && hitdelta < ZoomFrames)
                    {
                        var diff = ZoomTarget - currentzoom;
                        currentzoom = currentzoom + (diff / (ZoomFrames - hitdelta));
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
        public void Reset()
        {
        }
    }
}