using System.Collections.Generic;

using CommandUtils;

namespace BattleshipsServer
{
    class GameBoard
    {
        GridType[,] board;
        public bool setupComplete = false;
        private int shipSquaresRemaining = 0;

        public void InitBoard()
        {
            board = new GridType[10, 10];
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    board[c, r] = GridType.Water;
                }
            }
        }

        public void ChangeGridType(int x, int y, GridType type)
        {
            board[x, y] = type;
        }

        public GridType GetGridType(int x, int y)
        {
            return board[x, y];
        }

        public int ShipSquaresRemaining
        {
            get { return shipSquaresRemaining; }
            set { shipSquaresRemaining = value; }
        }

        public bool AreShipsRemaining()
        {
            if (shipSquaresRemaining > 0)
            {
                return true;
            }
            return false;
        }
    }
    class BattleshipsGame
    {
        GameBoard client1Board;
        GameBoard client2Board;
        ServerClient[] clients;
        List<GridPosition> client1TempPositions;
        List<GridPosition> client2TempPositions;
        int gameOverMessageCount = 0;

        public int GameOverMessageCount
        {
            get { return gameOverMessageCount; }
            set { gameOverMessageCount = value; }
        }

        public BattleshipsGame(ServerClient client1, ServerClient client2)
        {
            client1Board = new GameBoard();
            client2Board = new GameBoard();
            clients = new ServerClient[2];
            clients[0] = client1;
            clients[1] = client2;
            client1Board.InitBoard();
            client2Board.InitBoard();
            client1TempPositions = new List<GridPosition>();
            client2TempPositions = new List<GridPosition>();

        }

        public void CommandRecieved(CommandEventArgs e)
        {
            int clientNo = -1;
            if (clients[0].Username == e.Command.SenderName)
            {
                clientNo = 0;
            }
            else if (clients[1].Username == e.Command.SenderName)
            {
                clientNo = 1;
            }
            if (e.Command.CommandType == CommandType.GameShipRequest)
            {
                ParseShipData(e.Command.Data.Split(':')[1], clientNo);
                PlaceShips(clientNo);
                if (client1Board.setupComplete == true && client2Board.setupComplete == true)
                {
                    Command cmd = new Command(CommandType.GameStartInform, e.Command.SenderIP);
                    cmd.SenderName = "server";
                    cmd.TargetIP = clients[0].IP;
                    cmd.TargetPort = clients[0].Port;
                    cmd.Data = "true";
                    SendCommandToClient(cmd);
                    Command cmd2 = new Command(CommandType.GameStartInform, clients[1].IP);
                    cmd2.SenderName = "server";
                    cmd2.TargetPort = clients[1].Port;
                    cmd2.Data = "false";
                    SendCommandToClient(cmd2);
                }
            }
            if (e.Command.CommandType == CommandType.GameShotRequest)
            {
                if (CheckForHits(clientNo, e.Command.Data.Split(':')[1]))
                {
                    Command cmd = new Command(CommandType.GameShotResult, clients[clientNo].IP);
                    cmd.Data = "hit";
                    cmd.TargetPort = clients[clientNo].Port;
                    cmd.SenderName = "server";
                    SendCommandToClient(cmd);
                    Command cmd2 = new Command(CommandType.GameHitInform, e.Command.TargetIP);
                    cmd2.SenderName = "server";
                    cmd2.Data = e.Command.Data.Split(':')[1];
                    if (clientNo == 0)
                    {
                        cmd2.TargetIP = clients[1].IP;
                        cmd2.TargetPort = clients[1].Port;
                    }
                    else if (clientNo == 1)
                    {
                        cmd2.TargetIP = clients[0].IP;
                        cmd2.TargetPort = clients[0].Port;
                    }
                    SendCommandToClient(cmd2);

                    //Check is any ships remain in each board, if a board has no sips remaining then the game is over and that client loses
                    if (client1Board.AreShipsRemaining() == false)
                    {
                        Command winCmd = new Command(CommandType.GameOverInform, clients[1].IP, "win");
                        winCmd.TargetPort = clients[1].Port;
                        SendCommandToClient(winCmd);
                        Command lossCmd = new Command(CommandType.GameOverInform, clients[0].IP, "loss");
                        lossCmd.TargetPort = clients[0].Port;
                        SendCommandToClient(lossCmd);
                    }
                    else if (client2Board.AreShipsRemaining() == false)
                    {
                        Command winCmd = new Command(CommandType.GameOverInform, clients[0].IP, "win");
                        winCmd.TargetPort = clients[0].Port;
                        SendCommandToClient(winCmd);
                        Command lossCmd = new Command(CommandType.GameOverInform, clients[1].IP, "loss");
                        lossCmd.TargetPort = clients[1].Port;
                        SendCommandToClient(lossCmd);
                    }
                }
                else
                {
                    Command cmd = new Command(CommandType.GameShotResult, clients[clientNo].IP);
                    cmd.Data = "miss";
                    cmd.TargetPort = clients[clientNo].Port;
                    cmd.SenderName = "server";
                    SendCommandToClient(cmd);
                    Command cmd2 = new Command(CommandType.GameMissInform, e.Command.TargetIP);
                    cmd2.Data = e.Command.Data.Split(':')[1];
                    cmd2.SenderName = "server";


                    if (clientNo == 0)
                    {
                        cmd2.TargetIP = clients[1].IP;
                        cmd2.TargetPort = clients[1].Port;
                    }
                    else if (clientNo == 1)
                    {
                        cmd2.TargetIP = clients[0].IP;
                        cmd2.TargetPort = clients[0].Port;
                    }
                    SendCommandToClient(cmd2);
                }
            }
        }

        private void SendCommandToClient(Command cmd)
        {
            if (clients[0].IP.Equals(cmd.TargetIP) && clients[0].Port.Equals(cmd.TargetPort))
            {
                clients[0].SendCommand(cmd);
            }
            else if (clients[1].IP.Equals(cmd.TargetIP) && clients[1].Port.Equals(cmd.TargetPort))
            {
                clients[1].SendCommand(cmd);
            }
        }

        private void SendCommandToAll(Command cmd)
        {
            clients[0].SendCommand(cmd);
            clients[1].SendCommand(cmd);
        }

        public bool CheckForHits(int clientNo, string data)
        {
            GridPosition pos = new GridPosition(int.Parse(data.Split(',')[0]), int.Parse(data.Split(',')[1]));
            Command cmd = new Command();
            switch (clientNo)
            {
                case 0:
                    if (client2Board.GetGridType(pos.x, pos.y) == GridType.Ship)
                    {
                        client2Board.ChangeGridType(pos.x, pos.y, GridType.Hit);
                        client2Board.ShipSquaresRemaining--;
                        return true;
                    }
                    else
                        return false;
                case 1:
                    if (client1Board.GetGridType(pos.x, pos.y) == GridType.Ship)
                    {
                        client1Board.ChangeGridType(pos.x, pos.y, GridType.Hit);
                        client1Board.ShipSquaresRemaining--;
                        return true;
                    }
                    else
                        return false;
            }
            return false;
        }

        public void PlaceShips(int clientNo)
        {
            for (int i = 0; i < 17; i++)
            {
                if (clientNo == 0)
                {
                    client1Board.ChangeGridType(client1TempPositions[i].x, client1TempPositions[i].y, GridType.Ship);
                    client1Board.ShipSquaresRemaining++;
                }
                else if (clientNo == 1)
                {
                    client2Board.ChangeGridType(client2TempPositions[i].x, client2TempPositions[i].y, GridType.Ship);
                    client2Board.ShipSquaresRemaining++;

                }
            }
            if (clientNo == 0)
            {
                client1Board.setupComplete = true;
            }
            else if (clientNo == 1)
            {
                client2Board.setupComplete = true;
            }
        }

        public void ParseShipData(string data, int clientNo)
        {
            string[] ships = new string[5];
            for (int i = 0; i < 5; i++)
            {
                ships[i] = data.Split(';')[i];
                int length = 0;
                ShipType type = ShipType.Default;
                switch (i)
                {
                    case 0:
                        length = 2;
                        type = ShipType.Destroyer;
                        break;
                    case 1:
                        length = 3;
                        type = ShipType.Cruiser;
                        break;
                    case 2:
                        length = 3;
                        type = ShipType.Submarine;
                        break;
                    case 3:
                        length = 4;
                        type = ShipType.Battleship;
                        break;
                    case 4:
                        length = 5;
                        type = ShipType.Carrier;
                        break;
                }
                CalculateShipPositions(ships[i], type, length, clientNo);
            }
        }

        private void CalculateShipPositions(string ship, ShipType type, int length, int clientNo)
        {
            string[] points = new string[2];
            points[0] = ship.Split('.')[0];
            points[1] = ship.Split('.')[1];
            bool isHorizontal = false;
            GridPosition startPos = new GridPosition(int.Parse(points[0].Split(',')[0]), int.Parse(points[0].Split(',')[1]));
            GridPosition endPos = new GridPosition(int.Parse(points[1].Split(',')[0]), int.Parse(points[1].Split(',')[1]));

            if (startPos.x == endPos.x)//Ship is vertical
            {
                isHorizontal = false;
            }
            else if (startPos.y == endPos.y)//Ship is horizontal
            {
                isHorizontal = true;
            }

            Ship tempShip = new Ship(type, startPos, endPos, length, isHorizontal);

            for (int i = 0; i < tempShip.occupiedSquares.Length; i++)
            {
                if (clientNo == 0)
                {
                    client1TempPositions.Add(tempShip.occupiedSquares[i]);
                }
                else if (clientNo == 1)
                {
                    client2TempPositions.Add(tempShip.occupiedSquares[i]);
                }
            }
        }
    }
}
