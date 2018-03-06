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
        private HashSet<int> _renderer_changelist = new HashSet<int>();
        private Dictionary<int, int> _line_framehit = new Dictionary<int, int>();
        private ResourceSync _sync = new ResourceSync();
        private int _currentframe = Disabled;
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
                        if (Settings.Local.HitTest)
                            _renderer_changelist.Add(id);
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
                        if (Settings.Local.HitTest)
                            _renderer_changelist.Add(hit);
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
                            if (Settings.Local.HitTest)
                                _renderer_changelist.Add(id);
                            unique.Add(id);
                            _line_framehit.Add(id, start + i);
                        }
                    }
                    frames[start + i] = unique;
                }
            }
        }
        /// <summary>
        /// Sets the hit test position to the new frame and returns the
        /// line ids that need updating to the renderer.
        /// </summary>
        public HashSet<int> GetChangesForFrame(int newframe)
        {
            using (var sync = _sync.AcquireUpgradableRead())
            {
                var ret = new HashSet<int>();
                var current = _currentframe;
                foreach (var v in _renderer_changelist)
                {
                    ret.Add(v);
                }
                if (!Settings.Local.HitTest)
                {
                    newframe = Disabled;
                    if (current != Disabled)
                    {
                        foreach (var v in _allcollisions)
                        {
                            ret.Add(v);
                        }
                        current = Disabled;
                    }
                }
                else if (current != newframe)
                {
                    if (current == Disabled)
                        current = 0;
                    // i'm leaving this in seperate loops for now
                    // it's a lot more readable this way for changes
                    if (newframe < current)
                    {
                        // we're moving backwards.
                        // we compare to currentframe because we may have to
                        // remove its hit lines
                        for (int i = newframe; i <= current; i++)
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
                        for (int i = current + 1; i <= newframe; i++)
                        {
                            foreach (var id in _unique_frame_collisions[i])
                            {
                                ret.Add(id);
                            }
                        }
                    }
                }
                if (_currentframe != newframe || _renderer_changelist.Count != 0)
                {
                    sync.UpgradeToWriter();
                    _renderer_changelist.Clear();
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
                    _renderer_changelist.Add(v);
                }
                _unique_frame_collisions.Clear();
                _unique_frame_collisions.Add(new HashSet<int>());
                _line_framehit.Clear();
                _allcollisions.Clear();
                _currentframe = Disabled;
            }
        }
    }
}