using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using linerider.Audio;
using linerider.Game;
using System.Diagnostics;

namespace linerider.IO
{
    /// <summary>
    /// Static class that converts line based triggers to a timeline--based
    /// trigger system.
    /// </summary>
    public static class TriggerConverter
    {
        public static List<GameTrigger> ConvertTriggers(List<LineTrigger> triggers, Track track)
        {
            List<GameTrigger> gametriggers = new List<GameTrigger>();
            const int minute = 40 * 60;
            int lasthit = 0;
            var rider = track.GetStart();
            var hittest = new HitTestManager();
            int i = 1;
            int hitframe = -1;
            LineTrigger activetrigger = null;
            float zoom = track.StartZoom;
            GameTrigger newtrigger = null;
            do
            {
                var collisions = new LinkedList<int>();
                rider = rider.Simulate(
                    track.Grid,
                    track.Bones,
                    collisions);
                hittest.AddFrame(collisions);
                LineTrigger hittrigger = null;
                foreach (var lineid in collisions)
                {
                    foreach (var trigger in triggers)
                    {
                        if (trigger.LineID == lineid)
                        {
                            hittrigger = trigger;
                        }
                    }
                }
                if (hittrigger != null &&
                    hittrigger != activetrigger)
                {
                    if (activetrigger != null)
                    {
                        newtrigger.ZoomTarget = zoom;
                        newtrigger.End = i;
                        gametriggers.Add(newtrigger);
                    }
                    hitframe = i;
                    activetrigger = hittrigger;
                    newtrigger = new GameTrigger() { TriggerType = TriggerType.Zoom, Start = i };
                }
                if (activetrigger != null)
                {
                    var delta = i - hitframe;
                    if (!activetrigger.Activate(delta, ref zoom))
                    {
                        newtrigger.ZoomTarget = zoom;
                        newtrigger.End = i;
                        gametriggers.Add(newtrigger);
                        activetrigger = null;
                    }
                }
                if (hittest.HasUniqueCollisions(i))
                {
                    lasthit = i;
                }
                i++;
            }
            while (i - lasthit < (minute * 2)); // be REALLY sure, 2 minutes.
            return gametriggers;
        }
    }
}
