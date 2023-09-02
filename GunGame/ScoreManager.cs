using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GunGame
{
    public class ScoreManager
    {
        [Serializable]
        public class PlayerScore
        {
            public string UserName { get; set; }
            /// <summary>
            /// Stores total points at [0] and individual round points after
            /// </summary>
            public int[] Score { get; set; } = { 0 };
        }

        public class ScoreStorage
        {
            private const string FilePath = "plrScores.xml";

            public static List<PlayerScore> GetPoints()
            {
                if (File.Exists(FilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<PlayerScore>));
                    using (TextReader reader = new StreamReader(FilePath))
                        return (List<PlayerScore>)serializer.Deserialize(reader);
                }
                else
                    return new List<PlayerScore>();
            }

            public static void SaveData(List<PlayerScore> data)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<PlayerScore>));
                using (TextWriter writer = new StreamWriter(FilePath))
                    serializer.Serialize(writer, data);
            }

            public static void AddPoints(string uID, int score)
            {
                List<PlayerScore> data = GetPoints();

                foreach (PlayerScore entry in data)
                    if (entry.UserName == uID)
                    {
                        entry.Score = entry.Score.Append(score).ToArray();
                        entry.Score[0] += score;
                        return;
                    }

                data.Add(new PlayerScore { UserName = uID, Score = new int[] { score, score } });
                SaveData(data);
            }
            public static void AddPoints(Dictionary<string, int> ScoreList)
            {
                List<PlayerScore> data = GetPoints();
                foreach (var kvp in ScoreList)
                {
                    bool found = false;
                    foreach (PlayerScore entry in data)
                        if (entry.UserName == kvp.Key)
                        {
                            entry.Score = entry.Score.Append(kvp.Value).ToArray();
                            entry.Score[0] += kvp.Value;
                            found = true;
                            break;
                        }

                    if (!found)
                        data.Add(new PlayerScore { UserName = kvp.Key, Score = new int[] { kvp.Value, kvp.Value } });
                }
                SaveData(data);
            }
        }


    }
}