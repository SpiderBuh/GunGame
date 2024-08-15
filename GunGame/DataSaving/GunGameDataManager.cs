using System.IO;
using System.Xml.Serialization;

namespace GunGame.DataSaving
{
    public static class GunGameDataManager
    {
        public static void SaveData<T>(T data, string fileName) {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextWriter writer = new StreamWriter(Plugin.ConfigFilePath + fileName))
            {
                serializer.Serialize(writer, data);
            }
        }
        public static void SaveData(ConfigObject conf)
        {
            SaveData(conf.Wrapper, conf.FileName);
        }
        public static T LoadData<T>(string configFile)
        {
            if (File.Exists(Plugin.ConfigFilePath + configFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StreamReader(Plugin.ConfigFilePath + configFile))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            else
            {
                SaveData<T>(default, configFile);
                return default;
            }
        }
    }
}