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
    public class LineTrigger : GameService
    {
        public bool Enabled
        {
            get
            {
                return Zoomtrigger;
            }
        }

        public bool Zoomtrigger = false;
        public float ZoomTarget = 1;
        public int ZoomFrames = 40;
        private int zoomcounter = 0;

        public bool Activate()
        {
            if (Zoomtrigger)
            {
                if (zoomcounter == 0)
                {
                    if (Math.Abs(game.Track.Zoom - ZoomTarget) < 0.001)
                    {
                        return true;
                    }
                }
                zoomcounter++;
                if (ZoomFrames == 1)
                {
                    var diff = ZoomTarget - game.Track.Zoom;
                    game.Zoom(diff);
                }
                else
                {
                    var diff = ZoomTarget - game.Track.Zoom;
                    game.Zoom((float)diff / (ZoomFrames - zoomcounter));
                }
                if (zoomcounter >= ZoomFrames - 1)
                {
                    zoomcounter = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            zoomcounter = 0;
        }
    }
}