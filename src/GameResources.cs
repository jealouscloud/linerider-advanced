//
//  GameResources.cs
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
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace linerider
{
    internal class GameResources
    {
        private static Assembly Assembly = null;
        private static Dictionary<string, object> _lookuptable = null;
        public static void Init()
        {
            if (Assembly == null)
            {
                Assembly = typeof(GameResources).Assembly;
            }
            if (_lookuptable == null)
            {
                _lookuptable = new Dictionary<string, object>();
            }
        }
        private static Bitmap GetBitmap(string name)
        {
            object lookup;
            if (_lookuptable.TryGetValue(name, out lookup))
            {
                return (Bitmap)lookup;
            }
            using (var stream = Assembly.GetManifestResourceStream("linerider.Resources." + name))
            {
                var ret = new Bitmap(stream);
                _lookuptable[name] = ret;
                return ret;
            }
        }
        private static byte[] GetBytes(string name)
        {
            object lookup;
            if (_lookuptable.TryGetValue(name, out lookup))
            {
                return ((byte[])lookup).ToArray();//prevent writing to resource
            }
            using (var stream = Assembly.GetManifestResourceStream("linerider.Resources." + name))
            {
                byte[] ret = new byte[stream.Length];
                stream.Read(ret, 0, ret.Length);
                _lookuptable[name] = ret;
                return ret;
            }
        }
        private static string GetString(string name)
        {
            object lookup;
            if (_lookuptable.TryGetValue(name, out lookup))
            {
                return (string)lookup;//strings are immutable so there's no chance of writing to resource
            }
            using (var stream = Assembly.GetManifestResourceStream("linerider.Resources." + name))
            {
                using (var reader = new StreamReader(stream))
                {
                    var ret = reader.ReadToEnd();
                    _lookuptable[name] = ret;
                    return ret;
                }
            }
        }

        internal static byte[] beep
        {
            get
            {
                return GetBytes("beep.wav");
            }
        }

        internal static System.Drawing.Bitmap circletex
        {
            get
            {
                return GetBitmap("circletex.png");
            }
        }

        internal static System.Drawing.Bitmap cursor_closed_move
        {
            get
            {
                return GetBitmap("cursors.closed_move.png");
            }
        }

        internal static System.Drawing.Bitmap cursor_select
        {
            get
            {
                return GetBitmap("cursors.select.png");
            }
        }

        internal static System.Drawing.Bitmap DefaultSkin
        {
            get
            {
                return GetBitmap("DefaultSkin.png");
            }
        }

        internal static System.Drawing.Bitmap eraser_cursor
        {
            get
            {
                return GetBitmap("cursors.eraser.png");
            }
        }

        internal static System.Drawing.Bitmap eraser_icon
        {
            get
            {
                return GetBitmap("eraser_icon.png");
            }
        }

        internal static System.Drawing.Bitmap fast_forward
        {
            get
            {
                return GetBitmap("fast-forward.png");
            }
        }

        internal static System.Drawing.Bitmap eraser_icon_white
        {
            get
            {
                return GetBitmap("White.eraser_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap fast_forward_white
        {
            get
            {
                return GetBitmap("White.fast-forward_white.png");
            }
        }


        internal static System.Drawing.Bitmap flag_icon_white
        {
            get
            {
                return GetBitmap("White.flag_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap line_adjust_icon_white
        {
            get
            {
                return GetBitmap("White.line_adjust_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap line_icon_white
        {
            get
            {
                return GetBitmap("White.line_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap menu_icon_white
        {
            get
            {
                return GetBitmap("White.menu_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap move_icon_white
        {
            get
            {
                return GetBitmap("White.move_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap pause_white
        {
            get
            {
                return GetBitmap("White.pause_white.png");
            }
        }
        internal static System.Drawing.Bitmap pencil_icon_white
        {
            get
            {
                return GetBitmap("White.pencil_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap play_icon_white
        {
            get
            {
                return GetBitmap("White.play_icon_white.png");
            }
        }
        internal static System.Drawing.Bitmap rewind_white
        {
            get
            {
                return GetBitmap("White.rewind_white.png");
            }
        }
        internal static System.Drawing.Bitmap stop_icon_white
        {
            get
            {
                return GetBitmap("White.stop_icon_white.png");
            }
        }

        internal static System.Drawing.Bitmap flag_icon
        {
            get
            {
                return GetBitmap("flag_icon.png");
            }
        }

        internal static System.Drawing.Bitmap line_adjust_icon
        {
            get
            {
                return GetBitmap("line_adjust_icon.png");
            }
        }

        internal static System.Drawing.Bitmap line_cursor
        {
            get
            {
                return GetBitmap("cursors.line.png");
            }
        }

        internal static System.Drawing.Bitmap pencil_icon
        {
            get
            {
                return GetBitmap("pencil_icon.png");
            }
        }

        internal static System.Drawing.Bitmap line_icon
        {
            get
            {
                return GetBitmap("line_icon.png");
            }
        }

        internal static System.Drawing.Bitmap loading
        {
            get
            {
                return GetBitmap("loading.png");
            }
        }

        internal static System.Drawing.Bitmap menu_icon
        {
            get
            {
                return GetBitmap("menu_icon.png");
            }
        }

        internal static System.Drawing.Bitmap move_cursor
        {
            get
            {
                return GetBitmap("cursors.move.png");
            }
        }

        internal static System.Drawing.Bitmap move_icon
        {
            get
            {
                return GetBitmap("move_icon.png");
            }
        }


        internal static System.Drawing.Bitmap pause
        {
            get
            {
                return GetBitmap("pause.png");
            }
        }

        internal static System.Drawing.Bitmap pencil_cursor
        {
            get
            {
                return GetBitmap("cursors.pencil.png");
            }
        }
        internal static System.Drawing.Bitmap size_nesw_cursor
        {
            get
            {
                return GetBitmap("cursors.size_bdiag.png");
            }
        }
        internal static System.Drawing.Bitmap size_nwse_cursor
        {
            get
            {
                return GetBitmap("cursors.size_fdiag.png");
            }
        }
        internal static System.Drawing.Bitmap size_hor_cursor
        {
            get
            {
                return GetBitmap("cursors.size_hor.png");
            }
        }
        internal static System.Drawing.Bitmap size_ver_cursor
        {
            get
            {
                return GetBitmap("cursors.size_ver.png");
            }
        }
        internal static System.Drawing.Bitmap default_cursor
        {
            get
            {
                return GetBitmap("cursors.default.png");
            }
        }

        internal static System.Drawing.Bitmap play_icon
        {
            get
            {
                return GetBitmap("play_icon.png");
            }
        }

        internal static System.Drawing.Bitmap rewind
        {
            get
            {
                return GetBitmap("rewind.png");
            }
        }


        internal static Bitmap sled_img
        {
            get
            {
                return GetBitmap("sled.png");
            }
        }
        internal static Bitmap bosh_img
        {
            get
            {
                return GetBitmap("bosh.png");
            }
        }
        internal static Bitmap boshdead_img
        {
            get
            {
                return GetBitmap("boshdead.png");
            }
        }
        internal static Bitmap brokensled_img
        {
            get
            {
                return GetBitmap("brokensled.png");
            }
        }
        internal static Bitmap arm_img
        {
            get
            {
                return GetBitmap("arm.png");
            }
        }
        internal static Bitmap leg_img
        {
            get
            {
                return GetBitmap("leg.png");
            }
        }

        internal static System.Drawing.Bitmap stop_icon
        {
            get
            {
                return GetBitmap("stop_icon.png");
            }
        }

        internal static byte[] icon
        {
            get
            {
                return GetBytes("icon.ico");
            }
        }

        internal static string DefaultColors
        {
            get
            {
                return GetString("DefaultColors.xml");
            }
        }
        internal static string gamefont_15_fnt
        {
            get
            {
                return GetString("gamefont_15.fnt");
            }
        }
        internal static System.Drawing.Bitmap gamefont_15_png
        {
            get
            {
                return GetBitmap("gamefont_15_0.png");
            }
        }
    }
}
