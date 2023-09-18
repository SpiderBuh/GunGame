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

        public string[] Usage { get; } = { "FFA? ([y]/n)", "Zone? ([L]/H/E/S)", "Kills to win?", "Round number", /*"Full Shuffle? (y/[n])"*/ };

        public static GunGameUtils GG;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                GG = new GunGameUtils(
                    (arguments.Count > 0 && arguments.ElementAt(0).ToUpper() == "Y"),
                    (arguments.Count > 1 ? charZone(arguments.ElementAt(1).ToUpper()[0]) : charZone('L')),
                    (byte)((arguments.Count > 2 && byte.TryParse(arguments.ElementAt(2), out byte parsedValue)) ? parsedValue : 20)
                    );

                //if (3 < arguments.Count && arguments.ElementAt(3).ToUpper() == "Y")
                //    AllWeapons.ShuffleList();
                GG.currRound = (arguments.Count > 3 && int.TryParse(arguments.ElementAt(3), out int roundSet)) ? roundSet : 0;

                foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
                {
                    if (plr.IsServer)
                        continue;
                    GG.AssignTeam(plr);
                    GG.SpawnPlayer(plr);

                    if (plr.DoNotTrack)
                        plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.</color>", 15);
                }
                Server.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=yellow>Race to the final weapon!</color>", 10, shouldClearPrevious: true);

                if (zone == FacilityZone.Surface && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244a, out var gma) && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out var gpa)) //SCP244 obsticals on surface
                {
                    ExplosionUtils.ServerExplode(new Vector3(72f, 992f, -43f), new Footprint()); //Bodge to get rid of old grandma's if the round didn't restart
                    ExplosionUtils.ServerExplode(new Vector3(11.3f, 997.47f, -35.3f), new Footprint());

                    Scp244DeployablePickup Grandma = UnityEngine.Object.Instantiate(gma.PickupDropModel, new Vector3(72f, 992f, -43f), UnityEngine.Random.rotation) as Scp244DeployablePickup;
                    Grandma.NetworkInfo = new PickupSyncInfo
                    {
                        ItemId = gma.ItemTypeId,
                        WeightKg = gma.Weight,
                        Serial = ItemSerialGenerator.GenerateNext()
                    };
                    Grandma.State = Scp244State.Active;
                    NetworkServer.Spawn(Grandma.gameObject);

                    Scp244DeployablePickup Grandpa = UnityEngine.Object.Instantiate(gpa.PickupDropModel, new Vector3(11.3f, 997.47f, -35.3f), UnityEngine.Random.rotation) as Scp244DeployablePickup;
                    Grandpa.NetworkInfo = new PickupSyncInfo
                    {
                        ItemId = gpa.ItemTypeId,
                        WeightKg = gpa.Weight,
                        Serial = ItemSerialGenerator.GenerateNext()
                    };
                    Grandpa.State = Scp244State.Active;
                    NetworkServer.Spawn(Grandpa.gameObject);
                }
                EventInProgress = true;
                Round.IsLocked = true;
                DecontaminationController.Singleton.enabled = false;
                Round.Start();
                Server.FriendlyFire = FFA;
                GameStarted = true;
                response = $"GunGame event has begun. \nFFA: {FFA} | Zone: {zone} | Levels: {AllWeapons.Count}";
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