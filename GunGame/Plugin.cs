using GunGame.Game;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;

namespace GunGame
{
    public class Plugin
    {

        [PluginConfig]
        public static Config Config;

        public static string ConfigFolderPath { get; private set; }
        public static bool GameInProgress => GG != null && GG.GunsGaming;
        public static GunGameGame GG;

        [PluginEntryPoint("Gun Game", "1.2.0", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {
            Log.Info($"Guns are gaming...");

            ConfigFolderPath = PluginHandler.Get(this).MainConfigPath;
            ConfigFolderPath = ConfigFolderPath.Remove(ConfigFolderPath.Length - "config.yml".Length); //Is this better or worse than having a magic number?
            WeaponManager.LoadWeapons();
            GG = new GunGameGame();

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
