//
//  FPSCounter.cs
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

namespace linerider
{
    public class FPSCounter
    {
        private Queue<double> _queue = new Queue<double>();
        public double FPS
        {
            get
            {
                if (_queue.Count == 0)
                    return 1;
                var avg = _queue.Average();
                return 1.0 / avg;
            }
        }
        private double _framehistory = 20;
        public void AddFrame(double f)
        {
            _queue.Enqueue(f);
            if (_queue.Count > _framehistory)
            {
                _queue.Dequeue();
            }
        }
        public void Reset()
        {
            var last = _queue.LastOrDefault();
            _queue.Clear();
            _queue.Enqueue(last);
        }
        public void Reset(int fps)
        {
            for (int i = 0; i < _framehistory; i++)
            {
                AddFrame(1.0 / fps);
            }
        }
    }
}
