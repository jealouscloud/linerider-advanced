//
//  GLWindow.cs
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
using System.IO;
using OpenTK.Input;
using OpenTK.Graphics;
using linerider.Audio;
using linerider.UI;
using linerider.Utils;

namespace linerider
{
    static class Settings
    {
        public static class Local
        {
            public static bool HitTest = false;
            public static float DefaultPlayback = 1f;
            public static bool DisableSnap = false;
            public static bool ForceXySnap = false;
            public static bool MomentumVectors = false;
            public static bool PreviewMode;
            public static int SlowmoSpeed = 2;
            public static bool RecordingMode;
            public static bool RenderGravityWells;
            public static bool ColorPlayback;
            public static bool DrawContactPoints;
            public static bool OnionSkinning;
            //recording:
            public static bool RecordingShowTools = false;
            public static bool ShowFps = true;
            public static bool ShowPpf = true;
            public static bool ShowTimer = true;
            public static bool SmoothRecording = true;
            public static Song CurrentSong = new Song("", 0);
            public static bool EnableSong = false;
        }
        public static Dictionary<Hotkey, List<Keybinding>> Keybinds = new Dictionary<Hotkey, List<Keybinding>>();
        public static int PlaybackZoomType = 0;
        public static float PlaybackZoomValue = 4;
        public static float Volume = 100;
        public static bool LiveAdjustment = true;
        public static bool SuperZoom = false;
        public static bool WhiteBG = false;
        public static bool PinkLifelock = false;
        public static bool NightMode = false;
        public static bool SmoothCamera = true;
        public static bool RoundLegacyCamera = true;
        public static bool SmoothPlayback = true;
        public static bool CheckForUpdates = true;
        public static bool Render1080p = false;
        public static string LastSelectedTrack = "";
        static Settings()
        {

            CreateKeybind(Hotkey.EditorPencilTool, new Keybinding(Key.Q));
            CreateKeybind(Hotkey.EditorLineTool, new Keybinding(Key.W));
            CreateKeybind(Hotkey.EditorEraserTool, new Keybinding(Key.E));
            CreateKeybind(Hotkey.EditorSelectTool, new Keybinding(Key.R));
            CreateKeybind(Hotkey.EditorPanTool, new Keybinding(Key.T));
            CreateKeybind(Hotkey.EditorToolColor1, new Keybinding(Key.Number1));
            CreateKeybind(Hotkey.EditorToolColor2, new Keybinding(Key.Number2));
            CreateKeybind(Hotkey.EditorToolColor3, new Keybinding(Key.Number3));

            CreateKeybind(Hotkey.EditorUseTool, new Keybinding(MouseButton.Left));
            CreateKeybind(Hotkey.EditorCycleToolSetting, new Keybinding(Key.Tab));

            CreateKeybind(Hotkey.EditorRemoveLatestLine, new Keybinding(Key.BackSpace));
            CreateKeybind(Hotkey.EditorFocusStart, new Keybinding(Key.Home));
            CreateKeybind(Hotkey.EditorFocusLastLine, new Keybinding(Key.End));
            CreateKeybind(Hotkey.EditorFocusRider, new Keybinding(Key.F1));
            CreateKeybind(Hotkey.EditorFocusFlag, new Keybinding(Key.F2));
            CreateKeybind(Hotkey.ToolLifeLock, new Keybinding(Key.AltLeft));
            CreateKeybind(Hotkey.ToolLifeLock, new Keybinding(Key.AltRight));
            CreateKeybind(Hotkey.ToolAngleLock, new Keybinding(Key.ShiftLeft));
            CreateKeybind(Hotkey.ToolAngleLock, new Keybinding(Key.ShiftRight));
            CreateKeybind(Hotkey.ToolLengthLock, new Keybinding(Key.L));
            CreateKeybind(Hotkey.ToolXYSnap, new Keybinding(Key.X));
            CreateKeybind(Hotkey.ToolDisableSnap, new Keybinding(Key.S));
            CreateKeybind(Hotkey.ToolSelectBothJoints, new Keybinding(Key.ControlLeft));
            CreateKeybind(Hotkey.ToolSelectBothJoints, new Keybinding(Key.ControlRight));
            CreateKeybind(Hotkey.LineToolFlipLine, new Keybinding(Key.ShiftLeft));
            CreateKeybind(Hotkey.LineToolFlipLine, new Keybinding(Key.ShiftRight));
            CreateKeybind(Hotkey.EditorUndo, new Keybinding(Key.Z, KeyModifiers.Control));

            CreateKeybind(Hotkey.EditorRedo, new Keybinding(Key.Y, KeyModifiers.Control));
            CreateKeybind(Hotkey.EditorRedo, new Keybinding(Key.Z, KeyModifiers.Control | KeyModifiers.Shift));

            CreateKeybind(Hotkey.PlaybackStartIgnoreFlag, new Keybinding(Key.Y, KeyModifiers.Alt));
            CreateKeybind(Hotkey.PlaybackStartGhostFlag, new Keybinding(Key.I, KeyModifiers.Shift));
            CreateKeybind(Hotkey.PlaybackStartSlowmo, new Keybinding(Key.Y, KeyModifiers.Shift));
            CreateKeybind(Hotkey.PlaybackFlag, new Keybinding(Key.I));
            CreateKeybind(Hotkey.PlaybackStart, new Keybinding(Key.Y));
            CreateKeybind(Hotkey.PlaybackStop, new Keybinding(Key.U));
            CreateKeybind(Hotkey.PlaybackSlowmo, new Keybinding(Key.M));
            CreateKeybind(Hotkey.PlaybackZoom, new Keybinding(Key.Z));
            CreateKeybind(Hotkey.PlaybackUnzoom, new Keybinding(Key.X));

            CreateKeybind(Hotkey.PlaybackSpeedUp, new Keybinding(Key.Plus));
            CreateKeybind(Hotkey.PlaybackSpeedUp, new Keybinding(Key.KeypadPlus));

            CreateKeybind(Hotkey.PlaybackSpeedDown, new Keybinding(Key.Minus));
            CreateKeybind(Hotkey.PlaybackSpeedDown, new Keybinding(Key.KeypadMinus));

            CreateKeybind(Hotkey.PlaybackFrameNext, new Keybinding(Key.Right));
            CreateKeybind(Hotkey.PlaybackFramePrev, new Keybinding(Key.Left));
            CreateKeybind(Hotkey.PlaybackForward, new Keybinding(Key.Right, KeyModifiers.Shift));
            CreateKeybind(Hotkey.PlaybackBackward, new Keybinding(Key.Left, KeyModifiers.Shift));
            CreateKeybind(Hotkey.PlaybackIterationNext, new Keybinding(Key.Right, KeyModifiers.Alt));
            CreateKeybind(Hotkey.PlaybackIterationPrev, new Keybinding(Key.Left, KeyModifiers.Alt));
            CreateKeybind(Hotkey.PlaybackTogglePause, new Keybinding(Key.Space));

            CreateKeybind(Hotkey.PreferencesWindow, new Keybinding(Key.Escape));
            CreateKeybind(Hotkey.PreferencesWindow, new Keybinding(Key.P, KeyModifiers.Control));
            CreateKeybind(Hotkey.PreferenceOnionSkinning, new Keybinding(Key.O, KeyModifiers.Control));
            CreateKeybind(Hotkey.LoadWindow, new Keybinding(Key.O));
            CreateKeybind(Hotkey.Quicksave, new Keybinding(Key.S, KeyModifiers.Control));

            CreateKeybind(Hotkey.PlayButtonIgnoreFlag, new Keybinding(Key.AltLeft));
            CreateKeybind(Hotkey.PlayButtonIgnoreFlag, new Keybinding(Key.AltRight));

            CreateKeybind(Hotkey.EditorQuickPan, new Keybinding(Key.Space));
        }
        private static void CreateKeybind(Hotkey hotkey, Keybinding keybinding)
        {
            if (keybinding.IsEmpty)
                return;
            if (!Keybinds.ContainsKey(hotkey))
            {
                Keybinds[hotkey] = new List<Keybinding>();
            }
            Keybinds[hotkey].Add(keybinding);
        }
        public static void Load()
        {
            string[] lines = null;
            try
            {
                if (!File.Exists(Program.UserDirectory + "linerider.conf"))
                {
                    Save();
                }
                lines = File.ReadAllLines(Program.UserDirectory + "linerider.conf");
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
            LoadBool(GetSetting(lines, nameof(CheckForUpdates)), ref CheckForUpdates);
            LoadBool(GetSetting(lines, nameof(SmoothPlayback)), ref SmoothPlayback);
            LoadBool(GetSetting(lines, nameof(RoundLegacyCamera)), ref RoundLegacyCamera);
            LoadBool(GetSetting(lines, nameof(Render1080p)), ref Render1080p);
            var lasttrack = GetSetting(lines, nameof(LastSelectedTrack));
            if (File.Exists(lasttrack) && lasttrack.StartsWith(Constants.TracksDirectory))
            {
                LastSelectedTrack = lasttrack;
            }
            foreach (string name in Enum.GetNames(typeof(Hotkey)))
            {
                LoadKeybinding(lines, name);
            }
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
            config += "\r\n" + MakeSetting(nameof(CheckForUpdates), CheckForUpdates.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SmoothPlayback), SmoothPlayback.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(LastSelectedTrack), LastSelectedTrack);
            config += "\r\n" + MakeSetting(nameof(RoundLegacyCamera), RoundLegacyCamera.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Render1080p), Render1080p.ToString(Program.Culture));
            foreach (var binds in Keybinds)
            {
                foreach (var bind in binds.Value)
                {
                    config += "\r\n" + MakeSetting(binds.Key.ToString(),
                    "Mod=" + bind.Modifiers.ToString() +
                    ";Key=" + bind.Key.ToString() +
                    ";Mouse=" + bind.MouseButton.ToString() + ";");
                }
            }
            try
            {
                File.WriteAllText(Program.UserDirectory + "linerider.conf", config);
            }
            catch { }
        }
        private static void LoadKeybinding(string[] config, string hotkeyname)
        {
            var hotkey = (Hotkey)Enum.Parse(typeof(Hotkey), hotkeyname);
            int line = 0;
            var setting = GetSetting(config, hotkeyname, ref line);
            if (setting != null)
                Keybinds[hotkey] = new List<Keybinding>();
            while (setting != null)
            {
                line++;
                int modstart = setting.IndexOf("Mod=");
                int keystart = setting.IndexOf("Key=");
                int mousestart = setting.IndexOf("Mouse=");
                if (modstart == -1 || keystart == -1 || mousestart == -1)
                    return;
                modstart += 4;
                keystart += 4;
                mousestart += 6;
                int modend = setting.IndexOf(";", modstart);
                int keyend = setting.IndexOf(";", keystart);
                int mouseend = setting.IndexOf(";", mousestart);
                if (modend == -1 || keyend == -1 || mouseend == -1)
                    return;
                try
                {

                    Keybinding ret = new Keybinding();
                    ret.Modifiers = (KeyModifiers)Enum.Parse(typeof(KeyModifiers), setting.Substring(modstart, modend - modstart));
                    ret.MouseButton = (MouseButton)Enum.Parse(typeof(MouseButton), setting.Substring(mousestart, mouseend - mousestart));
                    ret.Key = (Key)Enum.Parse(typeof(Key), setting.Substring(keystart, keyend - keystart));
                    CreateKeybind(hotkey, ret);
                }
                catch
                {
                }
                setting = GetSetting(config, hotkeyname, ref line);
            }

        }
        private static string GetSetting(string[] config, string name)
        {
            int start = 0;
            return GetSetting(config, name, ref start);
        }
        private static string GetSetting(string[] config, string name, ref int start)
        {
            for (int i = start; i < config.Length; i++)
            {
                var idx = config[i].IndexOf("=");
                if (idx != -1 && idx + 1 < config[i].Length && config[i].Substring(0, idx) == name)//split[0] == name && split.Length > 1)
                {

                    var split = config[i].Substring(idx + 1);
                    start = i;
                    return split;
                }
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
