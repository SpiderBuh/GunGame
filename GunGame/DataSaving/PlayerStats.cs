using MapGeneration;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Utils.NonAllocLINQ;

namespace GunGame.DataSaving
{
    public class PlayerStats : ConfigObject
    {
        public override string FileName => "GunGamePlayerData.xml";
        public override DataWrapper Wrapper => AllPlayerData;
        public PlayerDataWrapper AllPlayerData { get; private set; }
        public PlayerStats() { AllPlayerData = GunGameDataManager.LoadData<PlayerDataWrapper>(FileName); }
        public PlayerStats(PlayerDataWrapper d)
        {
            AllPlayerData = d;
        }

        [Serializable]
        public class PlayerDataWrapper : DataWrapper
        {
            public PlayerDataWrapper() { }
            public List<PlayerData> Players { get; set; } = new List<PlayerData>();

            public void AddScores(List<PlayerData> PlayerList)
            {
                foreach (var plr in PlayerList)
                {
                    if (Players.TryGetFirstIndex(x => x.UserID == plr.UserID, out int plrIndex)) {
                        Players.ElementAt(plrIndex).Nickname = plr.Nickname;
                        Players.ElementAt(plrIndex).Kills += plr.Kills;
                        Players.ElementAt(plrIndex).Deaths += plr.Deaths;
                        Players.ElementAt(plrIndex).GamesPlayed += plr.GamesPlayed;
                    }
                    else
                        Players.Add(plr);
                }
            }

            public void RemovePlayer(List<string> uIDs)
            {
                foreach (var uID in uIDs)
                    RemovePlayer(uID);
            }
            public void RemovePlayer(string uID)
            {
                Players.RemoveAll(x => x.UserID.Equals(uID));
            }
        }

        [Serializable]
        public class PlayerData
        {
            public PlayerData() { }
            public string UserID { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty;
            public int Kills { get; set; } = 0;
            public int Deaths { get; set; } = 0;
            public int GamesPlayed { get; set; } = 0;
            [XmlIgnore]
            public float KDR => ((float)Kills) / Deaths;
            public PlayerData(Player plr)
            {
                UserID = plr.UserId;
                Nickname = plr.Nickname;
            }
            public PlayerData(string UID, string Nick)
            {
                UserID = UID;
                Nickname = Nick;
            }
            public static bool operator==(PlayerData a, PlayerData b) { return a.UserID.Equals(b.UserID); }
            public static bool operator!=(PlayerData a, PlayerData b) { return !a.UserID.Equals(b.UserID); }
            public static PlayerData operator+(PlayerData a, PlayerData b) { return new PlayerData(b.UserID, b.Nickname) { Kills = a.Kills + b.Kills, Deaths = a.Deaths + b.Deaths, GamesPlayed = a.GamesPlayed + b.GamesPlayed };  }

            public override string ToString()
            {
                return Nickname + "\t" + KDR;
            }

            public override bool Equals(object obj)
            {
                return this==(PlayerData)obj;
            }

            public override int GetHashCode()
            {
                return UserID.GetHashCode();
            }
        }
    }
}
