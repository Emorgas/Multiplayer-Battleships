using System;
using System.Net;
using System.Net.Sockets;

namespace CommandUtils
{
    public delegate void CommandRecievedEventHandler(object sender, CommandEventArgs e);
    public delegate void DisconnectedEventHandler(object sender, DisconnectEventArgs e);
    public delegate void SuccessfulConnectionEventHandler(object sender, EventArgs e);
    public delegate void UnsuccsessfulConnectionEventHandler(object sender, EventArgs e);
    public delegate void ServerConnectionLostEventHandler(object sender, EventArgs e);
    
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

    public class DisconnectEventArgs : EventArgs
    {
        private Command command;

        public Command Command
        {
            get { return command; }
        }

        public DisconnectEventArgs(Command cmd)
        {
            command = cmd;
        }
    }
}
