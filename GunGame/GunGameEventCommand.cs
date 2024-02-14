using CommandSystem;
using Footprinting;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PluginAPI.Core;
using System;
using System.Linq;
using UnityEngine;
using Utils;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GunGameEventCommand : ICommand, IUsageProvider
    {
        public string Command => "gungame";

        public string[] Aliases => null;

        public string Description => "Starts the GunGame event. (Args optional, [X]=default)";

        public string[] Usage { get; } = { "FFA? (y/[n])", "Zone? ([L]/H/E/S/O)", "Kills to win? [27]", "Round number", /*"Full Shuffle? (y/[n])"*/ };

        public static GunGameUtils GG;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                GG = new GunGameUtils(
                    (arguments.Count > 0 && arguments.ElementAt(0).ToUpper() == "Y"),
                    (arguments.Count > 1 ? charZone(arguments.ElementAt(1).ToUpper()[0]) : charZone('L')),
                    ((arguments.Count > 2 && int.TryParse(arguments.ElementAt(2), out int parsedValue)) ? parsedValue : 20)
                    );
                GG.currRound = (arguments.Count > 3 && int.TryParse(arguments.ElementAt(3), out int roundSet)) ? roundSet : 0;

                if (4 < arguments.Count && arguments.ElementAt(4).ToUpper() == "Y")
                    AllWeapons.ShuffleList();

                GG.Start();

                response = $"GunGame event has begun. \nFFA: {FFA} | Zone: {zone} | Levels: {GG.NumKillsReq}";
                return true;
            }
            catch (Exception e)
            {
                response = $"An error has occurred: {e.Message}\n{e.TargetSite}\n{e.StackTrace}";
                Round.IsLocked = false;
                return false;
            }
        }
    }
}