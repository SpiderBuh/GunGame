﻿using MapGeneration;
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
        public GunGameDataWrapper() { }
        public List<PlayerData> Players { get; set; } = new List<PlayerData>();
        public List<ScoreData> Scores { get; set; } = new List<ScoreData>();
        public List<RoundData> Rounds { get; set; } = new List<RoundData>();
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
        public int Score { get; set; } = -1;
        public int Position { get; set; } = -1;
        public bool NTF { get; set; } = false;

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

    public static class GunGameDataManager
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
            foreach (var plr in PlayerList) //Registers players
            {
                if (data.Players.TryGetFirstIndex(x => x.UserID == plr.UserID, out int plrIndex))
                    data.Players.ElementAt(plrIndex).Nickname = plr.Nickname;
                else
                    data.Players.Add(plr);
            }
            data.Scores.AddRange(GameResults);
            data.Rounds.Add(RoundInfo);
            SaveData(data);
        }

        public static void UserScrub(string uID) => SaveData(removePlayer(uID, LoadData()));
        public static void UserScrub(List<string> uIDs) => SaveData(removePlayer(uIDs, LoadData()));
        private static GunGameDataWrapper removePlayer(List<string> uIDs, GunGameDataWrapper data)
        {
            foreach (var uID in uIDs)
                data.removePlayer(uID);
            return data;
        }
        private static GunGameDataWrapper removePlayer(string uID, GunGameDataWrapper data)
        {
            data.Players.RemoveAll(x => x.UserID.Equals(uID));
            data.Scores.RemoveAll(x => x.UserID.Equals(uID));
            return data;
        }
        private static void removePlayer(this GunGameDataWrapper data, string uID) => removePlayer(uID, data); //Extension
    }
}
