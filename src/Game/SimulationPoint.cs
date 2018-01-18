using System;
using OpenTK;
namespace linerider.Game
{
    public struct SimulationPoint
    {
        public readonly Vector2d Location;
		public readonly Vector2d Previous;
		public readonly Vector2d Momentum;
        public readonly double Friction;
		public SimulationPoint(Vector2d loc, Vector2d prev, Vector2d momentum, double friction)
		{
			Location = loc;
			Previous = prev;
			Friction = friction;
            Momentum = momentum;
		}
        public SimulationPoint StepMomentum()
		{
            var momentum = Location - Previous + RiderConstants.Gravity;
            return new SimulationPoint(Location + momentum, Location, momentum, Friction);
        }
        public SimulationPoint StepMomentumFriction()
        {
            var momentum = (Location - Previous) * Friction + RiderConstants.Gravity;
            return new SimulationPoint(Location + momentum, Location, momentum, Friction);
        }
        public SimulationPoint CreateNewLocation(Vector2d newloc)
        {
            return new SimulationPoint(newloc, Previous, Momentum, Friction);
        }
    }
}
