namespace CommandUtils
{
    public class Ship
    {
        public GridPosition Front
        {
            get { return occupiedSquares[0]; }
        }
        public GridPosition Rear
        {
            get { return occupiedSquares[occupiedSquares.Length]; }
        }
        public GridPosition[] occupiedSquares;
        public int length;
        public ShipType type;
        public Ship(ShipType type, GridPosition front, GridPosition rear, int length, bool isHorizontal)
        {
            this.type = type;
            occupiedSquares = new GridPosition[length];
            this.length = length;
            occupiedSquares[0] = front;
            if (isHorizontal)
            {
                int difference = rear.x - front.x;
                for (int x = 1; x < length; x++)
                {
                    occupiedSquares[x] = new GridPosition(0, 0);
                    if (difference > 0)
                        occupiedSquares[x].x = front.x + x;
                    else if (difference < 0)
                        occupiedSquares[x].x = front.x - x;
                    occupiedSquares[x].y = front.y;
                }
            }
            else if (isHorizontal == false)
            {
                int difference = front.y - rear.y;
                for (int y = 1; y < length; y++)
                {
                    occupiedSquares[y] = new GridPosition(0, 0);
                    if (difference > 0)
                        occupiedSquares[y].y = front.y - y;
                    else if (difference < 0)
                        occupiedSquares[y].y = front.y + y;
                    occupiedSquares[y].x = front.x;
                }
            }
        }
    }
}
