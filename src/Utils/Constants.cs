using System;
using System.Drawing;
using OpenTK.Graphics;
namespace linerider.Utils
{
    static class Constants
    {
        public static readonly Color4 ColorOffwhite = new Color4(244, 245, 249, 255);
        public static readonly Color4 ColorWhite = new Color4(255, 255, 255, 255);
        public static readonly Color4 ColorNightMode = new Color4(20, 20, 25, 255);
        public static readonly int[] MotionArray =
        {
            1, 2, 5, 10, 20, 30, 40, 80, 160, 320, 640
        };
        public static readonly Color RedLineColor = Color.FromArgb(0xCC, 0, 0);
        public static readonly Color BlueLineColor = Color.FromArgb(0, 0x66, 0xFF);
        public static readonly Color SceneryLineColor = Color.FromArgb(0, 0xCC, 0);

        // the following have an alpha of 254 
        // so theyre overridden by the shader for hit test
        public static readonly Color DefaultLineColor = Color.FromArgb(254, 0, 0, 0);
        public static readonly Color DefaultNightLineColor = Color.FromArgb(254, 255, 255,255);
        public static readonly string TracksDirectory = Program.UserDirectory + TracksFolderName + System.IO.Path.DirectorySeparatorChar;
        public const string TracksFolderName = "Tracks";
        public const string DefaultTrackName = "<untitled>";
        public const float DefaultZoom = 4;
        public const int PhysicsRate = 40;
    }
}