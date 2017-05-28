using MatterHackers.VectorMath;

//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
using System.Collections.Generic;

namespace MatterHackers.Agg.VertexSource
{
    public interface IVertexSource
    {
        #region Methods

        void rewind(int pathId = 0);

        ShapePath.FlagsAndCommand vertex(out double x, out double y);

        IEnumerable<VertexData> Vertices();

        #endregion Methods

        // for a PathStorage this is the vertex index.
    }

    public interface IVertexSourceProxy : IVertexSource
    {
        #region Properties

        IVertexSource VertexSource { get; set; }

        #endregion Properties
    }

    public struct VertexData
    {
        #region Fields

        public ShapePath.FlagsAndCommand command;

        public Vector2 position;

        #endregion Fields

        #region Properties

        public bool IsLineTo
        {
            get { return ShapePath.is_line_to(command); }
        }

        public bool IsMoveTo
        {
            get { return ShapePath.is_move_to(command); }
        }

        #endregion Properties

        #region Constructors

        public VertexData(ShapePath.FlagsAndCommand command, Vector2 position)
        {
            this.command = command;
            this.position = position;
        }

        #endregion Constructors

        #region Methods

        public override string ToString()
        {
            return command.ToString();
        }

        #endregion Methods
    }
}