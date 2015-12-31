using System;
using System.Net;
using System.Net.Sockets;

namespace CommandUtils
{
    public delegate void CommandRecievedEventHandler(object sender, CommandEventArgs e);
    public delegate void DisconnectedEventHandler(object sender, ClientEventArgs e);
    public delegate void SuccessfulConnectionEventHandler(object sender, EventArgs e);
    public delegate void UnsuccsessfulConnectionEventHandler(object sender, EventArgs e);
    
    public class CommandEventArgs : EventArgs
    {
        private Command cmd;
        public Command Command
        {
            get { return cmd; }
        }
        public CommandEventArgs(Command cmd)
        {
            this.cmd = cmd;
        }
    }

    public class ClientEventArgs : EventArgs
    {
        private Socket socket;
        public IPAddress IP
        {
            get { return ((IPEndPoint)socket.RemoteEndPoint).Address; }
        }
        public int Port
        {
            get { return ((IPEndPoint)socket.RemoteEndPoint).Port; }
        }

        public ClientEventArgs(Socket clientManagersocket)
        {
            socket = clientManagersocket;
        }
    }
}
