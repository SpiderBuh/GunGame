using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GunGame
{

    //public enum EventType
    //{
    //    NONE = 0,

    //    //Infection = 1,
    //    //Battle = 2,
    //    //Hush = 3,
    //    Gungame = 4
    //}
    public class Plugin
    {
        public static bool EventInProgress = false;
        //public static EventType CurrentEvent = EventType.NONE;

        [PluginEntryPoint("Gun Game", "1.0.0", "Gamemode Gun Game recreated in SCP SL", "SpiderBuh")]
        public void OnPluginStart()
        {
            Log.Info($"Plugin is loading...");

            EventManager.RegisterEvents<Events>(this);

        }
    }
}
