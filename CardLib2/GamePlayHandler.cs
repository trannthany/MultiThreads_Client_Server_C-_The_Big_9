using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CardLib2
{
    [Serializable]
    public class QuitGame { }
    [Serializable]
    public class StartNewRound{}
    //[Serializable]
    //public class Hand 
    //{
    //    public PlayingCard[] MyHand { get; set; }
    //    public Hand(PlayingCard card1, PlayingCard card2) 
    //    {
    //        MyHand = new PlayingCard[2];
    //        MyHand[0] = card1;
    //        MyHand[1] = card2;
    //    }

    //    public override string ToString()
    //    {
    //        return string.Format($"{MyHand[0].ToString()}; {MyHand[1].ToString()}");
    //    }

        
    //}

    [Serializable]
    public class GameMessage 
    {
        public string Message { get; set; }
        public int Money { get; set; }

        public GameMessage(int money, string message) 
        {
            Money = money;
            Message = message;
        }

        public GameMessage(string message){
            Message = message;
        }
    }

    [Serializable]
    public class Player
    { 
        public string Name { get; set; }
        // public TcpClient MyTcpClient { get; set; }
        // public NetworkStream MyNetworkStream { get; set; }
        public PlayingCard[] MyHand { get; set; }
        public Status MyStatus { get; set; }
        public int MyMoney { get; set; }
        public int MyBid { get; set; }

        public Player(string name) 
        {
            Name = name;
            MyHand = new PlayingCard[2];
            MyMoney = 100;
            MyBid = 0;
          //  MyTcpClient = client;
         //   MyNetworkStream = client.GetStream();
        }
        public override string ToString()
        {
            if (MyHand != null)
                return string.Format($"{MyHand[0].ToString()}; {MyHand[1].ToString()}");
            else
                return "Hand is empty";
        }
    }

}
