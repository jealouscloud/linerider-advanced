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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace linerider.Utils
{
    public class AsyncTask : IDisposable
    {
        private Action _action;
        private Action _oncompletion;
        private Func<bool> _condition;
        private Task _task = null;
        private readonly object _sync = new object();
        private bool running = false;
        public AsyncTask(Action action, Func<bool> condition, Action oncompletion)
        {
            _action = action;
            _condition = condition;
            _oncompletion = oncompletion;
        }
        private void ThreadProc()
        {
            while (true)
            {
                lock (_sync)
                {
                    if (!_condition())
                    {
                        running = false;
                        break;
                    }
                }
                _action.Invoke();
            }
            _oncompletion();

        }
        public void RunSynchronously()
        {
            bool wait = false;
            lock (_sync)
            {
                if (running)
                {
                    wait = true;
                }
            }
            if (wait)
            {
                _task.Wait();
            }
            else
            {
                ThreadProc();
            }
        }
        public void EnsureRunning()
        {
            lock (_sync)
            {
                if (!running)
                {
                    running = true;
                    _task?.Dispose();
                    _task = Task.Run((Action)ThreadProc);
                }
            }
        }
        public void Dispose()
        {
            _task?.Dispose();
        }
    }
}
