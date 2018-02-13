using OpenTK;
namespace linerider.Game
{

    public struct CellLocation
    {
        public Vector2d Remainder;
        public GridPoint Point; 
        public CellLocation(GridPoint point, Vector2d remainder)
        {
            Point = point;
            Remainder = remainder;
        }
        public int X 
        {
            get
            {
                return Point.X;
            }
            set
            {
                Point.X = value;
            }
        }

        public int Y
        {
            get
            {
                return Point.Y;
            }
            set
            {
                Point.Y = value;
            }
        }
    }
}