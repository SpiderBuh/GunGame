using CommandSystem;
using Footprinting;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
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

        public string[] Usage { get; } = { "FFA? ([y]/n)", "Zone? ([L]/H/E/S)", "Special weapons? (y/[n])", "Kills to win?", "Full Shuffle? (y/[n])" };

        public static GunGameUtils GG;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                GG = new GunGameUtils();
                if (arguments.Count >= 1)
                    if (arguments.ElementAt(0).ToUpper().Equals("Y"))
                        FFA = true;
                    else
                        FFA = false;

                if (arguments.Count >= 2)
                    switch (arguments.ElementAt(1).ToUpper().ElementAt(0))
                    {
                        case 'L':
                            zone = FacilityZone.LightContainment; break;
                        case 'H':
                            zone = FacilityZone.HeavyContainment; break;
                        case 'E':
                            zone = FacilityZone.Entrance; break;
                        case 'S':
                            zone = FacilityZone.Surface; break;
                    }

                SpecialEvent = false; // In theory I could remove some of these, but I am unable to test at the moment so they will stay.
                AllPlayers.Clear(); //Clears all values
                Spawns.Clear();
                Tntf = 0;
                Tchaos = 0;
                               
                if (zone == FacilityZone.Surface)                
                    Spawns = GG.SurfaceSpawns.ToList();                

                SpecialTier = new List<Gat>();
                if (arguments.Count >= 3)
                    if (arguments.ElementAt(2).ToUpper().Equals("Y"))
                        SpecialTier = GG.SpecialWeapons;

                NumGuns = (byte)(GG.Tier1.Count + GG.Tier2.Count + GG.Tier3.Count + GG.Tier4.Count + SpecialTier.Count);
                InnerPosition = new short[NumGuns + 2];

                byte NumTarget = arguments.Count < 4 ? NumGuns : byte.Parse(arguments.ElementAt(2));

                GG.SetNumWeapons(NumTarget);

                if (arguments.Count >= 5)
                    if (arguments.ElementAt(4).ToUpper().Equals("Y"))
                        AllWeapons.ShuffleList();

                byte z = 0;
                foreach (RoomName roomName in GG.LRNames)
                {
                    if (RoomIdUtils.TryFindRoom(GG.LRNames[z], FacilityZone.None, RoomShape.Undefined, out var foundRoom))
                        LoadingRoom[z] = foundRoom.transform.position + foundRoom.transform.rotation * GG.LROffset[z];
                    else LoadingRoom[z] = new Vector3(-15.5f, 1014.5f, -31.5f);

                    z++;
                }

                foreach (string room in GG.BlacklistRoomNames) //Gets blacklisted room objects for current game
                    BlacklistRooms.AddRange(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)));

                foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list
                {
                    if (door.IsInZone(zone) && !(door is ElevatorDoor || door is CheckpointDoor) && !door.Rooms.Any(x => BlacklistRooms.Any(y => y == x)))
                    {
                        Vector3 doorpos = door.gameObject.transform.position;
                        Spawns.Add(new Vector3(doorpos.x, doorpos.y + 1, doorpos.z));
                        door.NetworkTargetState = true;
                    }
                }

                GG.RollSpawns(); //Shuffles spawns

                foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
                {
                    if (plr.IsServer)
                        continue;
                    GG.AssignTeam(plr);
                    GG.SpawnPlayer(plr);
                    plr.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=yellow>Race to the final weapon!</color>", 10, shouldClearPrevious: true);

                    if (plr.DoNotTrack)
                        plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.</color>", 15);
                }

                if (zone == FacilityZone.Surface /*&& !Plugin.EventInProgress*/ && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244a, out var gma) && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out var gpa)) //SCP244 obsticals on surface
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
                response = $"GunGame event has begun. \nFFA: {FFA} | Zone: {zone} | Levels: {NumTarget}";
                return true;
            }
            catch (Exception e)
            {
                response = $"An error has occurred: {e.Message}";
                Round.IsLocked = false;
                return false;
            }
        }
    }
}