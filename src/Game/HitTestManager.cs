using linerider.Rendering;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using linerider.Game;
using linerider.Utils;
using System.Diagnostics;

namespace linerider
{
    public class HitTestManager
    {
        private HashSet<int> _allcollisions = new HashSet<int>();
        private List<HashSet<int>> _unique_frame_collisions = new List<HashSet<int>>();
        private HashSet<int> _changed = new HashSet<int>();
        private Dictionary<int, int> _line_framehit = new Dictionary<int, int>();
        private ResourceSync _sync = new ResourceSync();
        private int _currentframe = 0;
        const int Disabled = -1;
        public HitTestManager()
        {
            Reset();
        }
        public void AddFrame(LinkedList<int> collisions)
        {
            int frameid = _unique_frame_collisions.Count;
            using (_sync.AcquireWrite())
            {
                HashSet<int> unique = new HashSet<int>();
                foreach (var collision in collisions)
                {
                    var id = collision;
                    if (_allcollisions.Add(id))
                    {
                        _changed.Add(id);
                        unique.Add(id);
                        _line_framehit.Add(id, frameid);
                    }
                }
                _unique_frame_collisions.Add(unique);
            }
        }
        public void ChangeFrames(int start, List<LinkedList<int>> collisions)
        {
            Debug.Assert(start != 0, "Attempt to change frame 0 from hit test");
            using (_sync.AcquireWrite())
            {
                var frames = _unique_frame_collisions;
                for (int i = 0; i < collisions.Count; i++)
                {
                    foreach (var hit in frames[start + i])
                    {
                        _allcollisions.Remove(hit);
                        _changed.Add(hit);
                        _line_framehit.Remove(hit);
                    }
                }
                for (int i = 0; i < collisions.Count; i++)
                {
                    HashSet<int> unique = new HashSet<int>();
                    foreach (var collision in collisions[i])
                    {
                        var id = collision;
                        if (_allcollisions.Add(id))
                        {
                            _changed.Add(id);
                            unique.Add(id);
                            _line_framehit.Add(id, start + i);
                        }
                    }
                    frames[start + i] = unique;
                }
            }
        }

        public HashSet<int> SetFrame(int newframe)
        {
            using (_sync.AcquireWrite())
            {
                var ret = new HashSet<int>();
                foreach (var v in _changed)
                {
                    ret.Add(v);
                }
                _changed.Clear();
                if (!Settings.Local.HitTest)
                {
                    if (_currentframe != Disabled)
                    {
                        foreach (var v in _allcollisions)
                        {
                            ret.Add(v);
                        }
                        _currentframe = Disabled;
                    }
                    return ret;
                }

                if (_currentframe != newframe)
                {
                    if (_currentframe == Disabled)
                        _currentframe = 0;
                    // i'm leaving this in seperate loops for now
                    // it's a lot more readable this way for changes
                    if (newframe < _currentframe)
                    {
                        // we're moving backwards.
                        // we compare to currentframe because we may have to
                        // remove its hit lines
                        for (int i = newframe; i <= _currentframe; i++)
                        {
                            foreach (var id in _unique_frame_collisions[i])
                            {
                                var framehit = _line_framehit[id];
                                //was hit, but isnt now
                                if (framehit > newframe)
                                    ret.Add(id);
                            }
                        }
                    }
                    else
                    {
                        // we're moving forwards
                        // we ignore currentframe, its render data is
                        // established
                        for (int i = _currentframe + 1; i <= newframe; i++)
                        {
                            foreach (var id in _unique_frame_collisions[i])
                            {
                                ret.Add(id);
                            }
                        }
                    }
                    _currentframe = newframe;
                }
                return ret;
            }
        }
        public bool IsHit(int id)
        {
            using (_sync.AcquireRead())
            {
                if (_line_framehit.TryGetValue(id, out int frameid))
                {
                    if (_currentframe >= frameid)
                        return true;
                }
                return false;
            }
        }
        public bool IsHitBy(int id, int frame)
        {
            using (_sync.AcquireRead())
            {
                if (_line_framehit.TryGetValue(id, out int frameid))
                {
                    if (frame >= frameid)
                        return true;
                }
                return false;
            }
        }

        public void Reset()
        {
            using (_sync.AcquireWrite())
            {
                foreach (var v in _allcollisions)
                {
                    _changed.Add(v);
                }
                _unique_frame_collisions.Clear();
                _unique_frame_collisions.Add(new HashSet<int>());
                _line_framehit.Clear();
                _allcollisions.Clear();
                _currentframe = 0;
            }
        }
    }
}