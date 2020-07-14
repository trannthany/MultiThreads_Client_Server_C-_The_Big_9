using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CardLib2;
namespace Project_MultiThreads_Client
{
    public partial class Form1 : Form
    {
        #region Fields
        private IPAddress _serverIPAddress;
        private int _port;
        private TcpClient _client;
        private const string LOCALHOST = "127.0.0.1";
        private const int DEFAULT_PORT = 8800;
        IFormatter bfmt;
        NetworkStream myStream;
        PictureBox[] pBoxes;
        PictureBox[] opBoxes;
        int roundCount = 0;
        int timerCount = 0;
        bool done;
        int opponentPoints = 0;
        int myPoints = 0;
        int biddingPool = 0;
        private const int initialBid = 25; 

        Player myPlayer;
        PlayingCard[] opponentHands;
        #endregion
        public Form1()
        {
            InitializeComponent();
            _richTextBox.ScrollToCaret();
            bfmt = new BinaryFormatter();
            _serverIPAddress = _getIPAddress(_ipAddressTextBox.Text);
            _port = _getPort(_portTextBox.Text);
            _player1PictureBox1.Image = imageList.Images[56];
            _player1PictureBox2.Image = imageList.Images[56];
            _opponentPictureBox1.Image = imageList.Images[56];
            _opponentPictureBox2.Image = imageList.Images[56];
            pBoxes = new PictureBox[] { _player1PictureBox1, _player1PictureBox2 };
            opBoxes = new PictureBox[] { _opponentPictureBox1, _opponentPictureBox2 };
            _player2MoneyPictureBox.Image = moneyImageList.Images[2];
            _player2MoneyPictureBox.Visible = false;
            _poolMoneyPictureBox.Image = moneyImageList.Images[3];
            _poolMoneyPictureBox.Visible = false;

            _connectButton.Enabled = true;
            _disconnectButton.Enabled = false;
            _sendMessageButton.Enabled = false;
            _setNameButton.Enabled = false;
        }
        #region Buttons

        private void _disconnectButton_Click(object sender, EventArgs e)
        {
            _disconnectFromServer();
        }

        private void _connectButton_Click(object sender, EventArgs e)
        {
            //isConneected = true;
            try
            {
               
                _client = new TcpClient(_getIPAddress(_ipAddressTextBox.Text).ToString(), _getPort(_portTextBox.Text));
                Thread t = new Thread(_processClientTransactions);
                t.IsBackground = true;
                t.Start(_client);
                _connectButton.Enabled = false;
                _disconnectButton.Enabled = true;
                // _sendMessageButton.Enabled = true;
                _setNameButton.Enabled = true;

            }
            catch (Exception ex)
            {
                _richTextBox.AppendText("problem connecting to server !!!\n");
                _richTextBox.AppendText($"{ex.ToString()}\n");//this is good for an error log for showing to the user is not necessary
            }
        }

        #endregion

