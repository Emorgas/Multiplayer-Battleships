﻿using System;
//using System.Data;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using CommandUtils;
namespace BattleshipsClient
{
    public partial class ChatForm : Form
    {
        private LocalClient client;
        private LoginForm parent;
        private List<string> clientList;
        private bool quitApplication = true;
        private bool activeChallenge = false;
        private int activeGameID = -1;
        private BattleshipGameForm gameForm;
        public ChatForm(ref LocalClient client, LoginForm parent)
        {
            InitializeComponent();

            AcceptButton = btnSend;

            this.parent = parent;
            clientList = new List<string>();
            this.client = client;
            Text += " - " + client.Username;
            this.client.CommandRecieved += new CommandRecievedEventHandler(CommandRecieved);
            this.client.ConnectionLost += new ServerConnectionLostEventHandler(ConnectionLost);
            this.client.RequestClientList();
            lblWins.Text = "Your Wins: " + this.client.Wins;
            lblLosses.Text = "Your Losses: " + this.client.Losses;
        }

        private void CommandRecieved(object sender, CommandEventArgs e)
        {
            if (e.Command.SenderName != client.Username)
            {
                //Recieving a chat message
                if (e.Command.CommandType == CommandType.Message)
                {
                    rtbChat.AppendText(e.Command.SenderName.ToString() + ": " + e.Command.Data);
                }
                //Notify of user connecting
                if (e.Command.CommandType == CommandType.UserConnected)
                {
                    clientList.Add(e.Command.Data);
                    string username = e.Command.Data.Split(':')[2];
                    rtbChat.AppendText(username + " has connected." + Environment.NewLine);
                    lstUsers.Items.Add(username);
                }
                //Notify of user disconnecting
                if (e.Command.CommandType == CommandType.UserDisconnected)
                {
                    string username = e.Command.Data.Split(':')[2];
                    for (int i = 0; i < lstUsers.Items.Count; i++)
                    {
                        if ((string)lstUsers.Items[i] == username)
                        {
                            lstUsers.Items.RemoveAt(i);
                            break;
                        }
                    }
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        if (clientList[i] == e.Command.Data)
                        {
                            clientList.RemoveAt(i);
                        }
                    }
                    rtbChat.AppendText(username + " has disconnected." + Environment.NewLine);
                }
            }
            //Update client list when recieved - Outside self message check to enable server to inform client of it's existence
            if (e.Command.CommandType == CommandType.ClientListRequest)
            {
                string[] clients = Array.ConvertAll(e.Command.Data.Split(','), p => p.Trim());
                lstUsers.Items.Clear();
                for (int i = 0; i < clients.Length; i++)
                {
                    if (clients[i] != "")
                    {
                        string username = clients[i].Split(':')[2];
                        lstUsers.Items.Add(username);
                        clientList.Add(clients[i]);
                    }
                }
            }

            if (e.Command.CommandType == CommandType.ChallengeRequest)
            {
                if (activeChallenge == false)
                {
                    string data = e.Command.Data;
                    DialogResult dr = MessageBox.Show(e.Command.SenderName + " has challenged you!" + Environment.NewLine 
                        + "Wins: " + int.Parse(data.Split(':')[0]) + Environment.NewLine
                        + "Losses: " + int.Parse(data.Split(':')[1]) + Environment.NewLine
                        + "Do You accept?", "You have been challenged!", MessageBoxButtons.YesNo);
                    if (dr == DialogResult.Yes)
                    {
                        activeChallenge = true;
                        //Accept the request
                        Command cmd = new Command(CommandType.ChallengeResponse, e.Command.SenderIP, "true");
                        cmd.TargetPort = e.Command.SenderPort;
                        cmd.SenderIP = client.IP;
                        cmd.SenderName = client.Username;
                        cmd.SenderPort = client.Port;
                        client.SendCommand(cmd);
                    }
                    else
                    {
                        //Reject the request
                        Command cmd = new Command(CommandType.ChallengeResponse, e.Command.SenderIP, "false");
                        cmd.TargetPort = e.Command.SenderPort;
                        cmd.SenderIP = client.IP;
                        cmd.SenderName = client.Username;
                        cmd.SenderPort = client.Port;
                        client.SendCommand(cmd);
                    }
                }
            }

            if (e.Command.CommandType == CommandType.ChallengeResponse)
            {
                if (e.Command.Data.ToString().ToLower() == "true")
                {
                    //Challenge Accepted
                    Command cmd = new Command(CommandType.GameStartRequest, client.ServerIP, e.Command.SenderIP + ":" + e.Command.SenderPort);
                    cmd.TargetPort = client.ServerPort;
                    cmd.SenderIP = client.IP;
                    cmd.SenderPort = client.Port;
                    cmd.SenderName = client.Username;
                    client.SendCommand(cmd);
                }
                else
                {
                    //challenge Rejected
                    activeChallenge = false;
                    rtbChat.AppendText("Your game challenge was rejected." + Environment.NewLine);
                }
            }

