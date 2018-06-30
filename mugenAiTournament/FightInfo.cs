using System;
using System.Collections.Generic;

namespace mugenAiTournament
{
    public class FightInfo
    {
        public string Player1 { get; set; }
        public int Player1Id { get; set; }
        public string Player2 { get; set; }
        public int Player2Id { get; set; }
        public string Stage { get; set; }
        
        public WinningTeam Result { get; set; }

        public bool MakesSense
        {
            get
            {
                if (Player1.Equals(Player2))
                    return false;
                if (Player1Id.Equals(Player2Id))
                    return false;
                if (Stage == null || Stage.Equals(""))
                    return false;

                return true;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} VS {1}", Player1, Player2);
        }

        protected bool Equals(FightInfo other)
        {
            return (Player1Id == other.Player1Id && Player2Id == other.Player2Id) ||
                   ((Player1Id == other.Player2Id) && (Player2Id == other.Player1Id));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FightInfo) obj);
        }

        public override int GetHashCode()
        {
            int a = Math.Min(Player1Id, Player2Id);
            int b = Math.Max(Player2Id, Player1Id);
            unchecked
            {
                return (a * 397) ^ b;
            }
        }
    }
}