using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace CardLib2
{
    [Serializable]
    public class PlayingCard
    {
        public CardRank Rank { get; set; }
        public CardSuit Suit { get; set; }
        public int Points { get; set; } = 0;

        public int ID { get; set; }

        public string Name { get; set; }

        public string Abbrev { get; set; }
        public bool IsFaceUp { get; set; }
        public Image FrontImage { get; set; } = null;
        public Image BackImage { get; set; } = null;
        public PlayingCard(CardRank rank, CardSuit suit)
        {
            Rank = rank;
            Suit = suit;
            //Points = 0;
            //FrontImage = null;
            //BackImage = null;
            ID = (int)Suit * 13 + (int)Rank;
            Name = String.Format("{0} of {1}", Rank, Suit);
            Abbrev = GetDefaultAbbrev(); // can't set this at the property
        }

        public override string ToString()
        {
            return Name;
        }
        public string GetDefaultAbbrev()
        {
            int ordinal = (int)Rank + 2;
            string abbrev = "";
            if (ordinal < 10)
            {
                abbrev = ordinal.ToString() + Suit.ToString()[0];
            }
            else
            {
                abbrev = string.Format("{0}{1}", Rank.ToString()[0], Suit.ToString()[0]);
            }
            return abbrev;
        }
        public void Flip()
        {
            IsFaceUp = !IsFaceUp;
        }
        public Image GetImage()
        {
            if (IsFaceUp) return FrontImage;
            else return BackImage;
        }
    }
}
