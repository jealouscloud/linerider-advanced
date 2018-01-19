using OpenTK;
namespace linerider.Game
{

    public struct CellLocation
    {
        public Vector2d Remainder;
        public GridPoint Point; 

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