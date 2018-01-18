using System;
using System.Collections.Generic;
using OpenTK;
namespace linerider.Game
{
	public class RiderConstants
	{
        public static readonly Vector2d[] DefaultRider = new[] {
				new Vector2d(0, 0),
				new Vector2d(0, 5),
				new Vector2d(15, 5),
				new Vector2d(17.5, 0),
				new Vector2d(5, 0),
				new Vector2d(5, -5.5),
				new Vector2d(11.5, -5),
				new Vector2d(11.5, -5),
				new Vector2d(10, 5),
				new Vector2d(10, 5)
			};
		public static readonly Vector2d[] DefaultScarf = new[] {
				new Vector2d(3.5,-5),
				new Vector2d(1.5,-5),
				new Vector2d(0,-5),
				new Vector2d(-2.0,-5),
				new Vector2d(-3.5,-5),
				new Vector2d(-5.5,-5),
		};
		public const double EnduranceFactor = 0.0285;//0.057*0.05
		public const double StartingMomentum = 0.8 * 0.5;
		public static Vector2d Gravity = new Vector2d(0, 0.35 * 0.5);
		public const int SledTL = 0;
		public const int SledBL = 1;
		public const int SledBR = 2;
		public const int SledTR = 3;
		public const int BodyButt = 4;
		public const int BodyShoulder = 5;
		public const int BodyHandLeft = 6;
		public const int BodyHandRight = 7;
		public const int BodyFootLeft = 8;
		public const int BodyFootRight = 9;
		internal static readonly Bone[] Bones;
		static RiderConstants()
		{
			var bonelist = new List<Bone>();
			bonelist.Add(CreateBone(SledTL, SledBL));
			bonelist.Add(CreateBone(SledBL, SledBR));
			bonelist.Add(CreateBone(SledBR, SledTR));
			bonelist.Add(CreateBone(SledTR, SledTL));
			bonelist.Add(CreateBone(SledTL, SledBR));
			bonelist.Add(CreateBone(SledTR, SledBL));

			bonelist.Add(CreateBone(SledTL, BodyButt, breakable: true));
			bonelist.Add(CreateBone(SledBL, BodyButt, breakable: true));
			bonelist.Add(CreateBone(SledBR, BodyButt, breakable: true));

			bonelist.Add(CreateBone(BodyShoulder, BodyButt));
			bonelist.Add(CreateBone(BodyShoulder, BodyHandLeft));
			bonelist.Add(CreateBone(BodyShoulder, BodyHandRight));
			bonelist.Add(CreateBone(BodyButt, BodyFootLeft));
			bonelist.Add(CreateBone(BodyButt, BodyFootRight));
			bonelist.Add(CreateBone(BodyShoulder, BodyHandRight));

			bonelist.Add(CreateBone(BodyShoulder, SledTL, breakable: true));
			bonelist.Add(CreateBone(SledTR, BodyHandLeft, breakable: true));
			bonelist.Add(CreateBone(SledTR, BodyHandRight, breakable: true));
			bonelist.Add(CreateBone(BodyFootLeft, SledBR, breakable: true));
			bonelist.Add(CreateBone(BodyFootRight, SledBR, breakable: true));

			bonelist.Add(CreateBone(BodyShoulder, BodyFootLeft, repel: true));
			bonelist.Add(CreateBone(BodyShoulder, BodyFootRight, repel: true));
			Bones = bonelist.ToArray();
		}
		private static Bone CreateBone(int a, int b, bool breakable = false, bool repel = false)
		{
            var ret = new Bone() { joint1 = a, joint2 = b, RestLength = (DefaultRider[a] - DefaultRider[b]).Length, Breakable = breakable, OnlyRepel = repel };
            if (repel)
            {
                ret.RestLength *= 0.5;
            }
            return ret;
		}
	}
}
