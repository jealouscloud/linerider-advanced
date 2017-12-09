using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace linerider
{
    static class Settings
    {
        public static int PlaybackZoomType = 0;
        public static float PlaybackZoomValue = 4;
        public static float Volume = 100;
        public static bool LiveAdjustment = true;
        public static bool SuperZoom = false;
        public static bool WhiteBG = false;
        public static bool PinkLifelock = false;
        public static bool NightMode = false;
        public static bool SmoothCamera = true;
        public static void Load()
        {
            string[] lines = null;
            try
            {
                if (!File.Exists(Program.CurrentDirectory + "linerider.conf"))
                {
                    Save();
                }
                lines = File.ReadAllLines(Program.CurrentDirectory + "linerider.conf");
            }
            catch
            {
            }
            LoadInt(GetSetting(lines, nameof(PlaybackZoomType)), ref PlaybackZoomType);
            LoadFloat(GetSetting(lines, nameof(PlaybackZoomValue)), ref PlaybackZoomValue);
            LoadFloat(GetSetting(lines, nameof(Volume)), ref Volume);
            LoadBool(GetSetting(lines, nameof(LiveAdjustment)), ref LiveAdjustment);
            LoadBool(GetSetting(lines, nameof(SuperZoom)), ref SuperZoom);
            LoadBool(GetSetting(lines, nameof(WhiteBG)), ref WhiteBG);
            LoadBool(GetSetting(lines, nameof(PinkLifelock)), ref PinkLifelock);
            LoadBool(GetSetting(lines, nameof(NightMode)), ref NightMode);
            LoadBool(GetSetting(lines, nameof(SmoothCamera)), ref SmoothCamera);
        }
        public static void Save()
        {
            string config = MakeSetting(nameof(PlaybackZoomType), PlaybackZoomType.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(PlaybackZoomValue), PlaybackZoomValue.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Volume), Volume.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(LiveAdjustment), LiveAdjustment.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SuperZoom), SuperZoom.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(WhiteBG), WhiteBG.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(PinkLifelock), PinkLifelock.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(NightMode), NightMode.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SmoothCamera), SmoothCamera.ToString(Program.Culture));
            try
            {
                File.WriteAllText(Program.CurrentDirectory + "linerider.conf", config);
            }
            catch { }
        }
        private static string GetSetting(string[] config, string name)
        {
            for (int i = 0; i < config.Length; i++)
            {
                var split = config[i].Split('=');
                if (split[0] == name && split.Length > 1)
                    return split[1];
            }
            return null;
        }
        private static string MakeSetting(string name, string value)
        {
            return name + "=" + value;
        }
        private static void LoadInt(string setting, ref int var)
        {
            int val;
            if (int.TryParse(setting, System.Globalization.NumberStyles.Integer, Program.Culture, out val))
                var = val;
        }
        private static void LoadFloat(string setting, ref float var)
        {
            float val;
            if (float.TryParse(setting, System.Globalization.NumberStyles.Float, Program.Culture, out val))
                var = val;
        }
        private static void LoadBool(string setting, ref bool var)
        {
            bool val;
            if (bool.TryParse(setting, out val))
                var = val;
        }
    }
}
