using System;
using System.Net;

namespace CommandUtils
{
    [Serializable]
    public class Command
    {
        //Private Variables
        private CommandType cmdType;
        private IPAddress senderIP;
        private int senderPort;
        private string senderName;
        private IPAddress targetIP;
        private int targetPort;
        private string data;
        //Properties
        public CommandType CommandType
        {
            get { return cmdType; }
            set { cmdType = value; }
        }
        
        public IPAddress SenderIP
        {
            get { return senderIP; }
            set { senderIP = value; }
        }

        public int SenderPort
        {
            get { return senderPort; }
            set { senderPort = value; }
        }

        public string SenderName
        {
            get { return senderName; }
            set { senderName = value; }
        }

        public IPAddress TargetIP
        {
            get { return targetIP; }
            set { targetIP = value; }
        }

        public int TargetPort
        {
            get { return targetPort; }
            set { targetPort = value; }
        }

        public string Data
        {
            get { return data; }
            set { data = value; }
        }

        public Command()
        {

        }

        public Command(CommandType type, IPAddress target, string cmdData = "")
        {
            cmdType = type;
            targetIP = target;
            data = cmdData;
        }
    }
}
