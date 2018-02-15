using System;

namespace linerider.Game
{
    public sealed class Bone
	{
		public Bone(int joint1, int joint2, double rest, bool breakable, bool onlyrepel)
		{
			this.joint1 = joint1;
            this.joint2 = joint2;
            this.RestLength = rest;
            this.Breakable = breakable;
            this.OnlyRepel = onlyrepel;
		}
		public readonly int joint1;
		public readonly int joint2;
        public readonly double RestLength;

		public readonly bool Breakable;
		public readonly bool OnlyRepel;
	}
}