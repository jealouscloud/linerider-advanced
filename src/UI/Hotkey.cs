using System;
using System.Collections.Generic;
namespace linerider.UI
{
    public enum Hotkey
    {
        None,
        EditorPencilTool,
        EditorLineTool,
        EditorEraserTool,
        EditorSelectTool,
        EditorPanTool,
        EditorQuickPan,

        EditorToolColor1,
        EditorToolColor2,
        EditorToolColor3,
        EditorRemoveLatestLine,
        EditorFocusStart,
        EditorFocusLastLine,
        EditorUseTool,
        EditorCycleToolSetting,
        EditorUndo,
        EditorRedo,
        EditorFocusRider,
        EditorFocusFlag,
        EditorCancelTool,
        EditorMoveStart,

        ToolLifeLock,
        ToolAngleLock,
        ToolAxisLock,
        ToolPerpendicularAxisLock,
        ToolLengthLock,
        ToolXYSnap,
        ToolDisableSnap,
        ToolSelectBothJoints,
        LineToolFlipLine,

        PlaybackStartSlowmo,
        PlaybackStartIgnoreFlag,
        PlaybackStartGhostFlag,
        PlaybackStart,
        PlaybackStop,
        PlaybackFlag,
        PlaybackSlowmo,
        PlaybackZoom,
        PlaybackUnzoom,
        PlaybackSpeedUp,
        PlaybackSpeedDown,
        PlaybackFrameNext,
        PlaybackFramePrev,
        PlaybackIterationNext,
        PlaybackIterationPrev,
        PlaybackTogglePause,
        PlaybackForward,
        PlaybackBackward,
        PreferenceOnionSkinning,
        PreferencesWindow,
        TrackPropertiesWindow,
        LoadWindow,
        Quicksave,

        PlayButtonIgnoreFlag
    }

}