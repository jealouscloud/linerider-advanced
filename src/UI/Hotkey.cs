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
        EditorDragCanvas,

        EditorToolColor1,
        EditorToolColor2,
        EditorToolColor3,
        EditorRemoveLatestLine,
        EditorFocusStart,
        EditorFocusLastLine,
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
        ToolToggleSnap,
        ToolSelectBothJoints,
        ToolCopy,
        ToolCut,
        ToolPaste,
        ToolDelete,
        ToolAddSelection,
        ToolToggleSelection,
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
        PlaybackResetCamera,
        PreferenceOnionSkinning,
        PreferencesWindow,
        TrackPropertiesWindow,
        LoadWindow,
        Quicksave,

        PlayButtonIgnoreFlag
    }

}