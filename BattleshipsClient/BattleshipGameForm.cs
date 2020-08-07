using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using CommandUtils;
namespace BattleshipsClient
{
    public partial class BattleshipGameForm : Form
    {
        private string introString;
        private ShipType selectedShipType = ShipType.Default;
        private GridPosition[,] shipPositions;
        private bool shipIsHorizontal;
        private PictureBox pictureBox;
        private LocalClient client;
        private int gameID;
        private bool myTurn = false;
        private GridPosition gridTarget;

        //Ships
        private Ship destroyer;
        private Ship cruiser;
        private Ship submarine;
        private Ship battleship;
        private Ship carrier;

        public BattleshipGameForm(ref LocalClient client, int ID)
        {
            InitializeComponent();
            Text += " - " + client.Username;
            InitGrid(EnemyGrid);
            AssignGridClickEvent(EnemyGrid, false);
            InitGrid(PlayerGrid);
            AssignGridClickEvent(PlayerGrid, true);
            shipPositions = new GridPosition[5, 2];
            this.client = client;
            this.client.CommandRecieved += new CommandRecievedEventHandler(GameCommandRecieved);
            gameID = ID;
            gridTarget = new GridPosition(-1, -1);
        }

        public void GameCommandRecieved(object sender, CommandEventArgs e)
        {
            Console.WriteLine("Game command received. data:{0} type:{1}", e.Command.Data, e.Command.CommandType);
            if (e.Command.CommandType == CommandType.GameStartInform)
            {
                if (e.Command.Data.ToLower() == "true")
                {
                    myTurn = true;
                    rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("firstShotMe")); ; });
                    //btnFire.Enabled = true;
                }
                else if (e.Command.Data.ToLower() == "false")
                {
                    myTurn = false;
                    btnFire.Enabled = false;
                    rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("firstShotEnemy")); ; });

                }
            }
            if (e.Command.CommandType == CommandType.GameShotResult)
            {
                if (e.Command.Data.ToLower().Equals("hit"))
                {
                    //Deal with hits
                    rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shotHit")); ; });
                    pictureBox = (PictureBox)EnemyGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                    pictureBox.Image = Properties.Resources.ShipHit;
                    pictureBox.Tag = "ShipHit";
                    btnFire.Enabled = false;
                    myTurn = false;
                    rtbLog.BeginInvoke((MethodInvoker)delegate () {rtbLog.AppendText(i18n.GetText("waitEnemyShot")); ; });

                }
                else if (e.Command.Data.ToLower().Equals("miss"))
                {
                    //deal with misses
                    rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shotMissed")); ; });
                    pictureBox = (PictureBox)EnemyGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                    pictureBox.Image = Properties.Resources.WaterMiss;
                    pictureBox.Tag = "WaterMiss";
                    btnFire.Enabled = false;
                    myTurn = false;
                    rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("waitEnemyShot")); ; });

                }
            }
            if (e.Command.CommandType == CommandType.GameHitInform)
            {
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("oneShipHit")); ; });
                gridTarget.x = int.Parse(e.Command.Data.Split(',')[0]);
                gridTarget.y = int.Parse(e.Command.Data.Split(',')[1]);
                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                pictureBox.Image = Properties.Resources.ShipHit;
                myTurn = true;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("turnToShot")); ; });

            }
            if (e.Command.CommandType == CommandType.GameMissInform)
            {
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("opponentMissedFleet")); ; });
                gridTarget.x = int.Parse(e.Command.Data.Split(',')[0]);
                gridTarget.y = int.Parse(e.Command.Data.Split(',')[1]);
                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                pictureBox.Image = Properties.Resources.WaterMiss;
                myTurn = true;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("turnToShot")); ; });
            }
            if (e.Command.CommandType == CommandType.GameOverInform)
            {
                if (e.Command.Data.ToLower() == "win")
                {
                    Command cmdInform = new Command(CommandType.GameOverInform, client.ServerIP, gameID + ":" + "win");
                    cmdInform.TargetPort = client.ServerPort;
                    cmdInform.SenderIP = client.IP;
                    cmdInform.SenderPort = client.Port;
                    cmdInform.SenderName = client.Username;
                    client.Wins++;
                    client.SendCommand(cmdInform);
                    MessageBox.Show(i18n.GetText("wonCongratulations", client.Username) , i18n.GetText("winner"), MessageBoxButtons.OK);
                    Close();
                }
                else if (e.Command.Data.ToLower() == "loss")
                {
                    Command cmdInform = new Command(CommandType.GameOverInform, client.ServerIP, gameID + ":" + "loss");
                    cmdInform.TargetPort = client.ServerPort;
                    cmdInform.SenderIP = client.IP;
                    cmdInform.SenderPort = client.Port;
                    cmdInform.SenderName = client.Username;
                    client.Losses++;
                    client.SendCommand(cmdInform);
                    MessageBox.Show(i18n.GetText("lossGame", client.Username), i18n.GetText("gameLost"), MessageBoxButtons.OK);
                    Close();

                }
            }
        }

        private void BattleshipGameForm_Load(object sender, EventArgs e)
        {
            rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("welcomeMessage")); ; });
            rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("placementDescription")); ; });
            introString = rtbLog.Text;
            // Load Control Text
            btnDestroyer.Text = i18n.GetText("shipName1");
            btnCruiser.Text = i18n.GetText("shipName2");
            btnSubmarine.Text = i18n.GetText("shipName3");
            btnBattleship.Text= i18n.GetText("shipName4");
            btnCarrier.Text= i18n.GetText("shipName5");
            btnReset.Text = i18n.GetText("buttonReset");
            btnSubmit.Text= i18n.GetText("buttonSubmit");
            btnFire.Text= i18n.GetText("buttonFire");
            surrenderToolStripMenuItem.Text= i18n.GetText("toolMenuSurrender");
            label42.Text= i18n.GetText("EnemyWater");
            label41.Text= i18n.GetText("FriendlyWater");
            label43.Text = i18n.GetText("gameLog");
            this.Text = i18n.GetText("formBattleShip");
        }

        private void InitGrid(TableLayoutPanel grid)
        {
            for (int c = 0; c < grid.ColumnCount; c++)
            {
                for (int r = 0; r < grid.RowCount; r++)
                { 
                    pictureBox = new PictureBox();
                    pictureBox.Visible = true;
                    pictureBox.Dock = DockStyle.Fill;
                    pictureBox.Image = Properties.Resources.Water;
                    pictureBox.Margin = new Padding(0);
                    pictureBox.Tag = "Water";
                    grid.Controls.Add(pictureBox, c, r);
                }
            }
        }

        private void ResetGrid(TableLayoutPanel grid)
        {
            for (int c = 0; c < grid.ColumnCount; c++)
            {
                for (int r = 0; r < grid.RowCount; r++)
                {
                    pictureBox = (PictureBox)grid.GetControlFromPosition(c, r);
                    pictureBox.Image = Properties.Resources.Water;
                    pictureBox.Tag = "Water";
                }
            }
        }

        private void AssignGridClickEvent(TableLayoutPanel grid, bool playerGrid)
        {
            foreach (Control control in grid.Controls.OfType<PictureBox>())
            {
                if (playerGrid)
                {
                    control.Click += new EventHandler(PlayerGrid_Click);
                }
                else
                {
                    control.Click += new EventHandler(EnemyGrid_Click);
                }
            }
        }

        private void EnemyGrid_Click(object sender, EventArgs e)
        {
            if (myTurn)
            {
                Control control = (Control)sender;
                if (gridTarget.x > -1 && gridTarget.y > -1)
                {
                    if (pictureBox != null)
                    {
                        if (pictureBox.Tag.ToString() == "WaterTarget")
                        {
                            pictureBox = (PictureBox)EnemyGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                            pictureBox.Image = Properties.Resources.Water;
                            pictureBox.Tag = "Water";
                        }
                    }
                }
                gridTarget = new GridPosition(EnemyGrid.GetPositionFromControl(control).Column, EnemyGrid.GetPositionFromControl(control).Row);
                pictureBox = (PictureBox)EnemyGrid.GetControlFromPosition(gridTarget.x, gridTarget.y);
                if (pictureBox.Tag.ToString() == "Water")
                {
                    pictureBox.Image = Properties.Resources.WaterTarget;
                    pictureBox.Tag = "WaterTarget";
                    btnFire.Enabled = true;
                }
                else
                {
                    btnFire.Enabled = false;
                }

            }
        }

        private void PlayerGrid_Click(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            switch (selectedShipType)
            {
                #region Destroyer
                case ShipType.Destroyer:
                    if (shipPositions[0, 0] == null)
                    {
                        shipPositions[0, 0] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);

                    }
                    else if (shipPositions[0, 1] == null)
                    {
                        shipPositions[0, 1] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                        if (PlaceShip(ShipType.Destroyer) == false)
                        {
                            MessageBox.Show(i18n.GetText("invalidPlacement", i18n.GetText("shipName1")), i18n.GetText("invalidPlacementTitle"), MessageBoxButtons.OK);
                            shipPositions[0, 0] = null;
                            shipPositions[0, 1] = null;
                        }
                        else
                        {
                            for (int i = 0; i < destroyer.occupiedSquares.Length; i++)
                            {
                                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(destroyer.occupiedSquares[i].x, destroyer.occupiedSquares[i].y);
                                pictureBox.Image = Properties.Resources.Ship;
                            }
                            btnDestroyer.Enabled = false;
                            btnDestroyer.Text = i18n.GetText("shipPlaced");
                        }
                    }
                    break;
                #endregion
                #region Cruiser
                case ShipType.Cruiser:
                    if (shipPositions[1, 0] == null)
                    {
                        shipPositions[1, 0] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                    }
                    else if (shipPositions[1, 1] == null)
                    {
                        shipPositions[1, 1] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                        if (PlaceShip(ShipType.Cruiser) == false)
                        {
                            MessageBox.Show(i18n.GetText("invalidPlacement", i18n.GetText("shipName2")), i18n.GetText("invalidPlacementTitle"), MessageBoxButtons.OK);
                            shipPositions[1, 0] = null;
                            shipPositions[1, 1] = null;
                        }
                        else
                        {
                            for (int i = 0; i < cruiser.occupiedSquares.Length; i++)
                            {
                                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(cruiser.occupiedSquares[i].x, cruiser.occupiedSquares[i].y);
                                pictureBox.Image = Properties.Resources.Ship;
                            }
                            btnCruiser.Enabled = false;
                            btnCruiser.Text = i18n.GetText("shipPlaced");
                        }
                    }
                    break;
                #endregion
                #region Submarine
                case ShipType.Submarine:
                    if (shipPositions[2, 0] == null)
                    {
                        shipPositions[2, 0] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                    }
                    else if (shipPositions[2, 1] == null)
                    {
                        shipPositions[2, 1] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                        if (PlaceShip(ShipType.Submarine) == false)
                        {
                            MessageBox.Show(i18n.GetText("invalidPlacement", i18n.GetText("shipName3")), i18n.GetText("invalidPlacementTitle"), MessageBoxButtons.OK);
                            shipPositions[2, 0] = null;
                            shipPositions[2, 1] = null;
                        }
                        else
                        {
                            for (int i = 0; i < submarine.occupiedSquares.Length; i++)
                            {
                                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(submarine.occupiedSquares[i].x, submarine.occupiedSquares[i].y);
                                pictureBox.Image = Properties.Resources.Ship;
                            }
                            btnSubmarine.Enabled = false;
                            btnSubmarine.Text = i18n.GetText("shipPlaced");
                        }
                    }
                    break;
                #endregion
                #region Battleship
                case ShipType.Battleship:
                    if (shipPositions[3, 0] == null)
                    {
                        shipPositions[3, 0] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                    }
                    else if (shipPositions[3, 1] == null)
                    {
                        shipPositions[3, 1] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                        if (PlaceShip(ShipType.Battleship) == false)
                        {
                            MessageBox.Show(i18n.GetText("invalidPlacement", i18n.GetText("shipName4")), i18n.GetText("invalidPlacementTitle"), MessageBoxButtons.OK);
                            shipPositions[3, 0] = null;
                            shipPositions[3, 1] = null;
                        }
                        else
                        {
                            for (int i = 0; i < battleship.occupiedSquares.Length; i++)
                            {
                                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(battleship.occupiedSquares[i].x, battleship.occupiedSquares[i].y);
                                pictureBox.Image = Properties.Resources.Ship;
                            }
                            btnBattleship.Enabled = false;
                            btnBattleship.Text = i18n.GetText("shipPlaced");
                        }
                    }
                    break;
                #endregion
                #region Carrier
                case ShipType.Carrier:
                    if (shipPositions[4, 0] == null)
                    {
                        shipPositions[4, 0] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                    }
                    else if (shipPositions[4, 1] == null)
                    {
                        shipPositions[4, 1] = new GridPosition(PlayerGrid.GetPositionFromControl(control).Column, PlayerGrid.GetPositionFromControl(control).Row);
                        if (PlaceShip(ShipType.Carrier) == false)
                        {
                            MessageBox.Show(i18n.GetText("invalidPlacement", i18n.GetText("shipName5")), i18n.GetText("invalidPlacementTitle"), MessageBoxButtons.OK);
                            shipPositions[4, 0] = null;
                            shipPositions[4, 1] = null;
                        }
                        else
                        {
                            for (int i = 0; i < carrier.occupiedSquares.Length; i++)
                            {
                                pictureBox = (PictureBox)PlayerGrid.GetControlFromPosition(carrier.occupiedSquares[i].x, carrier.occupiedSquares[i].y);
                                pictureBox.Image = Properties.Resources.Ship;
                            }
                            btnCarrier.Enabled = false;
                            btnCarrier.Text = i18n.GetText("shipPlaced"); ;
                        }
                    }
                    break;
                    #endregion
            }
            if (carrier != null && battleship != null && submarine != null && cruiser != null && destroyer != null)
            {
                btnSubmit.Enabled = true;
            }
        }

        private bool PlaceShip(ShipType ship)
        {
            //Check that begin and end points of ship are different
            if (shipPositions[(int)ship, 0] == shipPositions[(int)ship, 1])
            {
                return false;
            }
            //check that selected points are unique
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    if ((int)ship != r && (shipPositions[(int)ship, 0] == shipPositions[r, c] || shipPositions[(int)ship, 1] == shipPositions[r, c]))
                    {
                        return false;
                    }
                }
            }
            //Check to ensure that ship is in a straight line
            if (shipPositions[(int)ship, 0].x == shipPositions[(int)ship, 1].x) //Ship is vertical
            {
                shipIsHorizontal = false;
            }
            else if (shipPositions[(int)ship, 0].y == shipPositions[(int)ship, 1].y) //Ship is horizontal
            {
                shipIsHorizontal = true;
            }
            else
                return false;

            switch (ship)
            {
                case ShipType.Destroyer:
                    if (shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].x - shipPositions[(int)ship, 1].x)) != 1)
                        {
                            return false;
                        }
                    }
                    else if (!shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].y - shipPositions[(int)ship, 1].y)) != 1)
                        {
                            return false;
                        }
                    }
                    destroyer = new Ship(ShipType.Destroyer, shipPositions[(int)ship, 0], shipPositions[(int)ship, 1], 2, shipIsHorizontal);
                    if (CheckForShipCollisions(destroyer) == true)
                        return false;
                    break;
                case ShipType.Cruiser:
                    if (shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].x - shipPositions[(int)ship, 1].x)) != 2)
                        {
                            return false;
                        }
                    }
                    else if (!shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].y - shipPositions[(int)ship, 1].y)) != 2)
                        {
                            return false;
                        }
                    }
                    cruiser = new Ship(ShipType.Cruiser, shipPositions[(int)ship, 0], shipPositions[(int)ship, 1], 3, shipIsHorizontal);
                    if (CheckForShipCollisions(cruiser) == true)
                        return false;
                    break;
                case ShipType.Submarine:
                    if (shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].x - shipPositions[(int)ship, 1].x)) != 2)
                        {
                            return false;
                        }
                    }
                    else if (!shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].y - shipPositions[(int)ship, 1].y)) != 2)
                        {
                            return false;
                        }
                    }
                    submarine = new Ship(ShipType.Submarine, shipPositions[(int)ship, 0], shipPositions[(int)ship, 1], 3, shipIsHorizontal);
                    if (CheckForShipCollisions(submarine) == true)
                        return false;
                    break;
                case ShipType.Battleship:
                    if (shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].x - shipPositions[(int)ship, 1].x)) != 3)
                        {
                            return false;
                        }
                    }
                    else if (!shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].y - shipPositions[(int)ship, 1].y)) != 3)
                        {
                            return false;
                        }
                    }
                    battleship = new Ship(ShipType.Battleship, shipPositions[(int)ship, 0], shipPositions[(int)ship, 1], 4, shipIsHorizontal);
                    if (CheckForShipCollisions(battleship) == true)
                        return false;
                    break;
                case ShipType.Carrier:
                    if (shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].x - shipPositions[(int)ship, 1].x)) != 4)
                        {
                            return false;
                        }
                    }
                    else if (!shipIsHorizontal)
                    {
                        if (Math.Abs((shipPositions[(int)ship, 0].y - shipPositions[(int)ship, 1].y)) != 4)
                        {
                            return false;
                        }
                    }
                    carrier = new Ship(ShipType.Carrier, shipPositions[(int)ship, 0], shipPositions[(int)ship, 1], 5, shipIsHorizontal);
                    if (CheckForShipCollisions(carrier) == true)
                        return false;
                    break;
            }
            return true;
        }

        private bool CheckForShipCollisions(Ship ship)
        {
            if (ship.type != ShipType.Destroyer && destroyer != null)
            {
                for (int o = 0; o < destroyer.occupiedSquares.Length; o++)
                {
                    for (int s = 0; s < ship.occupiedSquares.Length; s++)
                    {
                        if (ship.occupiedSquares[s].x == destroyer.occupiedSquares[o].x && ship.occupiedSquares[s].y == destroyer.occupiedSquares[o].y)
                        {
                            return true;
                        }
                    }
                }
            }
            if (ship.type != ShipType.Cruiser && cruiser != null)
            {
                for (int o = 0; o < cruiser.occupiedSquares.Length; o++)
                {
                    for (int s = 0; s < ship.occupiedSquares.Length; s++)
                    {
                        if (ship.occupiedSquares[s].x == cruiser.occupiedSquares[o].x && ship.occupiedSquares[s].y == cruiser.occupiedSquares[o].y)
                        {
                            return true;
                        }
                    }
                }
            }
            if (ship.type != ShipType.Submarine && submarine != null)
            {
                for (int o = 0; o < submarine.occupiedSquares.Length; o++)
                {
                    for (int s = 0; s < ship.occupiedSquares.Length; s++)
                    {
                        if (ship.occupiedSquares[s].x == submarine.occupiedSquares[o].x && ship.occupiedSquares[s].y == submarine.occupiedSquares[o].y)
                        {
                            return true;
                        }
                    }
                }
            }
            if (ship.type != ShipType.Battleship && battleship != null)
            {
                for (int o = 0; o < battleship.occupiedSquares.Length; o++)
                {
                    for (int s = 0; s < ship.occupiedSquares.Length; s++)
                    {
                        if (ship.occupiedSquares[s].x == battleship.occupiedSquares[o].x && ship.occupiedSquares[s].y == battleship.occupiedSquares[o].y)
                        {
                            return true;
                        }
                    }
                }
            }
            if (ship.type != ShipType.Carrier && carrier != null)
            {
                for (int o = 0; o < carrier.occupiedSquares.Length; o++)
                {
                    for (int s = 0; s < ship.occupiedSquares.Length; s++)
                    {
                        if (ship.occupiedSquares[s].x == carrier.occupiedSquares[o].x && ship.occupiedSquares[s].y == carrier.occupiedSquares[o].y)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void btnDestroyer_Click(object sender, EventArgs e)
        {
            if (selectedShipType != ShipType.Destroyer)
            {
                selectedShipType = ShipType.Destroyer;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shipSelected", i18n.GetText("shipName1"),"2")); ; });
            }
        }

        private void btnCruiser_Click(object sender, EventArgs e)
        {
            if (selectedShipType != ShipType.Cruiser)
            {
                selectedShipType = ShipType.Cruiser;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shipSelected", i18n.GetText("shipName2"),"3")); ; });
            }
        }

        private void btnSubmarine_Click(object sender, EventArgs e)
        {
            if (selectedShipType != ShipType.Submarine)
            {
                selectedShipType = ShipType.Submarine;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shipSelected", i18n.GetText("shipName3"),"3")); ; });
            }
        }

        private void btnBattleship_Click(object sender, EventArgs e)
        {
            if (selectedShipType != ShipType.Battleship)
            {
                selectedShipType = ShipType.Battleship;
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shipSelected", i18n.GetText("shipName4"),"4")); ; });
            }
        }

        private void btnCarrier_Click(object sender, EventArgs e)
        {
            if (selectedShipType != ShipType.Carrier)
            {
                selectedShipType = ShipType.Carrier;
                rtbLog.BeginInvoke((MethodInvoker)delegate () {  rtbLog.AppendText(i18n.GetText("shipSelected", i18n.GetText("shipName5"),"5")); ; });
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            rtbLog.Text = introString;
            ResetGrid(PlayerGrid);
            //Re-enable the buttons
            //Destroyer
            btnDestroyer.Enabled = true;
            btnDestroyer.Text = i18n.GetText("shipName1");
            btnDestroyer.Visible = true;
            //Cruiser
            btnCruiser.Enabled = true;
            btnCruiser.Text = i18n.GetText("shipName2"); 
            btnCruiser.Visible = true;
            //Submarine
            btnSubmarine.Enabled = true;
            btnSubmarine.Text = i18n.GetText("shipName3"); 
            btnSubmarine.Visible = true;
            //Battleship
            btnBattleship.Enabled = true;
            btnBattleship.Text = i18n.GetText("shipName4");
            btnBattleship.Visible = true;
            //Carrier
            btnCarrier.Enabled = true;
            btnCarrier.Text = i18n.GetText("shipName5");
            btnCarrier.Visible = true;

            btnSubmit.Enabled = false;

            selectedShipType = ShipType.Default;
            shipIsHorizontal = false;
            shipPositions = new GridPosition[5, 2];

            destroyer = null;
            cruiser = null;
            submarine = null;
            battleship = null;
            carrier = null;
        }

        private void rtbLog_TextChanged(object sender, EventArgs e)
        {
            rtbLog.ScrollToCaret();
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            string shipString = "";
            shipString = gameID.ToString() + ':';
            for (int s = 0; s < 5; s++)
            {
                shipString += shipPositions[s, 0].x + "," + shipPositions[s, 0].y + "." + shipPositions[s, 1].x + "," + shipPositions[s, 1].y + ";";
            }
            Command cmd = new Command(CommandType.GameShipRequest, client.ServerIP, shipString);
            cmd.TargetPort = client.ServerPort;
            cmd.SenderIP = client.IP;
            cmd.SenderPort = client.Port;
            cmd.SenderName = client.Username;
            client.SendCommand(cmd);
            rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shipPlacementSend")); ; });
            //Submit Button
            btnSubmit.Enabled = false;
            btnSubmit.Visible = false;
            //Destroyer
            btnDestroyer.Enabled = false;
            btnDestroyer.Visible = false;
            //Cruiser
            btnCruiser.Enabled = false;
            btnCruiser.Visible = false;
            //Submarine
            btnSubmarine.Enabled = false;
            btnSubmarine.Visible = false;
            //Battleship
            btnBattleship.Enabled = false;
            btnBattleship.Visible = false;
            //Carrier
            btnCarrier.Enabled = false;
            btnCarrier.Visible = false;
            //Reset Button
            btnReset.Enabled = false;
            btnReset.Visible = false;
            //Fire Button
            btnFire.Visible = true;
        }
        private string GetShotHumanReadablePosition(int x, int y)
        {
            return (System.Convert.ToChar(0x41 + y).ToString() + " " + (x + 1).ToString());
        }
        private void btnFire_Click(object sender, EventArgs e)
        {
            if (gridTarget.x > -1 && gridTarget.y > -1)
            {
                Command cmd = new Command(CommandType.GameShotRequest, client.ServerIP, gameID + ":" + gridTarget.x + "," + gridTarget.y);
                cmd.TargetPort = client.ServerPort;
                cmd.SenderIP = client.IP;
                cmd.SenderName = client.Username;
                cmd.SenderPort = client.Port;
                client.SendCommand(cmd);
                rtbLog.BeginInvoke((MethodInvoker)delegate () { rtbLog.AppendText(i18n.GetText("shotAt", GetShotHumanReadablePosition(gridTarget.x,gridTarget.y))); ; });
            }
        }

        private void BattleshipGameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.CommandRecieved -= GameCommandRecieved;
        }
    }
}
