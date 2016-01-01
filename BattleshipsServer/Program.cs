using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System;
using CommandUtils;

namespace BattleshipsServer
{
    class Program
    {
        //Server Data
        private Socket listenerSocket;
        private IPAddress serverIP;
        private int serverPort;
        private BackgroundWorker bgListener;
        //Client Data
        private List<ServerClient> clientList;
        SQLiteDatabase dataBase;
        //Game Data
        private List<BattleshipsGame> activeGames;

        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.clientList = new List<ServerClient>();
            prog.activeGames = new List<BattleshipsGame>();

            if (args.Length == 0)
            {
                prog.serverIP = IPAddress.Loopback;
                prog.serverPort = 10001;
            }
            else if (args.Length == 1)
            {
                prog.serverIP = IPAddress.Parse(args[0]);
                prog.serverPort = 10001;
            }
            else if (args.Length == 2)
            {
                prog.serverIP = IPAddress.Parse(args[0]);
                prog.serverPort = int.Parse(args[1]);
            }

            prog.dataBase = new SQLiteDatabase();
            if (!System.IO.File.Exists("clientData.s3db"))
            {
                string createTableQuery = @"CREATE TABLE IF NOT EXISTS [Users] ([Username] TEXT NOT NULL PRIMARY KEY, [Wins] INTEGER NOT NULL, [Losses] INTEGER NOT NULL)";
                System.Data.SQLite.SQLiteConnection.CreateFile("clientData.s3db");
                prog.dataBase.ExecuteNonQuery(createTableQuery);
            }
            prog.bgListener = new BackgroundWorker();
            prog.bgListener.WorkerSupportsCancellation = true;
            prog.bgListener.DoWork += new DoWorkEventHandler(prog.Listen);
            prog.bgListener.RunWorkerAsync();

            Console.WriteLine("Listening on {0}:{1}. Press ENTER to shutdown the server.", prog.serverIP, prog.serverPort);
            Console.ReadLine();

