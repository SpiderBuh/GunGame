using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using Interactables.Interobjects;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Usables.Scp330;
using MapGeneration;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GunGame.Plugin;
using GameCore;
using Mirror;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace GunGame
{
    public class GunGameUtils
    {
        public static bool GameStarted = false;
        public static bool FFA = true;
        public static FacilityZone zone = FacilityZone.LightContainment;

        public static short[] InnerPosition = new short[0];
        public static Dictionary<string, PlrInfo> AllPlayers = new Dictionary<string, PlrInfo>(); //plrInfo stores: (NTF = true | Chaos = false) and Score

        public class PlrInfo
        {
            public bool IsNtfTeam { get; set; }
            public byte Score { get; set; }
            public short InnerPos { get; set; }
            public string[] lastHit { get; set; } = { null, null };

            public PlrInfo(bool isNtfTeam, byte score = 0)
            {
                IsNtfTeam = isNtfTeam;
                Score = score;
                InnerPos = ++InnerPosition[score];
            }
            public void inc()
            {
                InnerPos = ++InnerPosition[++Score];
            }
            public void softInc()
            {
                if (InnerPos >= 0)
                    InnerPos = (short)(++InnerPosition[Score + 1] - 64);
            }
            public void hit(string plrUID)
            {
                if (plrUID == lastHit[0])
                    return;
                lastHit[1] = lastHit[0];
                lastHit[0] = plrUID;
            }
        }

        public class Gat
        {
            public ItemType ItemType { get; set; }
            public uint Mod { get; set; } = 0;

            public Gat(ItemType itemType, uint attachmentCode = 0)
            {
                ItemType = itemType;
                Mod = attachmentCode;
            }
        }

        public static byte Tntf = 0; //Number of NTF
        public static byte Tchaos = 0; //Number of chaos

        public readonly List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", /*"LczClassDSpawn",*/ "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106", "Hcz079", "Lcz173" };
        public static List<RoomIdentifier> BlacklistRooms = new List<RoomIdentifier>();

        public static Dictionary<Vector3, Vector2Int> Spawns = new Dictionary<Vector3, Vector2Int>(); //List of all possible spawnpoints
        public static Vector3 NTFSpawn; //Current NTF spawn
        public static Vector3 ChaosSpawn; //Current chaos spawn
        public static Vector3[] LoadingRoom = new Vector3[] { new Vector3(-15.5f, 1014.5f, -31.5f), new Vector3(-15.5f, 1014.5f, -31.5f), new Vector3(-15.5f, 1014.5f, -31.5f), new Vector3(-15.5f, 1014.5f, -31.5f) }; //Zone loading room
        public readonly RoomName[] LRNames = new RoomName[] { RoomName.Lcz173, RoomName.Hcz079, RoomName.EzEvacShelter, RoomName.Outside };
        public readonly Vector3[] LROffset = new Vector3[] { new Vector3(3, 15.28f, 7.8f), new Vector3(-3.5f, -4.28f, -12), new Vector3(0, 0.96f, -1.5f), new Vector3(-13, 14.46f, -33f), };

        public static byte credits = 0; //Counts up to next spawnpoint rotation
        public readonly List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding

        public static byte NumGuns = 21;
        public static List<Gat> AllWeapons = new List<Gat>();

        public List<Gat> Tier1 = new List<Gat>() {
            new Gat(ItemType.Jailbird, 1),

            new Gat(ItemType.GunLogicer, 0x1881), //Hex for attachment code much more compact than binary
            
            new Gat(ItemType.GunShotgun, 0x429),

            new Gat(ItemType.GunCrossvec, 0xA054),

            new Gat(ItemType.GunE11SR, 0x1220A42),

            new Gat(ItemType.GunFRMG0, 0x26042),

        };

        public List<Gat> Tier2 = new List<Gat>() {
            new Gat(ItemType.ParticleDisruptor),

            new Gat(ItemType.GunCrossvec, 0x84A1),

            new Gat(ItemType.GunE11SR, 0x491504),
            new Gat(ItemType.GunE11SR),

            new Gat(ItemType.GunFRMG0/*, 0x29210*/),

            new Gat(ItemType.GunShotgun),
        };

        public List<Gat> Tier3 = new List<Gat>() {
            new Gat(ItemType.GunA7),

            new Gat(ItemType.GunCOM18, 0x12A),

            new Gat(ItemType.GunFSP9, 0x2922),

            new Gat(ItemType.GunAK, 0x14901),

            new Gat(ItemType.GunCrossvec),
        };

        public List<Gat> Tier4 = new List<Gat>() {
            //new Gat(ItemType.GunCOM15),            

            new Gat(ItemType.GunCom45),

            new Gat(ItemType.GunCOM18),

            new Gat(ItemType.GunFSP9),

            new Gat(ItemType.GunRevolver),
        };

        public List<Gat> SpecialWeapons = new List<Gat>() {
            new Gat(ItemType.MicroHID, 1),

            new Gat(ItemType.GrenadeHE, 2),

            new Gat(ItemType.SCP018, 5),

            new Gat(ItemType.SCP330), // pink candy
        };
        public static List<Gat> SpecialTier = new List<Gat>();

        public readonly RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.ChaosRepressor, RoleTypeId.NtfCaptain }, { RoleTypeId.ChaosMarauder, RoleTypeId.NtfSergeant }, { RoleTypeId.ChaosConscript, RoleTypeId.NtfPrivate }, { RoleTypeId.ClassD, RoleTypeId.Scientist } }; //Player visual levels

        public static bool SpecialEvent = false; //Event after a zombie is spawned

        public readonly List<Vector3> SurfaceSpawns = new List<Vector3> { new Vector3(0f, 1001f, 0f), new Vector3(8f, 991.65f, -43f), new Vector3(10.5f, 997.47f, -23.25f), new Vector3(63.5f, 995.69f, -34f), new Vector3(63f, 991.65f, -51f), new Vector3(114.5f, 995.45f, -43f), new Vector3(137f, 995.46f, -60f), new Vector3(38.5f, 1001f, -52f), new Vector3(0f, 1001f, -42f) }; //Extra surface spawns

        public void SetNumWeapons(byte num = 20)
        {
            byte T1 = (byte)Math.Round((double)Tier1.Count / NumGuns * num);
            byte T2 = (byte)Math.Round((double)Tier2.Count / NumGuns * num);
            byte T3 = (byte)Math.Round((double)Tier3.Count / NumGuns * num);
            byte Ts = (byte)Math.Round((double)SpecialTier.Count / NumGuns * num);
            byte T4 = (byte)(num - T1 - T2 - T3 - Ts);

            AllWeapons = ProcessTier(Tier1, T1).Concat(ProcessTier(Tier2, T2)).Concat(ProcessTier(Tier3, T3)).Concat(ProcessTier(Tier4, T4)).ToList();
            foreach (var special in ProcessTier(SpecialTier, Ts))
                AllWeapons.Insert(new System.Random().Next(AllWeapons.Count), special);
        }

        ///<summary>Takes in a list and returns a list of the target length comprising of the input's values</summary>
        public List<Gat> ProcessTier(List<Gat> tier, byte target)
        {
            List<Gat> outTier = tier.OrderBy(w => Guid.NewGuid()).Take(target).ToList();
            short additionalCount = (short)(target - tier.Count);
            while (additionalCount > 0)
            {
                short index = (short)new System.Random().Next(tier.Count);
                outTier.Add(tier[index]);
                additionalCount--;
            }
            return outTier;
        }

        ///<summary>Find's vector's 2d grid position</summary>
        public Vector2Int StampGrid(Vector3 spawn) => new Vector2Int((int)spawn.x >> 4, (int)spawn.z >> 4); // Divide by 16

        public void LoadSpawns(bool forceAll = false)
        {
            if (!GameStarted || forceAll)
            {
                byte z = 0;
                foreach (RoomName roomName in LRNames)
                {
                    if (RoomIdUtils.TryFindRoom(LRNames[z], FacilityZone.None, RoomShape.Undefined, out var foundRoom))
                        LoadingRoom[z] = foundRoom.transform.position + foundRoom.transform.rotation * LROffset[z];
                    else LoadingRoom[z] = new Vector3(-15.5f, 1014.5f, -31.5f);

                    z++;
                }

                foreach (string room in BlacklistRoomNames) //Gets blacklisted room objects for current game
                    BlacklistRooms.AddRange(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)));
            }

            if (zone == FacilityZone.Surface)
                foreach (Vector3 spot in SurfaceSpawns)
                    Spawns.Add(spot, StampGrid(spot));

            foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list            
                if (door.IsInZone(zone) && !(door is ElevatorDoor || door is CheckpointDoor) && !door.Rooms.Any(x => BlacklistRooms.Any(y => y == x)))
                {
                    Vector3 doorpos = door.gameObject.transform.position + new Vector3(0, 1, 0);
                    Spawns.Add(doorpos, StampGrid(doorpos));
                    door.NetworkTargetState = true;
                }

            RollSpawns();
        }

        public void RollSpawns(/*byte stop = 0*/) // Current system should be good for Teams, but meh FFA
        {
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
            foreach (Player plr in Player.GetPlayers())
                if (plr.IsAlive)
                    blocked.Add(StampGrid(plr.Position));

            //Dictionary<Vector3, Vector2Int> filteredSpawns = (Dictionary<Vector3, Vector2Int>)Spawns.Where(a => !blocked.Contains(a.Value));
            Dictionary<Vector3, Vector2Int> filteredSpawns = new Dictionary<Vector3, Vector2Int>();                          
                foreach (var ele in Spawns)
                    if (!blocked.Contains(ele.Value))
                    filteredSpawns.Add(ele.Key, ele.Value); 

            var CS = filteredSpawns.ElementAt(new System.Random().Next(filteredSpawns.Count));
            credits = 0;
            ChaosSpawn = CS.Key;
            if (FFA)
                return;
            NTFSpawn = filteredSpawns.Where(c => c.Value != CS.Value).ToList().RandomItem().Key;
            Cassie.Message(".G2", false, false, false);

            //if (stop < MaxRetryCount && (!FFA && Vector3.SqrMagnitude(NTFSpawn - ChaosSpawn) < MinSpawnDistanceSquared))
            //{
            //    RollSpawns((byte)(stop + 1), blocked, filteredSpawns);
            //    return;
            //}                           
        }
        //const float MinSpawnDistanceSquared = 750;
        //const byte MaxRetryCount = 10;

        public void AssignTeam(Player plr) //Assigns player to team
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial/* || AllPlayers.ContainsKey(plr.UserId)*/)
                return;
            if (!AllPlayers.TryGetValue(plr.UserId, out PlrInfo plrInfo))
                AllPlayers.Add(plr.UserId, new PlrInfo((Tntf < Tchaos) && !FFA)); //Adds player to list, uses bool operations to determine teams
            else plrInfo.IsNtfTeam = (Tntf < Tchaos) && !FFA;
            if ((Tntf < Tchaos) && !FFA)
                Tntf++;
            else
                Tchaos++;
        }

        public void RemovePlayer(Player plr) //Removes player from list
        {
            if (AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                if (plrStats.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                AllPlayers.Remove(plr.UserId);
            }
        }

        public void SpawnPlayer(Player plr) //Spawns player
        {
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return;
            }
            int level = FFA ? 3 : Mathf.Clamp((int)Math.Round((double)plrStats.Score / AllWeapons.Count * 3), 0, 2);
            plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, Convert.ToInt32(plrStats.IsNtfTeam)], RoleChangeReason.Respawn, RoleSpawnFlags.None); //Uses bool in 2d array to determine spawn class
            plr.ClearInventory();
            plr.Position = LoadingRoom[((int)zone) - 1];
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>((byte)(AllWeapons.Count - plrStats.Score), 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
                plr.AddAmmo(ammo, (ushort)plr.GetAmmoLimit(ammo));
            plr.SendBroadcast($"Guns left: {AllWeapons.Count - plrStats.Score}", 5);
            plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(200, 5 + 4, false);
            plr.ReferenceHub.inventory.enabled = false;
            MEC.Timing.CallDelayed(4, () =>
            {
                plr.ReferenceHub.inventory.enabled = true;
                plr.Position = plrStats.IsNtfTeam ? NTFSpawn : ChaosSpawn;
                plr.ReferenceHub.playerEffectsController.ChangeState<Invigorated>(127, 5, false);
                GiveGun(plr, 1.5f);
                plr.ReferenceHub.playerEffectsController.ChangeState<Invisible>(127, 2, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Blinded>(127, 0.5f, false);
                if (SpecialEvent) { plr.EffectsManager.EnableEffect<Scp207>(9999); plr.AddItem(ItemType.Painkillers); plr.AddItem(ItemType.Painkillers); plr.AddItem(ItemType.Painkillers); }
                if (FFA) RollSpawns();
            });
        }

        ///<summary>Gives player their next gun and equips it, and removes old gun</summary>
        public void GiveGun(Player plr, float delay = 0)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plr.ReferenceHub.inventory.enabled)
                return;

            if (plrStats.Score > 0)
                foreach (ItemBase item in plr.Items.ToList()) //Removes last gun                
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.Score - 1).ItemType || item.ItemTypeId == AllWeapons.ElementAtOrDefault(plrStats.Score).ItemType)
                    {
                        plr.RemoveItem(item);
                        //break; //Removes all items if user has multiple when commented out
                    }

            MEC.Timing.CallDelayed(delay, () =>
            {
                Gat currGun = AllWeapons.ElementAt(plrStats.Score);
                ItemBase weapon = plr.AddItem(currGun.ItemType);
                if (weapon is Firearm || weapon is Scp330Bag)
                    if (weapon is Firearm)
                    {
                        Firearm firearm = weapon as Firearm;
                        uint attachment_code = currGun.Mod == 0 ? AttachmentsUtils.GetRandomAttachmentsCode(firearm.ItemTypeId) : currGun.Mod; //Random attachments if no attachment code specified
                                                                                                                                               //AttachmentsServerHandler.PlayerPreferences[plr.ReferenceHub][firearm.ItemTypeId] //Player's chosen weapon attachments
                        AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                        firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, attachment_code);
                    }
                    else
                    {
                        Scp330Bag bag = weapon as Scp330Bag;

                        List<CandyKindID> bagCandies = new List<CandyKindID>();

                        while (bagCandies.Count < 5)
                            bagCandies.Add((CandyKindID)new System.Random().Next(1, 7));
                        bagCandies.Add(CandyKindID.Pink);
                        bagCandies.ShuffleList();
                        bag.Candies = bagCandies;
                        bag.ServerRefreshBag();
                    }
                else
                {
                    byte i = (byte)currGun.Mod;
                    while (i > 0 && !plr.IsInventoryFull)
                    {
                        plr.AddItem(currGun.ItemType);
                        i--;
                    }
                }

                MEC.Timing.CallDelayed(0.1f, () =>
                {
                    plr.CurrentItem = weapon;
                });
            });
        }

        public void AddScore(Player plr) //Increases player's score
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                //plr.ReceiveHint("You aren't registered as a player. Try rejoining", 5);
                return;
            }
            plr.EffectsManager.EnableEffect<Invigorated>(5, true);
            if (!FFA && plr.Role == Roles[3, Convert.ToInt32(plrStats.IsNtfTeam)])
            {
                plr.AddAmmo(ItemType.Ammo9x19, 12);
                //plr.Heal(25); //Broke after 13.2 update
                plr.GetStatModule<HealthStat>().ServerHeal(25);
                return;
            }
            if (plrStats.Score >= AllWeapons.Count - 1) //Final level check
            {
                plrStats.softInc();
                Firearm firearm = null;
                plr.ClearInventory();
                if (!FFA)
                {
                    plr.ReferenceHub.roleManager.ServerSetRole(Roles[3, Convert.ToInt32(plrStats.IsNtfTeam)], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                    plr.Damage(50, "krill issue (your health was too low somehow)");
                    Cassie.Message("pitch_1.50 .G4", false, false, false);
                    firearm = plr.AddItem(ItemType.GunCOM15) as Firearm;
                    AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x2B, true);
                    firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x2B);
                    while (!plr.IsInventoryFull)
                        plr.AddItem(ItemType.Coin);
                    plr.AddAmmo(ItemType.Ammo9x19, 13);
                    MEC.Timing.CallDelayed(0.1f, () =>
                    {
                        plr.CurrentItem = firearm;
                    });
                    return;
                }

                plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.Respawn, RoleSpawnFlags.AssignInventory); //Spawns zombie without increasing score
                plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(20, 99999, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(11, 99999, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Scp207>(1, 99999, false);
                if (!SpecialEvent) { AlphaWarheadController.Singleton.StartDetonation(false, true); SpecialEvent = true; Cassie.Message("WARNING . .G3 . SCP 0 4 9 2 DETECTED", false, false, false); }
                else Cassie.Message("pitch_1.50 .G4", false, false, false);
                firearm = plr.AddItem(ItemType.GunCOM15) as Firearm;
                AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x2B, true);
                firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x2B);
                plr.AddAmmo(ItemType.Ammo9x19, 26);
                MEC.Timing.CallDelayed(0.1f, () =>
                {
                    plr.CurrentItem = firearm;
                });
                return;
            }

            plrStats.inc(); //Adds 1 to score
            GiveGun(plr);
            plr.SendBroadcast($"{AllWeapons.Count - plrStats.Score}", 1);
            //MEC.Timing.CallDelayed(1f, () =>
            //{
            //    if (plr.Items.Count <= 6)
            //        plr.AddItem(ItemType.Medkit);                                
            //});

        }

        public void RemoveScore(Player plr)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;

            if (plr.IsAlive)
                foreach (ItemBase item in plr.Items) //Removes last gun
                {
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.Score).ItemType)
                    {
                        plr.RemoveItem(item);
                        break;
                    }
                }

            if (plrStats.Score > 0)
            {
                plrStats.Score--;
                if (plr.IsAlive)
                    GiveGun(plr);
            }
        }

        public void TriggerWin(Player plr) //Win sequence
        {

            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;

            plrStats.Score++;
            Round.IsLocked = false;
            Server.FriendlyFire = true;
            EventInProgress = false;
            //plr.Health = 42069; //Broke after 13.2 update
            plr.GetStatModule<HealthStat>().CurValue = 42069;
            plr.ReferenceHub.playerEffectsController.DisableAllEffects();
            ChaosSpawn = plr.Position;
            NTFSpawn = plr.Position;

            var sortedPlayers = AllPlayers.OrderByDescending(pair => pair.Value.Score)
                                         .ThenBy(pair => pair.Value.InnerPos)
                                         .ToDictionary(pair => pair.Key, pair => pair.Value);

            string team = "";
            if (!FFA)
                team = "\n and " + (plrStats.IsNtfTeam ? "NTF" : "Chaos") + " helped too I guess";

            Dictionary<string, int> playerPoints = new Dictionary<string, int>();
            int plrsLeft = sortedPlayers.Count();
            foreach (var loserEntry in sortedPlayers)
            {
                if (!Player.TryGet(loserEntry.Key, out Player loser))
                    continue;
                PlrInfo loserStats = loserEntry.Value;
                if (loser.IsServer || loser.IsOverwatchEnabled || loser.IsTutorial)
                {
                    plrsLeft--;
                    continue;
                }

                int teamBonus = !FFA & (loserStats.IsNtfTeam == plrStats.IsNtfTeam) ? 2 : 0;
                int positionScore = Convert.ToInt32((double)(plrsLeft + 1) / sortedPlayers.Count() * 13);

                loser.ClearInventory();
                if (loser != plr)
                {
                    loser.SetRole(RoleTypeId.ClassD);
                    loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                    loser.Position = plr.Position;
                }
                else positionScore = 15;
                loser.SendBroadcast($"<b><color=yellow>{plr.Nickname} wins!</color></b>{team}", 10, shouldClearPrevious: true);
                if (!loser.DoNotTrack)
                    playerPoints.Add(loser.Nickname, positionScore + teamBonus);
                plrsLeft--;
            }
            ScoreManager.ScoreStorage.AddPoints(playerPoints);
            Firearm firearm = plr.AddItem(ItemType.GunLogicer) as Firearm;
            AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x1881, true);
            firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x1881);
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = firearm;
            });
        }
    }
}
