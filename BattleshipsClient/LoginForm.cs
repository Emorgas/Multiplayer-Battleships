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
                    MessageBox.Show(i18n.GetText("userNameInUse"), i18n.GetText("invalidUsernameTitle"), MessageBoxButtons.OK);
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
            MessageBox.Show(i18n.GetText("serverConnectionFailed"),i18n.GetText("servernoresponse"), MessageBoxButtons.OK);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            this.Text = i18n.GetText("loginFormTitle");
            label1.Text = i18n.GetText("labelUsername");
            label2.Text = i18n.GetText("labelServerIP");
            label3.Text= i18n.GetText("labelServerport");
            btnLogin.Text=i18n.GetText("login");
            btnQuit.Text = i18n.GetText("quit");
        }


        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtServerIP.Text.Trim() == "")
            {
                MessageBox.Show(i18n.GetText("FieldBlank", i18n.GetText("labelServerIP")), i18n.GetText("invalidIP"), MessageBoxButtons.OK);
            }
            else if (txtServerPort.Text.Trim() == "")
            {
                MessageBox.Show(i18n.GetText("FieldBlank", i18n.GetText("labelServerport")), i18n.GetText("invalidPort"), MessageBoxButtons.OK);
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
                MessageBox.Show(i18n.GetText("FieldBlank", i18n.GetText("labelUsername")), i18n.GetText("invalidusername"), MessageBoxButtons.OK);
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
