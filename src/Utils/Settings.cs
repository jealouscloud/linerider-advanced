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
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using linerider.Audio;
using linerider.UI;
using linerider.Utils;

namespace linerider
{
    static class Settings
    {
        public static class Recording
        {
            public static bool ShowTools = false;
            public static bool ShowFps = true;
            public static bool ShowPpf = true;
        }
        public static class Local
        {
            public static bool RecordingMode;
            public static float MaxZoom
            {
                get
                {
                    return Settings.SuperZoom ? Constants.MaxSuperZoom : Constants.MaxZoom;
                }
            }
        }
        public static class Editor
        {
            public static bool HitTest;
            public static bool SnapNewLines;
            public static bool SnapMoveLine;
            public static bool ForceXySnap;
            public static bool MomentumVectors;
            public static bool RenderGravityWells;
            public static bool DrawContactPoints;
            public static bool LifeLockNoOrange;
            public static bool LifeLockNoFakie;
        }
        public static int PlaybackZoomType;
        public static float Volume;
        public static bool SuperZoom;
        public static bool WhiteBG;
        public static bool NightMode;
        public static bool SmoothCamera;
        public static bool RoundLegacyCamera;
        public static bool SmoothPlayback;
        public static bool CheckForUpdates;
        public static bool Record1080p;
        public static bool RecordSmooth;
        public static bool RecordMusic;
        public static float ScrollSensitivity;
        public static int SettingsPane;
        public static bool MuteAudio;
        public static bool PreviewMode;
        public static int SlowmoSpeed;
        public static float DefaultPlayback;
        public static bool ColorPlayback;
        public static bool OnionSkinning;
        public static string LastSelectedTrack = "";
        public static Dictionary<Hotkey, KeyConflicts> KeybindConflicts = new Dictionary<Hotkey, KeyConflicts>();
        public static Dictionary<Hotkey, List<Keybinding>> Keybinds = new Dictionary<Hotkey, List<Keybinding>>();
        private static Dictionary<Hotkey, List<Keybinding>> DefaultKeybinds = new Dictionary<Hotkey, List<Keybinding>>();
        static Settings()
        {
            RestoreDefaultSettings();
            foreach (Hotkey hk in Enum.GetValues(typeof(Hotkey)))
            {
                if (hk == Hotkey.None)
                    continue;
                KeybindConflicts.Add(hk, KeyConflicts.General);
                Keybinds.Add(hk, new List<Keybinding>());
            }
            //conflicts, for keybinds that depend on a state, so keybinds 
            //outside of its state can be set as long
            //as its dependant state (general) doesnt have a keybind set
            KeybindConflicts[Hotkey.PlaybackZoom] = KeyConflicts.Playback;
            KeybindConflicts[Hotkey.PlaybackUnzoom] = KeyConflicts.Playback;
            KeybindConflicts[Hotkey.PlaybackSpeedUp] = KeyConflicts.Playback;
            KeybindConflicts[Hotkey.PlaybackSpeedDown] = KeyConflicts.Playback;

            KeybindConflicts[Hotkey.LineToolFlipLine] = KeyConflicts.LineTool;

            KeybindConflicts[Hotkey.ToolXYSnap] = KeyConflicts.Tool;
            KeybindConflicts[Hotkey.ToolToggleSnap] = KeyConflicts.Tool;
            KeybindConflicts[Hotkey.EditorCancelTool] = KeyConflicts.Tool;

            KeybindConflicts[Hotkey.ToolLengthLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolAngleLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolAxisLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolPerpendicularAxisLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolLifeLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolLengthLock] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolCopy] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolCut] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolPaste] = KeyConflicts.SelectTool;
            KeybindConflicts[Hotkey.ToolDelete] = KeyConflicts.SelectTool;

            KeybindConflicts[Hotkey.PlayButtonIgnoreFlag] = KeyConflicts.HardCoded;
            KeybindConflicts[Hotkey.EditorCancelTool] = KeyConflicts.HardCoded;
            KeybindConflicts[Hotkey.ToolAddSelection] = KeyConflicts.HardCoded;
            KeybindConflicts[Hotkey.ToolToggleSelection] = KeyConflicts.HardCoded;
            SetupDefaultKeybinds();
        }
        public static void RestoreDefaultSettings()
        {
            Editor.HitTest = false;
            Editor.SnapNewLines = true;
            Editor.SnapMoveLine = true;
            Editor.ForceXySnap = false;
            Editor.MomentumVectors = false;
            Editor.RenderGravityWells = false;
            Editor.DrawContactPoints = false;
            Editor.LifeLockNoOrange = false;
            Editor.LifeLockNoFakie = false;
            PlaybackZoomType = 0;
            Volume = 100;
            SuperZoom = false;
            WhiteBG = false;
            NightMode = false;
            SmoothCamera = true;
            RoundLegacyCamera = true;
            SmoothPlayback = true;
            CheckForUpdates = true;
            Record1080p = false;
            RecordSmooth = true;
            RecordMusic = true;
            ScrollSensitivity = 1;
            SettingsPane = 0;
            MuteAudio = false;
            PreviewMode = false;
            SlowmoSpeed = 2;
            DefaultPlayback = 1f;
            ColorPlayback = false;
            OnionSkinning = false;
        }
        public static void ResetKeybindings()
        {
            foreach (var kb in Keybinds)
            {
                kb.Value.Clear();
            }
            LoadDefaultKeybindings();
        }
        private static void SetupDefaultKeybinds()
        {
            SetupDefaultKeybind(Hotkey.EditorPencilTool, new Keybinding(Key.Q));
            SetupDefaultKeybind(Hotkey.EditorLineTool, new Keybinding(Key.W));
            SetupDefaultKeybind(Hotkey.EditorEraserTool, new Keybinding(Key.E));
            SetupDefaultKeybind(Hotkey.EditorSelectTool, new Keybinding(Key.R));
            SetupDefaultKeybind(Hotkey.EditorPanTool, new Keybinding(Key.T));
            SetupDefaultKeybind(Hotkey.EditorToolColor1, new Keybinding(Key.Number1));
            SetupDefaultKeybind(Hotkey.EditorToolColor2, new Keybinding(Key.Number2));
            SetupDefaultKeybind(Hotkey.EditorToolColor3, new Keybinding(Key.Number3));

            SetupDefaultKeybind(Hotkey.EditorCycleToolSetting, new Keybinding(Key.Tab));
            SetupDefaultKeybind(Hotkey.EditorMoveStart, new Keybinding(Key.D));

            SetupDefaultKeybind(Hotkey.EditorRemoveLatestLine, new Keybinding(Key.BackSpace));
            SetupDefaultKeybind(Hotkey.EditorFocusStart, new Keybinding(Key.Home));
            SetupDefaultKeybind(Hotkey.EditorFocusLastLine, new Keybinding(Key.End));
            SetupDefaultKeybind(Hotkey.EditorFocusRider, new Keybinding(Key.F1));
            SetupDefaultKeybind(Hotkey.EditorFocusFlag, new Keybinding(Key.F2));
            SetupDefaultKeybind(Hotkey.ToolLifeLock, new Keybinding(KeyModifiers.Alt));
            SetupDefaultKeybind(Hotkey.ToolAngleLock, new Keybinding(KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.ToolAxisLock, new Keybinding(KeyModifiers.Control | KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.ToolPerpendicularAxisLock, new Keybinding(Key.X, KeyModifiers.Control | KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.ToolLengthLock, new Keybinding(Key.L));
            SetupDefaultKeybind(Hotkey.ToolXYSnap, new Keybinding(Key.X));
            SetupDefaultKeybind(Hotkey.ToolToggleSnap, new Keybinding(Key.S));
            SetupDefaultKeybind(Hotkey.ToolSelectBothJoints, new Keybinding(KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.LineToolFlipLine, new Keybinding(KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.EditorUndo, new Keybinding(Key.Z, KeyModifiers.Control));

            SetupDefaultKeybind(Hotkey.EditorRedo,
                new Keybinding(Key.Y, KeyModifiers.Control),
                new Keybinding(Key.Z, KeyModifiers.Control | KeyModifiers.Shift));

            SetupDefaultKeybind(Hotkey.PlaybackStartIgnoreFlag, new Keybinding(Key.Y, KeyModifiers.Alt));
            SetupDefaultKeybind(Hotkey.PlaybackStartGhostFlag, new Keybinding(Key.I, KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.PlaybackStartSlowmo, new Keybinding(Key.Y, KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.PlaybackFlag, new Keybinding(Key.I));
            SetupDefaultKeybind(Hotkey.PlaybackStart, new Keybinding(Key.Y));
            SetupDefaultKeybind(Hotkey.PlaybackStop, new Keybinding(Key.U));
            SetupDefaultKeybind(Hotkey.PlaybackSlowmo, new Keybinding(Key.M));
            SetupDefaultKeybind(Hotkey.PlaybackZoom, new Keybinding(Key.Z));
            SetupDefaultKeybind(Hotkey.PlaybackUnzoom, new Keybinding(Key.X));

            SetupDefaultKeybind(Hotkey.PlaybackSpeedUp,
                new Keybinding(Key.Plus),
                new Keybinding(Key.KeypadPlus));

            SetupDefaultKeybind(Hotkey.PlaybackSpeedDown,
                new Keybinding(Key.Minus),
                new Keybinding(Key.KeypadMinus));

            SetupDefaultKeybind(Hotkey.PlaybackFrameNext, new Keybinding(Key.Right));
            SetupDefaultKeybind(Hotkey.PlaybackFramePrev, new Keybinding(Key.Left));
            SetupDefaultKeybind(Hotkey.PlaybackForward, new Keybinding(Key.Right, KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.PlaybackBackward, new Keybinding(Key.Left, KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.PlaybackIterationNext, new Keybinding(Key.Right, KeyModifiers.Alt));
            SetupDefaultKeybind(Hotkey.PlaybackIterationPrev, new Keybinding(Key.Left, KeyModifiers.Alt));
            SetupDefaultKeybind(Hotkey.PlaybackTogglePause, new Keybinding(Key.Space));

            SetupDefaultKeybind(Hotkey.PreferencesWindow,
                new Keybinding(Key.Escape),
                new Keybinding(Key.P, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.TrackPropertiesWindow, new Keybinding(Key.T, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.PreferenceOnionSkinning, new Keybinding(Key.O, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.LoadWindow, new Keybinding(Key.O));
            SetupDefaultKeybind(Hotkey.Quicksave, new Keybinding(Key.S, KeyModifiers.Control));

            SetupDefaultKeybind(Hotkey.PlayButtonIgnoreFlag, new Keybinding(KeyModifiers.Alt));

            SetupDefaultKeybind(Hotkey.EditorQuickPan, new Keybinding(Key.Space, KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.EditorDragCanvas, new Keybinding(MouseButton.Middle));

            SetupDefaultKeybind(Hotkey.EditorCancelTool, new Keybinding(Key.Escape));
            SetupDefaultKeybind(Hotkey.PlayButtonIgnoreFlag, new Keybinding(KeyModifiers.Alt));
            SetupDefaultKeybind(Hotkey.PlaybackResetCamera, new Keybinding(Key.N));
            SetupDefaultKeybind(Hotkey.ToolCopy, new Keybinding(Key.C, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.ToolCut, new Keybinding(Key.X, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.ToolPaste, new Keybinding(Key.V, KeyModifiers.Control));
            SetupDefaultKeybind(Hotkey.ToolDelete, new Keybinding(Key.Delete));
            SetupDefaultKeybind(Hotkey.ToolAddSelection, new Keybinding(KeyModifiers.Shift));
            SetupDefaultKeybind(Hotkey.ToolToggleSelection, new Keybinding(KeyModifiers.Control));
        }
        private static void SetupDefaultKeybind(Hotkey hotkey, Keybinding keybinding, Keybinding secondary = null)
        {
            if (keybinding.IsEmpty)
                return;
            DefaultKeybinds[hotkey] = new List<Keybinding>();
            DefaultKeybinds[hotkey].Add(keybinding);
            if (secondary != null)
            {
                DefaultKeybinds[hotkey].Add(secondary);
            }
        }
        private static void LoadDefaultKeybindings()
        {
            foreach (Hotkey hk in Enum.GetValues(typeof(Hotkey)))
            {
                if (hk == Hotkey.None)
                    continue;
                LoadDefaultKeybind(hk);
            }
        }
        public static List<Keybinding> GetHotkeyDefault(Hotkey hotkey)
        {
            if (!DefaultKeybinds.ContainsKey(hotkey))
                return null;
            return DefaultKeybinds[hotkey];
        }
        private static void LoadDefaultKeybind(Hotkey hotkey)
        {
            if (DefaultKeybinds.ContainsKey(hotkey))
            {
                var defaults = DefaultKeybinds[hotkey];
                if (defaults == null || defaults.Count == 0)
                    return;
                var list = Keybinds[hotkey];
                if (list.Count == 0)
                    CreateKeybind(hotkey, defaults[0]);
                if (defaults.Count > 1)
                {
                    var secondary = defaults[1];
                    if (secondary != null && list.Count == 1 && list[0].IsBindingEqual(defaults[0]))
                        CreateKeybind(hotkey, secondary);
                }
            }
        }
        private static void CreateKeybind(Hotkey hotkey, Keybinding keybinding)
        {
            var conflict = CheckConflicts(keybinding, hotkey);
            if (keybinding.IsEmpty || conflict != Hotkey.None)
                return;
            Keybinds[hotkey].Add(keybinding);
        }
        public static Hotkey CheckConflicts(Keybinding keybinding, Hotkey hotkey)
        {
            if (!keybinding.IsEmpty)
            {
                var inputconflicts = Settings.KeybindConflicts[hotkey];
                foreach (var keybinds in Settings.Keybinds)
                {
                    var hk = keybinds.Key;
                    var conflicts = Settings.KeybindConflicts[hk];
                    //if the conflicts is equal to or below inputconflicts
                    //then we can compare for conflict
                    //if conflicts is above inputconflicts, ignore
                    if (inputconflicts.HasFlag(conflicts))
                    {
                        foreach (var keybind in keybinds.Value)
                        {
                            if (keybind.IsBindingEqual(keybinding))
                                return hk;
                        }
                    }
                }
            }
            return Hotkey.None;
        }
        public static void Load()
        {
            string[] lines = null;
            try
            {
                if (!File.Exists(Program.UserDirectory + "settings.conf"))
                {
                    Save();
                }
                lines = File.ReadAllLines(Program.UserDirectory + "settings.conf");
            }
            catch
            {
            }
            LoadInt(GetSetting(lines, nameof(PlaybackZoomType)), ref PlaybackZoomType);
            LoadFloat(GetSetting(lines, nameof(Volume)), ref Volume);
            LoadFloat(GetSetting(lines, nameof(ScrollSensitivity)), ref ScrollSensitivity);
            LoadBool(GetSetting(lines, nameof(SuperZoom)), ref SuperZoom);
            LoadBool(GetSetting(lines, nameof(WhiteBG)), ref WhiteBG);
            LoadBool(GetSetting(lines, nameof(NightMode)), ref NightMode);
            LoadBool(GetSetting(lines, nameof(SmoothCamera)), ref SmoothCamera);
            LoadBool(GetSetting(lines, nameof(CheckForUpdates)), ref CheckForUpdates);
            LoadBool(GetSetting(lines, nameof(SmoothPlayback)), ref SmoothPlayback);
            LoadBool(GetSetting(lines, nameof(RoundLegacyCamera)), ref RoundLegacyCamera);
            LoadBool(GetSetting(lines, nameof(Record1080p)), ref Record1080p);
            LoadBool(GetSetting(lines, nameof(RecordSmooth)), ref RecordSmooth);
            LoadBool(GetSetting(lines, nameof(RecordMusic)), ref RecordMusic);
            LoadBool(GetSetting(lines, nameof(Editor.LifeLockNoFakie)), ref Editor.LifeLockNoFakie);
            LoadBool(GetSetting(lines, nameof(Editor.LifeLockNoOrange)), ref Editor.LifeLockNoOrange);
            LoadInt(GetSetting(lines, nameof(SettingsPane)), ref SettingsPane);
            LoadBool(GetSetting(lines, nameof(MuteAudio)), ref MuteAudio);
            LoadBool(GetSetting(lines, nameof(Editor.HitTest)), ref Editor.HitTest);
            LoadBool(GetSetting(lines, nameof(Editor.SnapNewLines)), ref Editor.SnapNewLines);
            LoadBool(GetSetting(lines, nameof(Editor.SnapMoveLine)), ref Editor.SnapMoveLine);
            LoadBool(GetSetting(lines, nameof(Editor.ForceXySnap)), ref Editor.ForceXySnap);
            LoadBool(GetSetting(lines, nameof(Editor.MomentumVectors)), ref Editor.MomentumVectors);
            LoadBool(GetSetting(lines, nameof(Editor.RenderGravityWells)), ref Editor.RenderGravityWells);
            LoadBool(GetSetting(lines, nameof(Editor.DrawContactPoints)), ref Editor.DrawContactPoints);
            LoadBool(GetSetting(lines, nameof(PreviewMode)), ref PreviewMode);
            LoadInt(GetSetting(lines, nameof(SlowmoSpeed)), ref SlowmoSpeed);
            LoadFloat(GetSetting(lines, nameof(DefaultPlayback)), ref DefaultPlayback);
            LoadBool(GetSetting(lines, nameof(ColorPlayback)), ref ColorPlayback);
            LoadBool(GetSetting(lines, nameof(OnionSkinning)), ref OnionSkinning);
            var lasttrack = GetSetting(lines, nameof(LastSelectedTrack));
            if (File.Exists(lasttrack) && lasttrack.StartsWith(Constants.TracksDirectory))
            {
                LastSelectedTrack = lasttrack;
            }
            foreach (Hotkey hk in Enum.GetValues(typeof(Hotkey)))
            {
                if (hk == Hotkey.None)
                    continue;
                LoadKeybinding(lines, hk);
            }

            Volume = MathHelper.Clamp(Settings.Volume, 0, 100);
            LoadDefaultKeybindings();
        }
        public static void Save()
        {
            string config = MakeSetting(nameof(LastSelectedTrack), LastSelectedTrack); 
            config += "\r\n" + MakeSetting(nameof(Volume), Volume.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SuperZoom), SuperZoom.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(WhiteBG), WhiteBG.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(NightMode), NightMode.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SmoothCamera), SmoothCamera.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(CheckForUpdates), CheckForUpdates.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SmoothPlayback), SmoothPlayback.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(PlaybackZoomType), PlaybackZoomType.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(RoundLegacyCamera), RoundLegacyCamera.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Record1080p), Record1080p.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(RecordSmooth), RecordSmooth.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(RecordMusic), RecordMusic.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(ScrollSensitivity), ScrollSensitivity.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.LifeLockNoFakie), Editor.LifeLockNoFakie.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.LifeLockNoOrange), Editor.LifeLockNoOrange.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SettingsPane), SettingsPane.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(MuteAudio), MuteAudio.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.HitTest), Editor.HitTest.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.SnapNewLines), Editor.SnapNewLines.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.SnapMoveLine), Editor.SnapMoveLine.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.ForceXySnap), Editor.ForceXySnap.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.MomentumVectors), Editor.MomentumVectors.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.RenderGravityWells), Editor.RenderGravityWells.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(Editor.DrawContactPoints), Editor.DrawContactPoints.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(PreviewMode), PreviewMode.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(SlowmoSpeed), SlowmoSpeed.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(DefaultPlayback), DefaultPlayback.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(ColorPlayback), ColorPlayback.ToString(Program.Culture));
            config += "\r\n" + MakeSetting(nameof(OnionSkinning), OnionSkinning.ToString(Program.Culture));
            foreach (var binds in Keybinds)
            {
                foreach (var bind in binds.Value)
                {
                    if (KeybindConflicts[binds.Key] == KeyConflicts.HardCoded)
                        continue;
                    if (!bind.IsEmpty)
                    {
                        config += "\r\n" + MakeSetting(binds.Key.ToString(),
                        "Mod=" + bind.Modifiers.ToString() +
                        ";Key=" + bind.Key.ToString() +
                        ";Mouse=" + bind.MouseButton.ToString() + ";");
                    }
                }
            }
            try
            {
                File.WriteAllText(Program.UserDirectory + "settings.conf", config);
            }
            catch { }
        }
        private static void LoadKeybinding(string[] config, Hotkey hotkey)
        {
            if (KeybindConflicts[hotkey] == KeyConflicts.HardCoded)
                return;
            int line = 0;
            var hotkeyname = hotkey.ToString();
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
