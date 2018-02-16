using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK;
namespace linerider.Game
{
    [StructLayout(LayoutKind.Sequential)]

    public struct SimulationPoint
    {
        public readonly Vector2d Location;
        public readonly Vector2d Previous;
        public readonly Vector2d Momentum;
        public readonly double Friction;
        public SimulationPoint(Vector2d location, Vector2d prev, Vector2d momentum, double friction)
        {
            Location = location;
            Previous = prev;
            Friction = friction;
            Momentum = momentum;
        }
        public SimulationPoint Step()
        {
            var momentum = Location - Previous + RiderConstants.Gravity;
            return new SimulationPoint(Location + momentum, Location, momentum, Friction);
        }
        public SimulationPoint StepFriction()
        {
            var momentum = (Location - Previous) * Friction + RiderConstants.Gravity;
            return new SimulationPoint(Location + momentum, Location, momentum, Friction);
        }
        public SimulationPoint AddPosition(Vector2d add)
        {
            return new SimulationPoint(Location + add, Previous, Momentum, Friction);
        }
        public SimulationPoint Replace(Vector2d location)
        {
            return new SimulationPoint(location, Previous, Momentum, Friction);
        }
        public SimulationPoint Replace(Vector2d location, Vector2d prev)
        {
            return new SimulationPoint(location, prev, Momentum, Friction);
        }
        public override bool Equals(Object obj)
        {
            return obj is SimulationPoint && this == (SimulationPoint)obj;
        }
        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ Previous.GetHashCode();
        }
        public static bool FastEquals(ref SimulationPoint a, ref SimulationPoint b)
        {
            return a.Location == b.Location && a.Previous == b.Previous;
        }
        public static bool operator ==(SimulationPoint x, SimulationPoint y)
        {
            return x.Location == y.Location && x.Previous == y.Previous;
        }
        public static bool operator !=(SimulationPoint x, SimulationPoint y)
        {
            return !(x == y);
        }
    }
}
