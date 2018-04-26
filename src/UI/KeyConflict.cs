using System;
namespace linerider.UI
{
    [Flags]
    public enum KeyConflicts
    {
        General = 1,
        Playback = General | 2,
        Tool = General | 4,
        LineTool = Tool | 8,
        SelectTool = Tool | 16,
        Misc = General | 32,
        HardCoded = 64,
    }
}