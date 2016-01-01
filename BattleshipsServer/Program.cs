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

        //Saved for next session
        //http://www.fluxbytes.com/csharp/how-to-create-and-connect-to-an-sqlite-database-in-c/
        //http://www.dreamincode.net/forums/topic/157830-using-sqlite-with-c%23/

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

        private void CommandRecieved(object sender, CommandEventArgs e)
        {
            //When a user connects set their client username
            if (e.Command.CommandType == CommandType.UserConnected)
            {
                string username = e.Command.Data.Split(':')[2];
                Console.WriteLine("Checking for : " + username);
                bool nameAvailability = CheckUsernameAvailability(e.Command.SenderIP, e.Command.SenderPort, username);
                if (nameAvailability)
                {
                    SetClientUsername(e.Command.SenderIP, e.Command.SenderPort, username);
                    AnswerUsernameRequest(e.Command.SenderIP, e.Command.SenderPort, nameAvailability);
                    SendCommandToAll(e.Command);
                }
                else if (nameAvailability == false)
                {
                    AnswerUsernameRequest(e.Command.SenderIP, e.Command.SenderPort, nameAvailability);
                }

            }

            //User asks to disconnect
            if (e.Command.CommandType == CommandType.UserDisconnectRequest)
            {
                int index = FindClientID(e.Command.SenderIP, e.Command.SenderPort);
                string clientDetails = clientList[index].IP.ToString() + ":" + clientList[index].Port.ToString() + ":" + clientList[index].Username;
                Console.WriteLine("User {0}:{1} ({2}) has disconnected ({3}/{4})", e.Command.SenderIP, e.Command.SenderPort, clientList[index].Username, DateTime.Now.ToShortTimeString(), DateTime.Now.ToLongDateString());
                clientList[index].Disconnect();
                clientList.RemoveAt(index);
                Command cmd = new Command(CommandType.UserDisconnected, IPAddress.Broadcast);
                cmd.SenderName = e.Command.SenderName;
                cmd.SenderIP = e.Command.SenderIP;
                cmd.SenderPort = e.Command.SenderPort;
                cmd.Data = clientDetails;
                SendCommandToAll(cmd);

            }

            //Reply to client list request
            if (e.Command.CommandType == CommandType.ClientListRequest)
            {
                SendClientList(e.Command.SenderIP, e.Command.SenderPort);
            }

            //Sends message commands to all connected clients
            if (e.Command.CommandType == CommandType.Message)
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
            if (e.Command.CommandType == CommandType.ChallengeRequest)
            {
                SendCommandToClient(e.Command);
            }
            
            //Pass challenge response to challenger
            if (e.Command.CommandType == CommandType.ChallengeResponse)
            {
                SendCommandToClient(e.Command);
            }

            //Handle game start request
            if (e.Command.CommandType == CommandType.GameStartRequest)
            {
                IPAddress client2IP = IPAddress.Parse(e.Command.Data.Split(':')[0]);
                int client2Port = int.Parse(e.Command.Data.Split(':')[1]);
                int client1ID = FindClientID(e.Command.SenderIP, e.Command.SenderPort);
                int client2ID = FindClientID(client2IP, client2Port);
                BattleshipsGame game = new BattleshipsGame(clientList[client1ID], clientList[client2ID]);

                Command cmd = new Command(CommandType.GameIDInform, e.Command.SenderIP, activeGames.Count.ToString());
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
            if (e.Command.CommandType == CommandType.GameShipRequest)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].CommandRecieved(e);
            }
            //Handle GameShotRequest
            if (e.Command.CommandType == CommandType.GameShotRequest)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].CommandRecieved(e);
            }
            //Handle GameOverInform
            if (e.Command.CommandType == CommandType.GameOverInform)
            {
                activeGames[int.Parse(e.Command.Data.Split(':')[0])].GameOverMessageCount++;
                if (activeGames[int.Parse(e.Command.Data.Split(':')[0])].GameOverMessageCount >= 2)
                {
                    activeGames.RemoveAt(int.Parse(e.Command.Data.Split(':')[0]));
                    GC.Collect();
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
            Command cmd = new Command(CommandType.UserDisconnected, IPAddress.Broadcast);
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
                clientDetails = client.IP.ToString() + ':' + client.Port.ToString() + ':' + client.Username + ',';
                clientList += clientDetails;
            }
            Console.WriteLine("Send Client list info to: " + targetIP + ':' + targetPort);
            Command cmd = new Command(CommandType.ClientListRequest, targetIP);
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
            Command cmd = new Command(CommandType.UsernameRequest, targetIP, usernameAvailiability.ToString());
            cmd.TargetPort = targetPort;
            cmd.SenderIP = serverIP;
            cmd.SenderPort = serverPort;
            cmd.SenderName = "Server";
            Console.WriteLine("Username Response Sent to {0}:{1} Value: {2}", targetIP.ToString(), targetPort.ToString(), usernameAvailiability.ToString());
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

        private bool RemoveClientFromList(IPAddress ip, int port)
        {
            lock (this)
            {
                int index = FindClientID(ip, port);
                if (index != -1)
                {
                    string clientName = clientList[index].Username;
                    clientList.RemoveAt(index);

                    Command cmd = new Command(CommandType.UserDisconnected, IPAddress.Broadcast);
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
