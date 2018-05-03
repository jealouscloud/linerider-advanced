using System;
using System.Collections.Generic;
using linerider.Game;
using OpenTK;

namespace linerider.Tools
{
    /// <summary>
    /// Represents a selected line with a clone of the line before any change
    /// and Line being the current line state
    /// </summary>
    public class LineSelection
    {
        public GameLine line;
        public GameLine clone;
        /// <summary>
        /// Is selection snapped to Position
        /// </summary>
        public bool joint1;
        /// <summary>
        /// Is selection snapped to Position2
        /// </summary>
        public bool joint2;
        /// <summary>
        /// Optional list of lines that are snapped this selection
        /// </summary>
        public List<LineSelection> snapped;
        public LineSelection()
        {
        }
        /// <summary>
        /// Initialize a lineselection automatically generating clone and applying bothjoints
        /// </summary>
        public LineSelection(GameLine Line, bool bothjoints)
        {
            line = Line;
            clone = line.Clone();
            joint1 = bothjoints;
            joint2 = bothjoints;
            snapped = new List<LineSelection>();
        }
        /// <summary>
        /// Initialize a lineselection automatically generating clone and generating joint snap
        /// </summary>
        public LineSelection(GameLine Line, Vector2d snapjoint)
        {
            line = Line;
            clone = line.Clone();
            joint1 = line.Position == snapjoint;
            joint2 = line.Position2 == snapjoint;
            snapped = new List<LineSelection>();
        }
    }
}