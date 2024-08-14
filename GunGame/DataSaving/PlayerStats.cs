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
            public List<ScoreData> Scores { get; set; } = new List<ScoreData>();
            public List<RoundData> Rounds { get; set; } = new List<RoundData>();

            public void AddScores(List<PlayerData> PlayerList, List<ScoreData> GameResults, RoundData RoundInfo)
            {
                foreach (var plr in PlayerList)
                {
                    if (Players.TryGetFirstIndex(x => x.UserID == plr.UserID, out int plrIndex))
                        Players.ElementAt(plrIndex).Nickname = plr.Nickname;
                    else
                        Players.Add(plr);
                }
                Scores.AddRange(GameResults);
                Rounds.Add(RoundInfo);
            }

            public void RemovePlayer(List<string> uIDs)
            {
                foreach (var uID in uIDs)
                    RemovePlayer(uID);
            }
            public void RemovePlayer(string uID)
            {
                Players.RemoveAll(x => x.UserID.Equals(uID));
                Scores.RemoveAll(x => x.UserID.Equals(uID));
            }
        }


        [Serializable]
        public class PlayerData
        {
            public PlayerData() { }
            public string UserID { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty;
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
        }

        [Serializable]
        public class ScoreData
        {
            public ScoreData() { }
            public string UserID { get; set; } = string.Empty;
            public string RoundID { get; set; } = string.Empty;
            public int kills { get; set; } = 0;
            public int deaths { get; set; } = 0;
            public int Score { get; set; } = -1;
            public int Position { get; set; } = -1;
            public bool NTF { get; set; } = false;

            public ScoreData(string userID, int kills, int deaths, int position, bool ntf)
            {
                UserID = userID;
                Position = position;
                NTF = ntf;
                this.kills = kills;
                this.deaths = deaths;
            }

            public ScoreData(string userID, string roundID, int score, int position, bool ntf)
            {
                UserID = userID;
                RoundID = roundID;
                Score = score;
                Position = position;
                NTF = ntf;
            }
        }

        [Serializable]
        public class RoundData
        {
            public RoundData() { }
            public int RoundNumber { get; set; } = -1;
            public bool FFA { get; set; } = false;
            public FacilityZone Zone { get; set; } = FacilityZone.None;
            public byte NumGuns { get; set; } = 0;
            public DateTime RoundDate { get; set; } = DateTime.Now;
            [XmlElement]
            public string RoundID => RoundDate.ToString("MMMddHHmm") + "#" + RoundNumber;

            public RoundData(int roundNumber, bool ffa, FacilityZone zone, byte numGuns, DateTime roundDate, out string roundID)
            {
                RoundNumber = roundNumber;
                FFA = ffa;
                Zone = zone;
                NumGuns = numGuns;
                RoundDate = roundDate;
                roundID = RoundID;
            }
        }
    }
}
