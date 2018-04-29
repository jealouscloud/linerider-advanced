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
using OpenTK.Graphics.OpenGL;
using OpenTK;
namespace linerider.Rendering
{
	public class GameDrawingMatrix : GameService
	{
		public static float Scale
		{
			get
			{
				return game.Track.Zoom;
			}
        }
		public static void Enter()
		{
			GL.PushMatrix();
			GL.Scale(game.Track.Zoom, game.Track.Zoom, 0);
			GL.Translate(new Vector3d(game.ScreenTranslation));
		}
		public static void Exit()
		{
			GL.PopMatrix();
        }
		/// <summary>
		/// Converts the input Vector2d in game coordinates to a screen coord
		/// </summary>
		/// <returns>A vector2 ready for drawing in screen space</returns>
        public static Vector2 ScreenCoord(Vector2d coords)
        {
            return (Vector2)ScreenCoordD(coords);
        }
        /// <summary>
        /// Converts the input Vector2d in game coordinates to a screen coord
        /// </summary>
        public static Vector2d ScreenCoordD(Vector2d coords)
        {
            return (coords + game.ScreenTranslation) * Scale;
        }
        /// <summary>
        /// Converts the input Vector2d in game coordinates to a screen coord
        /// </summary>
        /// <returns>A vector2 ready for drawing in screen space</returns>
        public static Vector2[] ScreenCoords(Vector2d[] coords)
        {
			Vector2[] screen = new Vector2[coords.Length];
            for (int i = 0; i < coords.Length; i++)
            {
                screen[i] = ScreenCoord(coords[i]);
            }
			return screen;
		}
	}
}
