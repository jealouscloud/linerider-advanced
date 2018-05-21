using System;
using System.Collections.Generic;
namespace linerider.IO.json
{
    public class track_json
    {
        public class point_json
        {
            public double x;
            public double y;
        }
        public string label { get; set; }
        public string creator { get; set; }
        public string description { get; set; }
        public int duration { get; set; }
        public string version { get; set; }
        public point_json startPosition { get; set; }
        public List<line_json> lines { get; set; }
        public string linesArrayCompressed { get; set; }
        public object[][] linesArray { get; set; }

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
