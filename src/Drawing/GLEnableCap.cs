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
namespace linerider.Drawing
{
	public class GLEnableCap : IDisposable
	{
		private bool _was_enabled = false;
		private EnableCap _cap;
		public GLEnableCap(EnableCap cap)
		{
			_cap = cap;
			_was_enabled = GL.IsEnabled(cap);
			if (!_was_enabled)
				GL.Enable(cap);
		}
		public void Close()
		{
			Dispose();
		}
		public void Dispose()
		{
			if (_was_enabled)
			{
				GL.Disable(_cap);
				_was_enabled = false;
			}
		}
	}
}
