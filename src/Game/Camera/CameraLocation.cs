using System;
using OpenTK;
using linerider.Utils;

namespace linerider.Game
{

    public struct CameraEntry
    {
        public Vector2d RiderCenter { get; }
        public Vector2d Position { get; }
        public Angle PositionAngle { get; }
        public double ppf { get; }
        public CameraEntry(Vector2d origin, Vector2d offset, Vector2d momentum)
        {
            RiderCenter = origin;
            Position = offset;
            ppf = momentum.Length;
            PositionAngle = Angle.FromVector(RiderCenter - Position);
        }
        public CameraEntry(Vector2d origin)
        {
            RiderCenter = origin;
            Position = origin;
            ppf =0;
            PositionAngle = Angle.FromVector(Position - RiderCenter);
        }
    }
}
