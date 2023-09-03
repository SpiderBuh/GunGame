using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;

namespace GunGame
{
    public class Plugin
    {
        public static bool EventInProgress = false;

        [PluginEntryPoint("Gun Game", "1.0.0", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {

            Log.Info($"Plugin is loading...");

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
