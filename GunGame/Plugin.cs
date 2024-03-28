using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;

namespace GunGame
{
    public class Plugin
    {
        public static bool GameInProgress = false;
        public static GunGameUtils GG;

        [PluginEntryPoint("Gun Game", "1.1.0", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {

            Log.Info($"Guns are gaming...");

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
