using System;
using System.Net;
using System.Windows.Forms;

using CommandUtils;

namespace BattleshipsClient
{
    public partial class LoginForm : Form
    {
        private LocalClient client;
        ChatForm chat;
        public LoginForm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            AcceptButton = btnLogin;
        }

        private void CommandRecieved(object sender, CommandEventArgs e)
        {
            if (e.Command.CommandType == CommandType.UserDataInform)
            {
                client.Wins = int.Parse(e.Command.Data.Split(':')[0]);
                client.Losses = int.Parse(e.Command.Data.Split(':')[1]);
            }
            if (e.Command.CommandType == CommandType.UsernameRequest)
            {
                if (e.Command.Data.ToLower() == "false")
                {
                    client.SignOut();
                    MessageBox.Show("Username already in use!", "Invalid Username", MessageBoxButtons.OK);
                    client.Disconnect();

                }
                else if (e.Command.Data.ToLower() == "true")
                {
                    client.CommandRecieved -= CommandRecieved;
                    if (InvokeRequired)
                    {
                        BeginInvoke(new MethodInvoker(delegate
                      {
                          chat = new ChatForm(ref client, this);
                          chat.Show();
                          Hide();
                      }));
                    }
                    else
                    {
                        chat = new ChatForm(ref client, this);
                        chat.Show();
                        Hide();
                    }
                }
            }
        }

        private void ConnectionSuccessful(object sender, EventArgs e)
        {

        }

        private void ConnectionUnsuccessful(object sender, EventArgs e)
        {
            MessageBox.Show("Connection Failed, please check server details and ensure server is running correctly", "No Response From Server", MessageBoxButtons.OK);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }


        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtServerIP.Text.Trim() == "")
            {
                MessageBox.Show("IP field is blank!", "Invalid IP", MessageBoxButtons.OK);
            }
            else if (txtServerPort.Text.Trim() == "")
            {
                MessageBox.Show("Port field is blank!", "Invalid Port", MessageBoxButtons.OK);
            }
            else
            {
                client = new LocalClient(IPAddress.Parse(txtServerIP.Text.Trim()), Int32.Parse(txtServerPort.Text), "N/A");
                client.CommandRecieved += new CommandRecievedEventHandler(CommandRecieved);
                client.ConnectionSuccessful += new SuccessfulConnectionEventHandler(ConnectionSuccessful);
                client.ConnectionUnsuccessful += new UnsuccsessfulConnectionEventHandler(ConnectionUnsuccessful);
                LoginToServer();
            }
        }

        private void LoginToServer()
        {
            if (txtUsername.Text.Trim() == "")
            {
                MessageBox.Show("Username field is blank!", "Invalid Username", MessageBoxButtons.OK);
            }
            else
            {
                client.Username = txtUsername.Text.Trim();
                client.ConnectToServer();

            }
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
