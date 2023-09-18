using MapGeneration;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Utils.NonAllocLINQ;

namespace GunGame
{
    [Serializable]
    public class GunGameDataWrapper
    {
        public List<PlayerData> Players { get; set; }
        public List<ScoreData> Scores { get; set; }
        public List<RoundData> Rounds { get; set; }

        public void RegisterPlayers(List<PlayerData> PlayerList)
        {
            foreach (var plr in PlayerList)
            {
                if (Players.TryGetFirstIndex(x => x.UserID == plr.UserID, out int plrIndex))
                    Players.ElementAt(plrIndex).Nickname = plr.Nickname;
                else
                    Players.Add(plr);
            }
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string UserID { get; set; }
        public string Nickname { get; set; }
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
        public string UserID { get; set; }
        public string RoundID { get; set; }
        public int Score { get; set; }
        public int Position { get; set; }
        public bool NTF { get; set; }

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
        public int RoundNumber { get; set; }
        public bool FFA { get; set; }
        public FacilityZone Zone { get; set; }
        public byte NumGuns { get; set; }
        public DateTime RoundDate { get; set; }
        public string RoundID => RoundDate.ToString("yyMMddHH") + "#" + RoundNumber;

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

    class GunGameDataManager
    {
        private const string FilePath = "GunGameScoreData.xml";

        public static void SaveData(GunGameDataWrapper dataWrapper)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GunGameDataWrapper));
            using (TextWriter writer = new StreamWriter(FilePath))
            {
                serializer.Serialize(writer, dataWrapper);
            }
        }

        public static GunGameDataWrapper LoadData()
        {
            if (File.Exists(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GunGameDataWrapper));
                using (TextReader reader = new StreamReader(FilePath))
                {
                    return (GunGameDataWrapper)serializer.Deserialize(reader);
                }
            }
            else
            {
                return new GunGameDataWrapper();
            }
        }

        public static void AddScores(List<PlayerData> PlayerList, List<ScoreData> GameResults, RoundData RoundInfo)
        {
            GunGameDataWrapper data = LoadData();
            data.RegisterPlayers(PlayerList);
            data.Scores.AddRange(GameResults);
            data.Rounds.Add(RoundInfo);
            SaveData(data);
        }
    }
}
