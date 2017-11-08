//
//  Camera.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using linerider.Game;
using linerider.Drawing;
using OpenTK;
namespace linerider.Game
{
	public class Camera : GameService
	{
		public Vector2d ViewPosition { get; set; }

		public Vector2d AimPosition;
		public void UpdateCamera()
		{
			if (Math.Abs((ViewPosition - AimPosition).Length) < 1 || !Settings.Default.SmoothCamera)
				return;
			ViewPosition = Vector2d.Lerp(ViewPosition, AimPosition, 0.33333333f / 2);
			if (Math.Abs((AimPosition - ViewPosition).Length) < 0.1)
			{
				ViewPosition = AimPosition;
			}
		}
		public static Vector2 EllipseClamp(FloatRect rect, Vector2 position)
		{
				var center = rect.Vector + (rect.Size / 2);
				var a = rect.Width / 2;
				var b = rect.Height / 2;
			var p = position - center;
				var d = p.X * p.X / (a * a) + p.Y * p.Y / (b * b);

			if (d > 1)
			{
				Tools.Angle angle = Tools.Angle.FromLine(center, position);
				double t = Math.Atan((rect.Width / 2) * Math.Tan(angle.Radians)
									 / (rect.Height / 2));
				if (angle.Degrees <= 270 && angle.Degrees >= 90)
				{
					t += Math.PI;
				}
				Vector2 ptfPoint = (Vector2)
				   new Vector2d(center.X + (rect.Width / 2) * Math.Cos(t),
							   center.Y + (rect.Height / 2) * Math.Sin(t));

				position = (Vector2)ptfPoint;
			}
			return position;
		}
		public FloatRect GetRenderRect(float zoom, float width, float height)
		{
			if (!Settings.Default.SmoothCamera)
			{
				return GetRenderRectOriginal(zoom, width, height);
			}
			Vector2 viewpos = (Vector2)ViewPosition;
			var sz = new Vector2(width / zoom, height / zoom);
			float Box = 0.2f;
			var rect = new FloatRect(((Vector2)AimPosition - (sz * Box)), sz * Box * 2);


			if (game.Track.Animating)
			{
				viewpos = EllipseClamp(rect, viewpos);
			}
			var pos = viewpos;
			pos -= sz / 2;
			return new FloatRect(pos, sz);
		}
		private FloatRect GetRenderRectOriginal(float zoom, float width, float height)
		{
			Vector2 viewpos = (Vector2)ViewPosition;

			var sz = new Vector2(width / zoom, height / zoom);
			const float Box = 0.125f;
			var rect = new FloatRect(((Vector2)AimPosition - (sz * Box)), sz * Box * 2);
			if (game.Track.Animating)
			{
				viewpos = rect.Clamp(viewpos);
			}
			ViewPosition = (Vector2d)viewpos;
			var pos = viewpos;
			pos -= sz / 2;
			return new FloatRect(pos, sz);
		}

		public void SetPosition(Vector2d v)
		{
			ViewPosition = v;
			AimPosition = v;
		}
	}
}