        #region SupportMethods

        
        private void _processClientTransactions(object tcpClient)
        {
            try
            {
                _updateStatusBox("You've connected to the Server !!!\n");
                TcpClient client = (TcpClient)tcpClient;
                myStream = client.GetStream();
                int round = 1;
                done = false;
                while (!done)
                {
                    if (myStream.DataAvailable)
                    {
                        object obj = bfmt.Deserialize(myStream);

                        if (obj is PlayingCard)
                        {
                            _setMoneyPictureBoxVisible(_poolMoneyPictureBox, true);
                            
                            PlayingCard card = obj as PlayingCard;
                            if (roundCount % 2 == 0)
                            {
                                _enableButton(_foldButton, true);
                                _enableButton(_bidButton, true);
                                myPlayer.MyHand[0] = card;
                                _player1PictureBox1.Image = imageList.Images[card.ID];
                            }
                            else
                            {
                                myPlayer.MyHand[1] = card;
                                _player1PictureBox2.Image = imageList.Images[card.ID];
                                _updateStatusBox($"Round: {round}: {myPlayer.MyHand[0].Rank}, {myPlayer.MyHand[1].Rank}\n");
                                _updateBalance(initialBid, true);
                                bfmt.Serialize(myStream, initialBid);
                                round++;
                            }
                            roundCount++;
                          //  biddingPool = 0;
                            _updatePoolMoneyLabel(50);
                            //round 0
                        }
                        else if (obj is PlayingCard[])
                        {
                            opponentHands = obj as PlayingCard[];
                            if (myPlayer.MyHand.Length > 0)
                            {
                                for (int i = 0; i < opponentHands.Length; i++)
                                {
                                    opBoxes[i].Image = imageList.Images[opponentHands[i].ID];
                                }
                            }
                            
                            _checkWinCondition();
                            runTimer();
                           
                            myPlayer.MyStatus = Status.Unassigned;
                            
                        }
                        else if (obj is ReadyStatus)
                        {
                            ReadyStatus rs = (ReadyStatus)obj;
                            if (rs == ReadyStatus.Ready)
                            {
                                bfmt.Serialize(myStream, myPlayer.MyHand);
                            }
                          
                        }
                        else if (obj is GameMessage)
                        {
                            GameMessage gm = obj as GameMessage;
                            _updateStatusBox(gm.Message);
                        }
                        else if (obj is Status)
                        {
                           
                            Status status = (Status)obj;
                            if (status == Status.Fold)
                            {
                                opponentPoints = -1;
                                bfmt.Serialize(myStream, Status.Bid);
                            }
                           if(status == Status.Bid)                               
                                _setMoneyPictureBoxVisible(_player2MoneyPictureBox, true);
                        }
                        else if (obj is int)
                        {
                           
                            biddingPool = (int)obj;
                            _updatePoolMoneyLabel(biddingPool);

                        }
                        else if (obj is StartNewRound)
                        {
                            bfmt.Serialize(myStream, new StartNewRound());
                            _updatePoolMoneyLabel(biddingPool);
                        }
                        else if (obj is QuitGame) 
                        {
                            _disconnectFromServer();
                        }
                    }
                }
                _client.Close();
                _updateStatusBox($"You've disconnected from the server !!!\n");
            }
            catch (Exception ex)
            {
                _updateStatusBox($"problem communicating with the server. Connection may have been intentionally disconnected.\n");
                _client.Close();
                _updateStatusBox($"{ex.ToString()}\n");//this is good for an error log for showing to the user is not necessary
            }
            // _connectButton.Enabled = true;
            _enableButton(_connectButton,true);
            // _disconnectButton.Enabled = false;
            _enableButton(_disconnectButton, false);
            // _sendMessageButton.Enabled = false;
            _enableButton(_sendMessageButton, false);

            _enableButton(_setNameButton, false);
        }//End processClientTransactions

        delegate void _updatePoolMoneyLabelDel(int money);
        private void _updatePoolMoneyLabel(int money) 
        {
            if (InvokeRequired)
            {
                _updatePoolMoneyLabelDel del = _updatePoolMoneyLabel;
                this.Invoke(del, money);
            }
            else 
            {
                _poolMoneyLabel.Text = $"Pool Money: ${money}"; 
            }
        }

        #region Delegate Buttons
        delegate void _enableConnectButtonDel(bool b);
        public void _enableConnectButton(bool b)
        {
            if (InvokeRequired)
            {
                _enableConnectButtonDel con = _enableConnectButton;
                _connectButton.Invoke(con, b);
            }
            else
            {
                _connectButton.Enabled = b;
            }
        }

        delegate void _enableDisconnectButtonDel(bool b);
        public void _enableDisconnectButton(bool b)
        {
            if (InvokeRequired)
            {
                _enableDisconnectButtonDel con = _enableDisconnectButton;
                _disconnectButton.Invoke(con, b);
            }
            else
            {

                _disconnectButton.Enabled = b;

            }
        }

        delegate void _enableButtonDel(Button btn,bool b);
        public void _enableButton(Button btn,bool b)
        {
            if (InvokeRequired)
            {
                _enableButtonDel con = _enableButton;
                this.Invoke(con,btn, b);
            }
            else
            {
                btn.Enabled = b;
            }
        }

        delegate void _enableSendButtonDel(bool b);
        public void _enableSendButton(bool b)
        {
            if (InvokeRequired)
            {
                _enableSendButtonDel con = _enableSendButton;
                _sendMessageButton.Invoke(con, b);
            }
            else
            {
                _sendMessageButton.Enabled = b;
            }
        }

