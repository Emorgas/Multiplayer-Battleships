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
        private byte[] senderIPAddressBytes = {0,0,0,0};
        private byte[] targetIPAddressBytes = {0,0,0,0};
        // IPAddress.Broadcast -> Error on Linux/Mono .Error sending data to server, Error: Type 'System.Net.IPAddress+ReadOnlyIPAddress' in Assembly 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' is not marked as serializable.
        public static IPAddress BroadcastAddress = new IPAddress(new byte[] { 255, 255, 255, 255 });
        //Properties
        public CommandType CommandType
        {
            get { return cmdType; }
            set { cmdType = value; }
        }
        
        public IPAddress SenderIP
        {
            get {
                // return senderIP;
                return new IPAddress(senderIPAddressBytes);
            }
            set {
                senderIP = value; 
                senderIPAddressBytes = value.GetAddressBytes();
            }
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
            get {
                // return targetIP; 
                return new IPAddress(targetIPAddressBytes);
            }
            set {
                targetIP = value;
                targetIPAddressBytes = value.GetAddressBytes();
            }
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
            targetIPAddressBytes = target.GetAddressBytes();
            data = cmdData;
        }
    }
}
