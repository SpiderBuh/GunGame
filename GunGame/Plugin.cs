using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using static GunGame.DataSaving.WeaponAttachments;

namespace GunGame
{
    public class Plugin
    {
        public static bool GameInProgress = false;
        public static GunGameUtils GG;
        public static WeaponDataWrapper WeaponData;

        [PluginEntryPoint("Gun Game", "1.1.0", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {

            Log.Info($"Guns are gaming...");

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