        delegate void _endableSetNameButtonDel(bool b);
        public void _endableSetNameButton(bool b)
        {
            if (InvokeRequired)
            {
                _endableSetNameButtonDel con = _endableSetNameButton;
                _setNameButton.Invoke(con, b);
            }
            else
            {
                _setNameButton.Enabled = b;
            }
        }
        #endregion
        delegate void _disConnectFromServerDel();
        public void _disconnectFromServer()
        {
            //if (InvokeRequired)
            //{
            //    _disConnectFromServerDel del = _disconnectFromServer;
            //    this.Invoke(del);
            //}
            //else
            //{
         //   isConneected = false;
            Thread.Sleep(200);
            try
            {
                bfmt.Serialize(myStream, new QuitGame());
                done = true;
                _updateStatusBox(string.Empty);
                //_enableConnectButton(true);
                _enableButton(_connectButton, true);
                //_enableDisconnectButton(false);
                _enableButton(_disconnectButton, false);
                //  _enableSendButton(false);
                _enableButton(_sendMessageButton, false);
                _updateStatusBox($"Disconnected from the server !!!\n");
              //  _resetPictureBox();
            }
            catch (Exception ex)
            {
                _updateStatusBox($"{ex.Message}\n");
            }
            // }

        }



        delegate void _updateStatusBoxDelegate(string message);
        public void _updateStatusBox(string message)
        {
            if (InvokeRequired)
            {
                _updateStatusBoxDelegate msgDel = _updateStatusBox;
                _richTextBox.Invoke(msgDel, message);
            }
            else
            {
                _richTextBox.AppendText(message);
                _richTextBox.ScrollToCaret();
            }
        }

        private IPAddress _getIPAddress(string ipAddress)
        {
            IPAddress address = IPAddress.Parse(LOCALHOST);
            try
            {
                if (!IPAddress.TryParse(ipAddress, out address))
                {
                    address = IPAddress.Parse(LOCALHOST);
                }
            }
            catch (Exception ex)
            {
                _richTextBox.AppendText($"Invalid IP address {address.ToString()}\n");
                _richTextBox.AppendText($"{ex.ToString()}\n");//this is good for an error log for showing to the user is not necessary
            }

            return address;
        }

        private int _getPort(string serverPort)
        {
            int port = DEFAULT_PORT;

            try
            {
                if (!Int32.TryParse(serverPort, out port))
                {
                    port = DEFAULT_PORT;
                }

            }
            catch (Exception ex)
            {
                _richTextBox.AppendText($"Invalid port value - Client will connect to port: {port.ToString()}\n");
                _richTextBox.AppendText($"{ex.ToString()}\n");//this is good for an error log for showing to the user is not necessary

            }
            return port;
        }


        #endregion

