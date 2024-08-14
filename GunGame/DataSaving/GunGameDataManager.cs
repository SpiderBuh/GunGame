using System.IO;
using System.Xml.Serialization;

namespace GunGame.DataSaving
{
    public static class GunGameDataManager
    {
        public static void SaveData(ConfigObject conf)
        {
            XmlSerializer serializer = new XmlSerializer(conf.Wrapper.GetType());
            using (TextWriter writer = new StreamWriter(Plugin.ConfigFilePath + conf.FileName))
            {
                serializer.Serialize(writer, conf.Wrapper);
            }
        }
        public static T LoadData<T>(string ConfigFile)
        {
            if (File.Exists(Plugin.ConfigFilePath + ConfigFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StreamReader(Plugin.ConfigFilePath + ConfigFile))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            else
            {
                return default;
            }
        }
    }
}