            if (e.Command.CommandType == CommandType.GameIDInform)
            {
                activeGameID = int.Parse(e.Command.Data);
                if (InvokeRequired)
                {
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        gameForm = new BattleshipGameForm(ref client, activeGameID);
                        gameForm.Show();
                        gameForm.FormClosed += new FormClosedEventHandler(GameForm_Closed);
                    }));
                }
                else
                {
                    gameForm = new BattleshipGameForm(ref client, activeGameID);
                    gameForm.Show();
                    gameForm.FormClosed += new FormClosedEventHandler(GameForm_Closed);

                }
            }

            if (e.Command.CommandType == CommandType.UserDataInform)
            {
                ConfirmChallenge(e.Command);
            }
        }

        private void GameForm_Closed(object sender, EventArgs e)
        {
            activeChallenge = false;
            activeGameID = -1;
            lblWins.Text = "Your Wins: " + client.Wins;
            lblLosses.Text = "Your Losses: " + client.Losses;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "")
            {
                Command cmd = new Command(CommandType.Message, IPAddress.Broadcast, txtMessage.Text + Environment.NewLine);
                cmd.SenderIP = client.IP;
                cmd.SenderName = client.Username;
                cmd.SenderPort = client.Port;
                client.SendCommand(cmd);
                rtbChat.AppendText(client.Username + ": " + txtMessage.Text + Environment.NewLine);

                txtMessage.Text = "";
            }
        }

        private void btnChallenge_Click(object sender, EventArgs e)
        {
            if (activeChallenge == false)
            {
                int itemIndex = lstUsers.SelectedIndex;
                string username = lstUsers.Items[itemIndex].ToString();
                if (username != client.Username)
                {
                    IPAddress targetIP;
                    int targetPort;
                    int index = FindClientByUsername(username);
                    if (index == -1)
                    {
                        MessageBox.Show("User could not be found! Refreshing user list...", "Error - User Not Found!", MessageBoxButtons.OK);
                        client.RequestClientList();
                        return;
                    }
                    else
                    {
                        targetIP = IPAddress.Parse(clientList[index].Split(':')[0]);
                        targetPort = int.Parse(clientList[index].Split(':')[1]);
                        Command cmd = new Command(CommandType.UserDataRequest, targetIP);
                        cmd.TargetPort = targetPort;
                        cmd.SenderIP = client.IP;
                        cmd.SenderPort = client.Port;
                        cmd.SenderName = client.Username;
                        client.SendCommand(cmd);
                    }
                }
            }
        }

        private void ConfirmChallenge(Command cmd)
        {
            string data = cmd.Data;
            int wins = int.Parse(data.Split(':')[0]);
            int losses = int.Parse(data.Split(':')[1]);
            DialogResult dr = MessageBox.Show(cmd.SenderName + Environment.NewLine + "Wins: " + wins + Environment.NewLine + "Losses: " + losses + Environment.NewLine + "Challenge this user?", "Challenge a User", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                Command responseCmd = new Command(CommandType.ChallengeRequest, cmd.SenderIP);
                responseCmd.TargetPort = cmd.SenderPort;
                responseCmd.SenderName = client.Username;
                responseCmd.SenderIP = client.IP;
                responseCmd.SenderPort = client.Port;
                client.SendCommand(responseCmd);
                activeChallenge = true;
                rtbChat.AppendText("Challenge request sent to " + cmd.SenderName + "." + Environment.NewLine + "Waiting for a response..." + Environment.NewLine);
            }
            else
            {
                rtbChat.AppendText("Challenge aborted." + Environment.NewLine);
            }
        }

        private void ConnectionLost(object sender, EventArgs e)
        {
            MessageBox.Show("Connection to server lost. Signing out...", "Connection Lost!", MessageBoxButtons.OK);
            quitApplication = false;
            Close();
        }

        private int FindClientByUsername(string username)
        {
            int index = 0;
            foreach (string s in clientList)
            {
                if (s.Split(':')[2].Trim() == username.Trim())
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.SignOut();
            Thread.Sleep(1000);             //Delay ensures message is completely sent before exiting program
            client.Disconnect();
        }

        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (quitApplication)
            {
                Application.Exit();
            }
            else
            {
                parent.Show();
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            quitApplication = true;
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            quitApplication = false;
            Close();
        }
    }
}