        private void _setNameButton_Click(object sender, EventArgs e)
        {
            string name = _playerNameTextBox.Text.Trim();

            if (name != "")
            {
                myPlayer = new Player(name);
                _balanceTextBox.Text = "$"+myPlayer.MyMoney.ToString();
                _bidNumericUpDown.Maximum = myPlayer.MyMoney;
                bfmt.Serialize(myStream, myPlayer);
                _enableSendButton(true);
                _endableSetNameButton(false);
                bfmt.Serialize(myStream, new StartNewRound());
            }
            else
            {
                _updateStatusBox("Must enter your name\n");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _disconnectFromServer();
        }

        private void _bidButton_Click(object sender, EventArgs e)
        {
            Thread.Sleep(200);
            _enableButton(_foldButton, false);
            _enableButton(_bidButton, false);
            _player1MoneyPictureBox.Image = moneyImageList.Images[2];
            _player1MoneyPictureBox.Visible = true;
            bfmt.Serialize(myStream, new GameMessage($"{myPlayer.Name} bids ${(int)_bidNumericUpDown.Value}\n"));

            bfmt.Serialize(myStream, (int)_bidNumericUpDown.Value);
            _updateBalance((int)_bidNumericUpDown.Value, true);

            myPlayer.MyStatus = Status.Bid;
            bfmt.Serialize(myStream, myPlayer.MyStatus);

            _updateStatusBox("Waiting for another player . . . \n");
        }

        private void _foldButton_Click(object sender, EventArgs e)
        {
            Thread.Sleep(200);
            Warning warningForm = new Warning();
            if (warningForm.ShowDialog() == DialogResult.OK) {
                myPlayer.MyStatus = Status.Fold;
                myPoints = -1;
                myPlayer.MyHand = new PlayingCard[] { };
                bfmt.Serialize(myStream, myPlayer.MyStatus);
            }
        }

        private void _checkWinCondition()
        {
            
            foreach (PlayingCard card in opponentHands)
            {
                opponentPoints += card.Points;
                opponentPoints %= 10;
            }

            foreach (PlayingCard card in myPlayer.MyHand)
            {
                myPoints += card.Points;
                myPoints %= 10;
            }

            if (opponentPoints > myPoints)
            {
              //  _updateStatusBox("You lose\n");
                bfmt.Serialize(myStream, new GameMessage("You Win\n"));
             //   System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer("Shoryuken.wav");//cross thread with the client occansionally
             //   soundPlayer.Play();
          
            } else if (myPoints > opponentPoints )
            {
              //  _updateStatusBox("You Win\n");
                bfmt.Serialize(myStream, new GameMessage("You Lose\n"));
              //  System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer("incorrect.wav");
              //  soundPlayer.Play();
              
                _updateBalance(biddingPool, false);
            } else
            {
              //  _updateStatusBox("Draw\n");
                bfmt.Serialize(myStream, new GameMessage("Draw\n"));
               
                _updateBalance(biddingPool / 2, false);
            }
            _setMoneyPictureBoxVisible(_player1MoneyPictureBox, false);
            _setMoneyPictureBoxVisible(_player2MoneyPictureBox, false);
            if (myPlayer.MyMoney <= 0)
            {
                System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer("funnySong.wav");
                soundPlayer.Play();
                myPlayer.MyMoney = 100;
                _updateStatusBox("This is what you get lol\n");
                _enableButton(_bidButton, false);
                _enableButton(_foldButton, false);
                Thread.Sleep(10000);
                _updateStatusBox("You have been ricked\n");
                //  bfmt.Serialize(myStream, new QuitGame());
                // isConneected = false;
                soundPlayer.Stop();
            }
            else if (myPlayer.MyMoney >= 200)
            {
                System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer("10wins.wav");
                soundPlayer.Play();
                myPlayer.MyMoney = 100;
                _updateStatusBox("Waiting for another player\n");
                _enableButton(_bidButton, false);
                _enableButton(_foldButton, false);
                Thread.Sleep(10000);
                _updateStatusBox("Other player has been ricked\n");
                //  bfmt.Serialize(myStream, new QuitGame());
                //  isConneected = false;
                soundPlayer.Stop();
            }
            
        }

        delegate void _setMoneyPictureBoxVisibleDel(PictureBox p , bool b);
        private void _setMoneyPictureBoxVisible(PictureBox p ,bool b) 
        {
            if (InvokeRequired)
            {
                _setMoneyPictureBoxVisibleDel del = _setMoneyPictureBoxVisible;
                this.Invoke(del, p, b);
            }
            else 
            {
                p.Visible = b;
            }
        }

      //  delegate void _resetPictureBoxDel();
        private void _resetPictureBox()
        {
            //if (InvokeRequired)
            //{
            //    _resetPictureBoxDel del = _resetPictureBox;
            //    this.Invoke(del);
            //}
            //else 
            //{
                foreach (PictureBox pb in pBoxes)
                {
                    pb.Image = imageList.Images[56];
                }

                foreach (PictureBox pb in opBoxes)
                {
                    pb.Image = imageList.Images[56];
                }
           // }
            
        }

        private delegate void runTimerDel();
        private void runTimer()
        {
            if (InvokeRequired)
            {
                runTimerDel del = new runTimerDel(runTimer);
                this.Invoke(del);
            } else
            {
                timer.Start();
                _enableButton(_foldButton, false);
                _enableButton(_bidButton, false);
            }
        }
        //bool isConneected;
        private void timer_Tick(object sender, EventArgs e)
        {
            timerCount++;
            
            if (timerCount > 5)
            {
                timer.Stop();
                _resetPictureBox();
                //reset myhands
                myPlayer.MyHand = new PlayingCard[2];
                myPoints = 0;
                //reset opponenthands
                opponentHands = new PlayingCard[2];
                opponentPoints = 0;
                timerCount = 0;
                biddingPool = 0;
             //   if(isConneected)
                    bfmt.Serialize(myStream, new StartNewRound());
                _setMoneyPictureBoxVisible(_poolMoneyPictureBox, false);
            }
           
               
        }

        private delegate void _updateBalanceDel(int value, bool isLosing);
        private void _updateBalance(int value, bool isLosing)
        {
            if (InvokeRequired)
            {
                _updateBalanceDel del = _updateBalance;
                this.Invoke(del, value, isLosing);
            }
            else
            {
                if (isLosing)
                {
                    myPlayer.MyMoney -= value;
                } else
                {
                    myPlayer.MyMoney += value;
                }
                _balanceTextBox.Text = "$" + myPlayer.MyMoney;
            }
        }

        private void _sendMessageButton_Click(object sender, EventArgs e)
        {
            string message = _commandTextBox.Text.Trim();
            if (message != "") 
            {
                bfmt.Serialize(myStream, new GameMessage($"{myPlayer.Name}: {message}\n"));
                _commandTextBox.Text = string.Empty;
            }
                
        }
    }
}
