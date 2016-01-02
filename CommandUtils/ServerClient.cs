using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommandUtils
{
    public class ServerClient
    {
        //Client Event Handlers
        public event CommandRecievedEventHandler CommandRecieved;
        public event DisconnectedEventHandler Disconnected;
        //Client Variables
        private Socket socket;
        private string clientUsername;
        private int wins;
        private int losses;
        private NetworkStream networkStream;
        private BinaryFormatter formatter;
        private BackgroundWorker bgReciever;
        //Client Properties
        public IPAddress IP
        {
            get
            {
                if (socket != null)
                    return ((IPEndPoint)socket.RemoteEndPoint).Address;
                else
                    return IPAddress.None;
            }
        }
        public int Port
        {
            get
            {
                if (socket != null)
                    return ((IPEndPoint)socket.RemoteEndPoint).Port;
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
            get { return clientUsername; }
            set { clientUsername = value; }
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

        public ServerClient(Socket clientSocket)
        {
            socket = clientSocket;
            networkStream = new NetworkStream(socket);
            formatter = new BinaryFormatter();
            bgReciever = new BackgroundWorker();
            bgReciever.WorkerSupportsCancellation = true;
            bgReciever.DoWork += new DoWorkEventHandler(Recieve);
            bgReciever.RunWorkerAsync();
        }

        //Recieve and build a command to be processed
        private void Recieve(object sender, DoWorkEventArgs e)
        {
            while (socket.Connected)
            {
                try
                {
                    Command cmd;
                    cmd = (Command)formatter.Deserialize(networkStream);
                    networkStream.Flush();
                    Console.WriteLine(cmd.CommandType + " Command Recieved");
                    cmd.SenderIP = IP;
                    cmd.SenderPort = Port;
                    if (cmd.CommandType == CommandType.UserConnected)
                        cmd.SenderName = cmd.Data.Split(new char[] { ':' })[1];
                    else
                        cmd.SenderName = clientUsername;
                    OnCommandRecieved(new CommandEventArgs(cmd));
                }
                catch (Exception sockEx)
                {
                    Console.WriteLine("An error occurred involving client " + Username + " at: " + IP + ":" + Port + "." + Environment.NewLine
                        + "Exception Message: " + sockEx.Message + Environment.NewLine 
                        + "Disconnecting client...");
                    Command dcCmd = new Command(CommandType.UserDisconnectRequest, IPAddress.Broadcast);
                    dcCmd.SenderName = Username;
                    dcCmd.SenderIP = IP;
                    dcCmd.SenderPort = Port;
                    OnDisconnected(new DisconnectEventArgs(dcCmd));
                }
            }
            //this.OnDisconnected(new ClientEventArgs(this.socket));
        }

        public void SendCommand(Command cmd)
        {
            if (socket != null && socket.Connected)
            {
                BackgroundWorker bgSender = new BackgroundWorker();
                bgSender.DoWork += new DoWorkEventHandler(bgSenderDoWork);
                bgSender.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgSenderCompleted);
                bgSender.RunWorkerAsync(cmd);
            }
        }

        private void bgSenderCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ((BackgroundWorker)sender).Dispose();
            GC.Collect();
        }

        private void bgSenderDoWork(object sender, DoWorkEventArgs e)
        {
            Command cmd = (Command)e.Argument;
            e.Result = SendCommandToTarget(cmd);
        }

        System.Threading.Semaphore semaphor = new System.Threading.Semaphore(1, 1);
        private bool SendCommandToTarget(Command cmd)
        {
            try
            {
                semaphor.WaitOne();
                formatter.Serialize(networkStream, cmd);
                semaphor.Release();
                return true;
            }
            catch
            {
                Console.WriteLine("Error Sending Data to client {0}:{1}", cmd.TargetIP, cmd.TargetPort);
                semaphor.Release();
                return false;
            }
        }

        public bool Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                try
                {
                    bgReciever.CancelAsync();
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Disconnection Exception Thrown: " + ex.Message);
                    if (ex.InnerException != null)
                        Console.WriteLine("Disconnection Inner Exception : " + ex.InnerException.Message);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        //Event Handlers
        protected virtual void OnDisconnected(DisconnectEventArgs e)
        {
            if (Disconnected != null)
                Disconnected(this, e);
        }
        protected virtual void OnCommandRecieved(CommandEventArgs e)
        {
            if (CommandRecieved != null)
                CommandRecieved(this, e);
        }
    }
}
