using System;
using OpenTK;
namespace linerider.Lines
{
    public struct LineState
    {
        public Line Parent;
        public Vector2d Pos1;
        public Vector2d Pos2;
        public bool Inverted;
        public StandardLine.ExtensionDirection extension;
        public Line Prev;
        public Line Next;
        public bool Exists;
    }
}
