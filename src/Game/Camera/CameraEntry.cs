using System;
using OpenTK;
using linerider.Utils;

namespace linerider.Game
{
    public struct CameraEntry
    {
        public Vector2d RiderCenter { get; }
        public Vector2d CameraOffset { get; }
        public double ppf { get; }
        public CameraEntry(Vector2d origin, Vector2d offset, Vector2d momentum)
        {
            RiderCenter = origin;
            CameraOffset = offset;
            ppf = momentum.Length;
        }
        public CameraEntry(Vector2d origin)
        {
            RiderCenter = origin;
            CameraOffset = Vector2d.Zero;
            ppf =0;
        }
    }
}