            prog.DisconnectServer();
        }
        private void Listen(object sender, DoWorkEventArgs e)
        {
            try
            {
                listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenerSocket.Bind(new IPEndPoint(serverIP, serverPort));
                listenerSocket.Listen(200);
                while (true)
                {
                    AddNewClient(listenerSocket.Accept());
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SERVER NOT CONNECTED! Socket already in use! Please re-run server using a different port.");
                Console.WriteLine(ex.Message);
            }
        }

        private void DisconnectServer()
        {
            if (clientList != null)
            {
                foreach (ServerClient client in clientList)
                {
                    client.Disconnect();

                    bgListener.CancelAsync();
                    bgListener.Dispose();
                    listenerSocket.Close();
                    GC.Collect();
                }
            }
        }

        private void AddNewClient(Socket socket)
        {
            ServerClient newClient = new ServerClient(socket);
            newClient.CommandRecieved += new CommandRecievedEventHandler(CommandRecieved);
            newClient.Disconnected += new DisconnectedEventHandler(ClientDisconnected);
            clientList.Add(newClient);
            Console.WriteLine("User connected from {0}:{1} at {2}/{3}", newClient.IP, newClient.Port, DateTime.Now.ToShortTimeString(), DateTime.Now.ToLongDateString());
        }

        private string CheckForClientData(string username)
        {
            string query = string.Format("SELECT COUNT(*) FROM Users WHERE Username = '{0}'", username);
            string retrievedData = "";
            int count = -1;
            try
            {
                count = int.Parse(dataBase.GetSingleEntry(query));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                count = 0;
            }
            if (count == 1)
            {
                //Check to see if data exists for this username in the database. If it does return the wins/losses to be stored in the ServerClient class
                //Get wins
                query = string.Format("SELECT Wins FROM Users WHERE Username = '{0}';", username);
                retrievedData = dataBase.GetSingleEntry(query);
                retrievedData += ":";
                //Get Losses
                query = string.Format("SELECT Losses FROM Users WHERE Username = '{0}';", username);
                retrievedData += dataBase.GetSingleEntry(query);
                return retrievedData;
            }
            else if (count == 0)
            {
                //If no data exists for user, create default data
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("USERNAME", username);
                data.Add("WINS", "0");
                data.Add("LOSSES", "0");
                try
                {
                    dataBase.Insert("USERS", data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                retrievedData = "0:0";
            }
            return retrievedData;
        }

        private void CommandRecieved(object sender, CommandEventArgs e)
        {
            //When a user connects set their client username
            if (e.Command.CommandType == CommandUtils.CommandType.UserConnected)
            {
                string username = e.Command.Data.Split(':')[2];
                Console.WriteLine("Checking for : " + username);
                bool nameAvailability = CheckUsernameAvailability(e.Command.SenderIP, e.Command.SenderPort, username);
                if (nameAvailability)
                {
                    string data = CheckForClientData(username);

                    SetClientUsername(e.Command.SenderIP, e.Command.SenderPort, username);
                    SetClientData(e.Command.SenderIP, e.Command.SenderPort, data);
                    SendUserStats(e.Command.SenderIP, e.Command.SenderPort, data);
                    AnswerUsernameRequest(e.Command.SenderIP, e.Command.SenderPort, nameAvailability);
                    e.Command.Data += (":" + data);
                    SendCommandToAll(e.Command);
                }
                else if (nameAvailability == false)
                {
                    AnswerUsernameRequest(e.Command.SenderIP, e.Command.SenderPort, nameAvailability);
                }

            }

            //User asks to disconnect
            if (e.Command.CommandType == CommandUtils.CommandType.UserDisconnectRequest)
            {
                int index = FindClientID(e.Command.SenderIP, e.Command.SenderPort);
                string clientDetails = clientList[index].IP.ToString() + ":" + clientList[index].Port.ToString() + ":" + clientList[index].Username;
                Console.WriteLine("User {0}:{1} ({2}) has disconnected ({3}/{4})", e.Command.SenderIP, e.Command.SenderPort, clientList[index].Username, DateTime.Now.ToShortTimeString(), DateTime.Now.ToLongDateString());
                clientList[index].Disconnect();
                clientList.RemoveAt(index);
                Command cmd = new Command(CommandUtils.CommandType.UserDisconnected, IPAddress.Broadcast);
                cmd.SenderName = e.Command.SenderName;
                cmd.SenderIP = e.Command.SenderIP;
                cmd.SenderPort = e.Command.SenderPort;
                cmd.Data = clientDetails;
                SendCommandToAll(cmd);

            }

            //Reply to client list request
            if (e.Command.CommandType == CommandUtils.CommandType.ClientListRequest)
            {
                SendClientList(e.Command.SenderIP, e.Command.SenderPort);
            }

            //Sends message commands to all connected clients
            if (e.Command.CommandType == CommandUtils.CommandType.Message)
            {
                if (e.Command.TargetIP.Equals(IPAddress.Broadcast))
                {
                    SendCommandToAll(e.Command);
                }
                else
                {
                    SendCommandToClient(e.Command);
                }
            }

            //Pass challenge request to challenged user
            if (e.Command.CommandType == CommandUtils.CommandType.ChallengeRequest)
            {
                SendCommandToClient(e.Command);
            }

            //Pass challenge response to challenger
            if (e.Command.CommandType == CommandUtils.CommandType.ChallengeResponse)
            {
                SendCommandToClient(e.Command);
            }

            //Handle game start request
            if (e.Command.CommandType == CommandUtils.CommandType.GameStartRequest)
            {
                IPAddress client2IP = IPAddress.Parse(e.Command.Data.Split(':')[0]);
                int client2Port = int.Parse(e.Command.Data.Split(':')[1]);
                int client1ID = FindClientID(e.Command.SenderIP, e.Command.SenderPort);
                int client2ID = FindClientID(client2IP, client2Port);
                BattleshipsGame game = new BattleshipsGame(clientList[client1ID], clientList[client2ID]);

                Command cmd = new Command(CommandUtils.CommandType.GameIDInform, e.Command.SenderIP, activeGames.Count.ToString());
                cmd.SenderName = "server";
                cmd.SenderIP = serverIP;
                cmd.SenderPort = serverPort;
                cmd.TargetPort = e.Command.SenderPort;
                SendCommandToClient(cmd);

                cmd.TargetIP = client2IP;
                cmd.TargetPort = client2Port;
                SendCommandToClient(cmd);

                activeGames.Add(game);
            }

            //Handle ShipPlacementRequest
            if (e.Command.CommandType == CommandUtils.CommandType.GameShipRequest)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].CommandRecieved(e);
            }
            //Handle GameShotRequest
            if (e.Command.CommandType == CommandUtils.CommandType.GameShotRequest)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].CommandRecieved(e);
            }
            //Handle GameOverInform
            if (e.Command.CommandType == CommandUtils.CommandType.GameOverInform)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].GameOverMessageCount++;
                if (activeGames[int.Parse(e.Command.Data.Split(':')[0])].GameOverMessageCount >= 2)
                {
                    PostGameStatisticsUpdate(activeGames[int.Parse(e.Command.Data.Split(':')[0])].Clients);
                    activeGames.RemoveAt(int.Parse(e.Command.Data.Split(':')[0]));
                    GC.Collect();
                }
            }
        }

        private void PostGameStatisticsUpdate(ServerClient[] clients)
        {
            for (int i = 0; i < clients.Length; i++)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("WINS", clients[i].Wins.ToString());
                data.Add("LOSSES", clients[i].Losses.ToString());
                try
                {
                    dataBase.Update("USERS", data, string.Format("USERNAME = {0}", clients[i].Username));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Data Update Failed: " + e.Message);
                }

            }
        }

        private void DisconnectUser(Command dcCmd)
        {
            int index = FindClientID(dcCmd.SenderIP, dcCmd.SenderPort);
            string clientDetails = clientList[index].IP.ToString() + ":" + clientList[index].Port.ToString() + ":" + clientList[index].Username;
            Console.WriteLine("User {0}:{1} ({2}) has disconnected ({3}/{4})", dcCmd.SenderIP, dcCmd.SenderPort, clientList[index].Username, DateTime.Now.ToShortTimeString(), DateTime.Now.ToLongDateString());
            clientList[index].Disconnect();
            clientList.RemoveAt(index);
            Command cmd = new Command(CommandUtils.CommandType.UserDisconnected, IPAddress.Broadcast);
            cmd.SenderName = dcCmd.SenderName;
            cmd.SenderIP = dcCmd.SenderIP;
            cmd.SenderPort = dcCmd.SenderPort;
            cmd.Data = clientDetails;
            SendCommandToAll(cmd);
        }

        private void ClientDisconnected(object sender, DisconnectEventArgs e)
        {
            DisconnectUser(e.Command);
        }

        private void SendClientList(IPAddress targetIP, int targetPort)
        {
            string clientList = "";
            string clientDetails = "";
            foreach (ServerClient client in this.clientList)
            {
                clientDetails = client.IP.ToString() + ':' + client.Port.ToString() + ':' + client.Username + ':' + client.Wins.ToString() + ':' + client.Losses.ToString() + ',';
                clientList += clientDetails;
            }
            Console.WriteLine("Send Client list info to: " + targetIP + ':' + targetPort);
            Command cmd = new Command(CommandUtils.CommandType.ClientListRequest, targetIP);
            cmd.TargetPort = targetPort;
            cmd.Data = clientList;
            cmd.SenderIP = serverIP;
            cmd.SenderPort = serverPort;
            cmd.SenderName = "Server";
            SendCommandToClient(cmd);
        }

        private bool CheckUsernameAvailability(IPAddress ip, int port, string name) //MUST HANDLE IPS AND PORTS TO MANAGE MULTIPLE USERS ON SAME MACHINE
        {
            foreach (ServerClient client in clientList)
            {
                if (client.Username == name)
                {
                    return false;
                }
            }
            return true;
        }

        private void AnswerUsernameRequest(IPAddress targetIP, int targetPort, bool usernameAvailiability)
        {
            Command cmd = new Command(CommandUtils.CommandType.UsernameRequest, targetIP, usernameAvailiability.ToString());
            cmd.TargetPort = targetPort;
            cmd.SenderIP = serverIP;
            cmd.SenderPort = serverPort;
            cmd.SenderName = "Server";
            Console.WriteLine("Username Response Sent to {0}:{1} Value: {2}", targetIP.ToString(), targetPort.ToString(), usernameAvailiability.ToString());
            SendCommandToClient(cmd);
        }

        private void SendUserStats(IPAddress targetIP, int targetPort, string data)
        {
            Command cmd = new Command(CommandType.UserDataInform, targetIP, data);
            cmd.TargetPort = targetPort;
            cmd.SenderIP = serverIP;
            cmd.SenderPort = serverPort;
            cmd.SenderName = "server";
            SendCommandToClient(cmd);
        }

        private void SetClientUsername(IPAddress ip, int port, string usernameData)
        {
            int index = FindClientID(ip, port);
            if (index != -1)
            {
                string username = usernameData;
                clientList[index].Username = username;
            }
        }

        private void SetClientData(IPAddress ip, int port, string data)
        {
            int index = FindClientID(ip, port);
            if (index != -1)
            {
                int wins = int.Parse(data.Split(':')[0]);
                int losses = int.Parse(data.Split(':')[1]);
                clientList[index].Wins = wins;
                clientList[index].Losses = losses;
            }
        }

        private bool RemoveClientFromList(IPAddress ip, int port)
        {
            lock (this)
            {
                int index = FindClientID(ip, port);
                if (index != -1)
                {
                    string clientName = clientList[index].Username;
                    clientList.RemoveAt(index);

                    Command cmd = new Command(CommandUtils.CommandType.UserDisconnected, IPAddress.Broadcast);
                    cmd.SenderName = clientName;
                    cmd.SenderIP = ip;
                    cmd.SenderPort = port;
                    SendCommandToAll(cmd);
                    return true;
                }
                return false;
            }
        }
        private int FindClientID(IPAddress ip, int port)
        {
            int index = -1;
            foreach (ServerClient client in clientList)
            {
                index++;
                if (client.IP.Equals(ip) && client.Port.Equals(port))
                    return index;
            }
            return -1;
        }

        private void SendCommandToAll(Command cmd)
        {
            IPEndPoint senderEndpoint = new IPEndPoint(cmd.SenderIP, cmd.SenderPort);
            foreach (ServerClient client in clientList)
            {
                if (client.Connected)
                {
                    IPEndPoint endPoint = new IPEndPoint(client.IP, client.Port);
                    if (endPoint != senderEndpoint)
                        client.SendCommand(cmd);
                }
            }
        }

        private void SendCommandToClient(Command cmd)
        {
            foreach (ServerClient client in clientList)
            {
                if (client.IP.Equals(cmd.TargetIP) && client.Port.Equals(cmd.TargetPort))
                {
                    client.SendCommand(cmd);
                    break;
                }
            }
        }

    }
}
