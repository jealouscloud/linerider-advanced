using System;
using System.Collections.Generic;
namespace linerider.IO.json
{
    public class track_json
    {
        public class zoomtrigger_json
        {
            public int ID;
            public bool zoom;
            public float target;
            public int frames;
        }
        public class gametrigger_json
        {
            public int start;
            public int end;
            public int triggerType;
            public float zoomTarget = 4;
        }
        public class point_json
        {
            public double x;
            public double y;
        }
        public string label { get; set; }
        public string creator { get; set; }
        public string description { get; set; }
        public float startZoom { get; set; }
        public bool zeroStart { get; set; }
        public int duration { get; set; }
        public string version { get; set; }
        public point_json startPosition { get; set; }
        public List<line_json> lines { get; set; }
        public string linesArrayCompressed { get; set; }
        public object[][] linesArray { get; set; }
        public List<zoomtrigger_json> triggers { get; set; }
        public List<gametrigger_json> gameTriggers { get; set; }

        public bool ShouldSerializezeroStart()
        {
            return zeroStart;
        }
        public bool ShouldSerializecreator()
        {
            if (creator != null && creator.Length != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ShouldSerializetriggers()
        {
            if (triggers != null && triggers.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ShouldSerializeduration()
        {
            if (duration > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ShouldSerializedescription()
        {
            if (!string.IsNullOrEmpty(description))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ShouldSerializelinesArrayCompressed()
        {
            if (!string.IsNullOrEmpty(linesArrayCompressed))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ShouldSerializelinesArray()
        {
            if (linesArray != null && linesArray.Length != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
