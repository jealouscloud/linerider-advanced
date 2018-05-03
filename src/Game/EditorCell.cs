using System;
using System.Collections.Generic;
using linerider.Game;
using System.Collections;

namespace linerider
{
    public class EditorCell : LineContainer<GameLine>
    {
        protected override LinkedListNode<GameLine> FindNodeAfter(LinkedListNode<GameLine> node, GameLine line)
        {
            if (line.ID < 0)
            {
                // scenery lines want to skip right to the beginning of the
                // other scenery line ids
                while (node.Value.ID >= 0)
                {
                    node = node.Next;
                    if (node == null)
                        return null;
                }
            }
            while (line.ID >= 0
                ? line.ID < node.Value.ID//phys
                : line.ID > node.Value.ID)//scenery
            {
                node = node.Next;
                if (node == null)
                    return null;
            }
            return node;
        }
    }
}