using GunGame.DataSaving;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using static GunGame.DataSaving.WeaponAttachments;

namespace GunGame
{
    public class Plugin
    {

        public static string ConfigFilePath { get; private set; }
        public static bool GameInProgress = false;
        public static GunGameUtils GG;
        public static WeaponAttachments WeaponData { get; private set; }
        public static PlayerStats PlayerStats { get; private set; }
        public static GGConfig Config { get; private set; }

        [PluginEntryPoint("Gun Game", "1.1.2", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {
            Log.Info($"Guns are gaming...");

            ConfigFilePath = PluginHandler.Get(this).MainConfigPath;

            WeaponData = new WeaponAttachments();
            PlayerStats = new PlayerStats();
            Config = new GGConfig();

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
