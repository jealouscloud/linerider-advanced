using System;
using System.Collections.Generic;
namespace linerider.IO.json
{
    public struct RiderData
    {
        public int Frame;
        public List<track_json.point_json> Points;
        public track_json.point_json CameraCenter;
    }
}
