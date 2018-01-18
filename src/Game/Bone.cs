using System;

namespace linerider.Game
{
    public struct Bone
	{
		public int joint1;
		public int joint2;
        public double RestLength;

		public bool Breakable;
		public bool OnlyRepel;
	}
}