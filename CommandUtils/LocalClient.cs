using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
namespace CommandUtils
{
    public class LocalClient
    {
        //Events
        public event CommandRecievedEventHandler CommandRecieved;
        public event ServerConnectionLostEventHandler ConnectionLost;
        public event SuccessfulConnectionEventHandler ConnectionSuccessful;
        public event UnsuccsessfulConnectionEventHandler ConnectionUnsuccessful;
        //Client Data
        private Socket socket;
        private NetworkStream networkStream;
        private BinaryFormatter formatter;
        private BackgroundWorker bgReciever;
        private string username;
        private IPEndPoint serverAddress;
        private bool signingOut = false;
        private int wins = -1;
        private int losses = -1;
        public IPAddress IP
        {
            get
            {
                if (socket != null)
                    return ((IPEndPoint)socket.LocalEndPoint).Address;
                else
                    return IPAddress.None;
            }
        }
        public int Port
        {
            get
            {
                if (socket != null)
                    return ((IPEndPoint)socket.LocalEndPoint).Port;
                else
                    return -1;
            }
        }
        public bool Connected
        {
            get
            {
                if (socket != null)
                    return socket.Connected;
                else
                    return false;
            }
        }

        public string Username
        {
            get { return username; }
            set { username = value; }
        }

        public IPAddress ServerIP
        {
            get
            {
                if (Connected)
                {
                    return serverAddress.Address;
                }
                else
                {
                    return IPAddress.None;
                }
            }
        }

        public int ServerPort
        {
            get
            {
                if (Connected)
                {
                    return serverAddress.Port;
                }
                else
                {
                    return -1;
                }
            }
        }

        public int Wins
        {
            get { return wins; }
            set { wins = value; }
        }

        public int Losses
        {
            get { return losses; }
            set { losses = value; }
        }

        public LocalClient(IPEndPoint server, string username)
        {
            serverAddress = server;
            this.username = username;
            formatter = new BinaryFormatter();
        }

        public LocalClient(IPAddress ip, int port, string username)
        {
            serverAddress = new IPEndPoint(ip, port);
            this.username = username;
            formatter = new BinaryFormatter();
        }

        public void ConnectToServer()
        {
            BackgroundWorker bgConnector = new BackgroundWorker();
            bgConnector.DoWork += new DoWorkEventHandler(bgConnector_Connect);
            bgConnector.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgConnector_Completed);
            bgConnector.RunWorkerAsync();
        }

        private void bgConnector_Connect(object sender, DoWorkEventArgs e)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(serverAddress);
                e.Result = true;
                networkStream = new NetworkStream(socket);
                bgReciever = new BackgroundWorker();
                bgReciever.WorkerSupportsCancellation = true;
                bgReciever.DoWork += new DoWorkEventHandler(Recieve);
                bgReciever.RunWorkerAsync();

                //Send connection command
                Command cmd = new Command(CommandType.UserConnected, IPAddress.Broadcast, IP.ToString() + ":" + Port.ToString() + ":" + Username);
                cmd.SenderIP = IP;
                cmd.SenderPort = Port;
                cmd.SenderName = Username;
                SendCommand(cmd);
            }
            catch
            {
                e.Result = false;
            }
        }

        public void SendCommand(Command cmd)
        {
            if (socket != null && socket.Connected)
            {
                BackgroundWorker bgSender = new BackgroundWorker();
                bgSender.DoWork += new DoWorkEventHandler(bgSender_Send);
                bgSender.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgSender_Completed);
                bgSender.WorkerSupportsCancellation = true;
                bgSender.RunWorkerAsync(cmd);
            }
            else
            {
                Console.WriteLine("Command failed to send to server");
            }
        }

        private void bgSender_Send(object sender, DoWorkEventArgs e)
        {
            Command cmd = (Command)e.Argument;
            e.Result = SendCommandToServer(cmd);
        }

        System.Threading.Semaphore semaphor = new System.Threading.Semaphore(1, 1);
        private bool SendCommandToServer(Command cmd)
        {
            try
            {
                semaphor.WaitOne();
                formatter.Serialize(networkStream, cmd);
                networkStream.Flush();
                semaphor.Release();
                return true;
            }
            catch
            {
                semaphor.Release();
                Console.WriteLine("Error sending data to server");
                return false;
            }
        }

        private void bgSender_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ((BackgroundWorker)sender).Dispose();
            GC.Collect();
        }

        private void Recieve(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (socket.Connected)
                {
                    Command cmd = (Command)formatter.Deserialize(networkStream);
                    networkStream.Flush();
                    OnCommandRecieved(new CommandEventArgs(cmd));
                }
                Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (signingOut == false)
                {
                    Disconnect();
                    OnConnectionLost(new EventArgs());
                }
            }
        }

        private void bgConnector_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((bool)e.Result)
            {
                OnConnectionSuccessful(new EventArgs());
            }
            else
            {
                OnConnectionUnsuccessful(new EventArgs());
            }

            ((BackgroundWorker)sender).Dispose();
            GC.Collect();
        }

        public void RequestClientList()
        {
            Command cmd = new Command(CommandType.ClientListRequest, ServerIP);
            cmd.TargetPort = ServerPort;
            cmd.SenderIP = IP;
            cmd.SenderName = Username;
            cmd.SenderPort = Port;
            SendCommand(cmd);
        }

        public void SignOut()
        {
            Command cmd = new Command(CommandType.UserDisconnectRequest, ServerIP);
            cmd.TargetPort = ServerPort;
            cmd.SenderIP = IP;
            cmd.SenderName = Username;
            cmd.SenderPort = Port;
            SendCommand(cmd);
            signingOut = true;
        }

        public bool Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    bgReciever.CancelAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
                return true;
        }

        //Event Handlers
        protected virtual void OnConnectionLost(EventArgs e)
        {
            if (ConnectionLost != null)
                ConnectionLost(this, e);
        }

        protected virtual void OnCommandRecieved(CommandEventArgs e)
        {
            if (CommandRecieved != null)
                CommandRecieved(this, e);
        }

        protected virtual void OnConnectionSuccessful(EventArgs e)
        {
            if (ConnectionSuccessful != null)
                ConnectionSuccessful(this, e);
        }

        protected virtual void OnConnectionUnsuccessful(EventArgs e)
        {
            if (ConnectionUnsuccessful != null)
                ConnectionUnsuccessful(this, e);
        }
    }
}
