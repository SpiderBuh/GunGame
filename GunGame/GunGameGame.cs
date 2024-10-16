﻿using CustomPlayerEffects;
using GunGame.Components;
using GunGame.DataSaving;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static GunGame.DataSaving.PlayerStats;
using static GunGame.DataSaving.WeaponAttachments;
using static GunGame.Plugin;

namespace GunGame
{
    public class GunGameGame
    {
        public bool GunsGaming;
        public bool FFA;
        public FacilityZone zone;
        public byte credits;
        public byte Tntf = 0; //Number of NTF
        public byte Tchaos = 0; //Number of chaos

        public GunGameGame(bool ffa = false, FacilityZone targetZone = FacilityZone.LightContainment, int numKills = 20)
        {
            FFA = ffa;
            zone = targetZone;
            AllWeapons = ProcessTier((byte)(Mathf.Clamp(numKills, 2, 255) - 1)).Concat(FinalTier).ToList();

            GunsGaming = false;
            LoadSpawns(true);
            InnerPosition = new short[256];
            AllPlayers = new Dictionary<string, GGPlayer>();
            Tntf = 0;
            Tchaos = 0;
            credits = 0;
            KillList = new List<KillInfo>();
        }

        public void Start()
        {
            GunsGaming = true;
            foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
            {
                if (plr.IsServer)
                    continue;
                AssignTeam(plr);
                SpawnPlayer(plr);

                if (plr.DoNotTrack)
                    plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round.\nAny existing scores will be deleted as well!</color>", 15);
            }
            Server.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \nRace to the final weapon!", 10, shouldClearPrevious: true);

            if (zone == FacilityZone.Surface && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244a, out var gma) && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out var gpa)) //SCP244 obsticals on surface
            {

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
            Round.IsLocked = true;
            DecontaminationController.Singleton.enabled = false;
            if (!Round.IsRoundStarted)
                Round.Start();
            Server.FriendlyFire = FFA;
        }

        #region utility functions
        public static int gridSize => GG.FFA || GG.zone.Equals(FacilityZone.Surface) ? 4 : 5;
        public static Vector2Int StampGrid(Vector3 spawn, int shift) => new Vector2Int((int)spawn.x >> shift, (int)spawn.z >> shift); // Divide by 2^shift
        public static FacilityZone charZone(char z)
        {
            switch (z)
            {
                case 'L':
                    return FacilityZone.LightContainment;
                case 'H':
                    return FacilityZone.HeavyContainment;
                case 'E':
                    return FacilityZone.Entrance;
                case 'S':
                    return FacilityZone.Surface;
                case 'O':
                    return FacilityZone.Other;
            }
            return FacilityZone.LightContainment;
        }
        public static Dictionary<string, GGPlayer> SortedPlayers => GG.AllPlayers.OrderByDescending(pair => pair.Value.PlayerInfo.Score)
                                         .ThenBy(pair => pair.Value.PlayerInfo.InnerPos)
                                         .ToDictionary(pair => pair.Key, pair => pair.Value);
        public static Func<int, string> ordinal = i => (i >= 11 && i <= 13) ? "th"
                                : (i % 10 == 1) ? "st"
                                : (i % 10 == 2) ? "nd"
                                : (i % 10 == 3) ? "rd"
                                : "th";
        #endregion

        #region map
        public Dictionary<Vector2Int, HashSet<Vector3>> Spawns;
        public Vector3 NTFSpawn; //Current NTF spawn
        public Vector3 ChaosSpawn; //Current chaos spawn
        public readonly List<Vector3> SurfaceSpawns = new List<Vector3> { new Vector3(0f, 1001f, 0f), new Vector3(8f, 991.65f, -43f), new Vector3(10.5f, 997.47f, -23.25f), new Vector3(63.5f, 995.69f, -34f), new Vector3(63f, 991.65f, -51f), new Vector3(114.5f, 995.45f, -43f), new Vector3(137f, 995.46f, -60f), new Vector3(38.5f, 1001f, -52f), new Vector3(0f, 1001f, -42f) }; //Extra surface spawns

        public HashSet<Vector2Int> ChaosTiles = new HashSet<Vector2Int>();
        public HashSet<Vector2Int> NTFTiles = new HashSet<Vector2Int>();
        public Action RefreshBlocklist = () => { };

        public List<RoomIdentifier> BlacklistRooms;
        public readonly List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", /*"LczClassDSpawn",*/ "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106", "Hcz079", "Lcz173" };

        public Vector3[] LoadingRoom;
        public readonly Vector3[] LROffset = new Vector3[] { new Vector3(3, 15.28f, 7.8f), new Vector3(-3.5f, -4.28f, -12), new Vector3(6f, 3.83f, -3.5f), new Vector3(-13, 14.46f, -33f), new Vector3(0, 0.96f, -1.5f), };
        public readonly RoomName[] LRNames = new RoomName[] { RoomName.Lcz173, RoomName.Hcz079, RoomName.EzGateB, RoomName.Outside, RoomName.EzEvacShelter };


        #endregion

        #region players
        public short[] InnerPosition;
        public Dictionary<string, GGPlayer> AllPlayers; //UserID, PlrInfo
        public struct KillInfo
        {
            public string atckr;
            public string vctm;
            public KillFeed.KillType type;
            public KillInfo(string attacker, string victim, KillFeed.KillType killType)
            {
                atckr = attacker;
                vctm = victim;
                type = killType;
            }
        }
        public static List<KillInfo> KillList = new List<KillInfo>();
        public Action SendKills = () => { };

        [Flags]
        public enum GGPlayerFlags : byte
        {
            Chaos = 0b0,
            NTF = 0b1,
            alive = 0b10,
            onMap = 0b100,
            spawned = alive | onMap,
            preFL = 0b1000,
            finalLevel = 0b10000,
            validFL = finalLevel | preFL | spawned,
        }
        public readonly RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.ChaosRepressor, RoleTypeId.NtfCaptain }, { RoleTypeId.ChaosMarauder, RoleTypeId.NtfSergeant }, { RoleTypeId.ChaosConscript, RoleTypeId.NtfPrivate }, { RoleTypeId.ClassD, RoleTypeId.Scientist } }; //Player visual levels
        public class PlrInfo
        {
            public GGPlayerFlags flags;
            public byte Score { get; set; }
            public short InnerPos { get; set; }
            public bool IsNtfTeam => flags.HasFlag(GGPlayerFlags.NTF);
            public int killsLeft => GG.NumKillsReq - Score;
            public int totKills = 0;
            public int totDeaths = 0;
            public void SetTeam(bool NTF) => flags = flags & ~GGPlayerFlags.NTF | (NTF ? GGPlayerFlags.NTF : 0);
            public void inc() => InnerPos = ++GG.InnerPosition[++Score];

            public PlrInfo(bool isNtfTeam, byte score = 0)
            {
                SetTeam(isNtfTeam);
                Score = score;
                InnerPos = ++GG.InnerPosition[score];
            }
        }
        public Dictionary<string, int> Positions()
        {
            var list = new Dictionary<string, int>();
            foreach (var item in SortedPlayers)
                if (Player.TryGet(item.Key, out Player plr))
                    list.Add(plr.Nickname, item.Value.PlayerInfo.Score);

            return list;
        }

        #endregion

        #region weapons
        public class Gat
        {
            public ItemType ItemType;
            public uint Mod;
            public byte Ammo;
            public int kills;
            public int deaths;
            public int usedBy;

            public Gat(ItemType itemType, uint modCode = 0, byte ammoCount = 0)
            {
                ItemType = itemType;
                Mod = modCode;
                if (modCode == 0)
                {
                    Mod = AttachmentsUtils.GetRandomAttachmentsCode(ItemType);
                }
                Ammo = itemType == ItemType.ParticleDisruptor ? byte.MaxValue : ammoCount;
                kills = 0;
                deaths = 0;
                usedBy = 0;
            }
        }

        /// <summary>
        /// The shuffled list of weapons for each GunGame round
        /// </summary>
        public List<Gat> AllWeapons;

        public List<Gat> gats = new List<Gat>();
        public readonly List<Gat> FinalTier = new List<Gat>()
        {
            new Gat(ItemType.GunCOM18, 0b10000100101),
        };

        /// <summary>
        /// The total number of guns this round, INCLUDING the final levels
        /// </summary>
        public byte NumKillsReq => (byte)AllWeapons.Count;

        public readonly List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding
        
        ///<summary>Takes in a list and returns a list of the target length comprising of the input's values</summary>
        public List<Gat> ProcessTier(byte target)
        {
            List<Gat> tier = new List<Gat>();
            for (int i = 0; i < target; i++)
                tier.Add(((WeaponDataWrapper)WeaponData.Wrapper).GetRandomGat());
            return tier;
        }
        #endregion
        
        #region spawning
        public void AssignTeam(Player plr)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial)
                return;
            bool teams = (Tntf < Tchaos) && !FFA;
            if (!AllPlayers.TryGetValue(plr.UserId, out GGPlayer plrInfo))
            {
                var plrComp = plr.GameObject.AddComponent<GGPlayer>();
                plrComp = new GGPlayer(plr, new PlrInfo(teams));
                AllPlayers.Add(plr.UserId, plrComp); 
                RefreshBlocklist += plrComp.BlockSpawn;
            }
            else plrInfo.PlayerInfo.SetTeam(teams);
            if (teams)
                Tntf++;
            else
                Tchaos++;

            var kf = plr.GameObject.AddComponent<KillFeed>();
            kf = new KillFeed(plr);
            SendKills += kf.UpdateFeed;
        }

        public void RemovePlayer(string UId) 
        {
            if (AllPlayers.TryGetValue(UId, out var plrStats))
            {
                plrStats.PlayerInfo.flags &= ~GGPlayerFlags.validFL | GGPlayerFlags.preFL | GGPlayerFlags.finalLevel;
                if (plrStats.PlayerInfo.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                plrStats = null;
                if (!Config.Options.RetainScoreAfterRejoin)
                    AllPlayers.Remove(UId);
            }
        }


        ///<summary>Find's vector's 2d grid position</summary>
        public void LoadSpawns(bool forceAll = false)
        {
            if (!GunsGaming || forceAll)
            {
                LoadingRoom = new Vector3[LRNames.Length];
                for (int z = 0; z < LRNames.Length; z++)
                    if (RoomIdUtils.TryFindRoom(LRNames[z], FacilityZone.None, RoomShape.Undefined, out var foundRoom))
                        LoadingRoom[z] = foundRoom.transform.position + foundRoom.transform.rotation * LROffset[z];
                    else LoadingRoom[z] = new Vector3(-15.5f, 1014.5f, -31.5f);

                BlacklistRooms = new List<RoomIdentifier>();
                foreach (string room in BlacklistRoomNames) //Gets blacklisted room objects for current game
                    BlacklistRooms.AddRange(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)));
            }

            Spawns = new Dictionary<Vector2Int, HashSet<Vector3>>();
            if (zone == FacilityZone.Surface)
                foreach (Vector3 spot in SurfaceSpawns)
                    if (Spawns.ContainsKey(StampGrid(spot, gridSize)))
                        Spawns[StampGrid(spot, gridSize)].Add(spot);
                    else
                        Spawns.Add(StampGrid(spot, gridSize), new HashSet<Vector3>() { spot });

            bool dual = zone == FacilityZone.Other;
            FacilityZone tempZone = !dual ? zone : FacilityZone.HeavyContainment;
            FacilityZone dualtempZone = !dual ? zone : FacilityZone.Entrance;
            foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list
            {
                if (!(door is ElevatorDoor || door is CheckpointDoor) && !door.Rooms.Any(x => BlacklistRooms.Contains(x)) && door.Rooms.Any(z => z.Zone == tempZone || z.Zone == dualtempZone))
                {
                    Vector3 doorpos = door.gameObject.transform.position + new Vector3(0, 1, 0);
                    if (Spawns.ContainsKey(StampGrid(doorpos, gridSize)))
                        Spawns[StampGrid(doorpos, gridSize)].Add(doorpos);
                    else
                        Spawns.Add(StampGrid(doorpos, gridSize), (new HashSet<Vector3>() { doorpos }));
                    door.NetworkTargetState = true;
                }
                else
                {
                    bool lockState = (dual || zone == FacilityZone.LightContainment) && door is CheckpointDoor;
                    door.ServerChangeLock(DoorLockReason.Warhead, lockState);
                    door.NetworkTargetState = lockState;
                }
            }
            RollSpawns(true);
        }

        public bool RollSpawns(Vector3 deathPos) // Current system should be good for Teams, but meh FFA
        {
            if (FFA || Vector2Int.Distance(StampGrid(ChaosSpawn - deathPos, gridSize + 1), Vector2Int.zero) < 1 || Vector2Int.Distance(StampGrid(NTFSpawn - deathPos, gridSize + 1), Vector2Int.zero) < 1)
            {
                return RollSpawns();
            }
            else
            {
                System.Random rnd = new System.Random();
                credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                if (credits >= Mathf.Clamp(Player.Count * 10, 30, 100)) //Rolls next spawns if credits high enough, based on player count
                    return RollSpawns();
            }
            return true;
        }
        public bool RollSpawns(bool skipCheck = false) // Current system should be good for Teams, but meh FFA
        {
            NTFTiles.Clear();
            ChaosTiles.Clear();
            RefreshBlocklist();
            Dictionary<Vector2Int, HashSet<Vector3>> noNTF = Spawns.Where(x => !NTFTiles.Contains(x.Key)).ToDictionary(k => k.Key, v => v.Value);
            Dictionary<Vector2Int, HashSet<Vector3>> noChaos = Spawns.Where(x => !ChaosTiles.Contains(x.Key)).ToDictionary(k => k.Key, v => v.Value);
            if (!noNTF.Any() || (!FFA && !noChaos.Any())) return false;
            var rng = new System.Random();
            credits = 0;
            ChaosSpawn = noNTF.ElementAt(rng.Next(noNTF.Count)).Value.ToList().RandomItem();
            if (FFA)
                return true;
            NTFSpawn = noChaos.ElementAt(rng.Next(noChaos.Count)).Value.ToList().RandomItem();
            Cassie.Message(".G2", false, false, false);
            return true;
        }

        public void SpawnPlayer(Player plr)
        {
            if (!GameInProgress) return;
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return;
            }
            Vector3 deathPosition = plr.Position;
            int level = FFA || plrStats.PlayerInfo.flags.HasFlag(GGPlayerFlags.preFL) ? 3 : Mathf.Clamp((int)Math.Round((double)plrStats.PlayerInfo.Score / AllWeapons.Count * 3), 0, 2);
            plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, (int)(plrStats.PlayerInfo.flags & GGPlayerFlags.NTF)], RoleChangeReason.Respawn, RoleSpawnFlags.None); //Uses bool in 2d array to determine spawn class
            plr.ClearInventory();
            plr.Position = LoadingRoom[((int)zone) - 1];
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>((byte)plrStats.PlayerInfo.killsLeft, 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            plr.ReceiveHint($"\n\nKills left: {plrStats.PlayerInfo.killsLeft}", 5);
            plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(200, 5, false);
            foreach (ItemType ammo in AllAmmo) //Gives ammo of all types
                plr.SetAmmo(ammo, 420);
            plrStats.PlayerInfo.flags |= GGPlayerFlags.alive;
            MEC.Timing.CallDelayed(4, async () =>
            {
                if (!GameInProgress) return;

                for (int t = 5; !RollSpawns(deathPosition) && t > 0; t--)
                    await Task.Delay(1000);

                plr.Position = plrStats.PlayerInfo.IsNtfTeam ? NTFSpawn : ChaosSpawn;
                plrStats.PlayerInfo.flags |= GGPlayerFlags.onMap;

                GiveGun(plr, 1);
                plr.ReferenceHub.playerEffectsController.ChangeState<Invigorated>(127, 5, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<SilentWalk>(8, 9999, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(200, 1, true);
                plr.ReferenceHub.playerEffectsController.ChangeState<Ensnared>(127, 0.5f, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Blinded>(127, 0.1f, false);
            });
        }

        ///<summary>Gives player their next gun and equips it, and removes old gun</summary>
        public void GiveGun(Player plr, float delay = 0)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plrStats.PlayerInfo.flags.HasFlag(GGPlayerFlags.spawned))
                return;

            if (plrStats.PlayerInfo.Score > 0)
                foreach (ItemBase item in plr.Items.ToList()) //Removes last gun(s)                
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.PlayerInfo.Score - 1).ItemType || item.ItemTypeId == AllWeapons.ElementAtOrDefault(plrStats.PlayerInfo.Score).ItemType)
                        plr.RemoveItem(item);
            if (plrStats.PlayerInfo.killsLeft <= 2)
                plrStats.PlayerInfo.flags |= GGPlayerFlags.preFL;

            if (plrStats.PlayerInfo.killsLeft <= 1)
                plrStats.PlayerInfo.flags |= GGPlayerFlags.finalLevel;

            MEC.Timing.CallDelayed(delay, () =>
        {
            Gat currGun = AllWeapons.ElementAt(plrStats.PlayerInfo.Score);
            ItemBase weapon = plr.AddItem(currGun.ItemType);
            if (weapon is Firearm firearm)
            {
                uint attachment_code = AttachmentsUtils.ValidateAttachmentsCode(firearm, currGun.Mod);
                AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                byte ammo_count = currGun.Ammo == 0 ? firearm.AmmoManagerModule.MaxAmmo : currGun.Ammo;
                firearm.Status = new FirearmStatus(ammo_count, FirearmStatusFlags.MagazineInserted | FirearmStatusFlags.Cocked | FirearmStatusFlags.Chambered, attachment_code);
            }
            else
                for (var i = /*currGun.Mod*/2; i > 0 && !plr.IsInventoryFull; i--)
                    plr.AddItem(currGun.ItemType);
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = weapon;
            });
        });
        }

        public void AddScore(Player plr) 
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plr.ReceiveHint("You aren't registered as a player. Try rejoining", 5);
                return;
            }

            switch (plrStats.PlayerInfo.killsLeft)
            {
                case 1:
                    plrStats.PlayerInfo.inc();
                    TriggerWin(plr);
                    return;
                case 2:
                    plrStats.PlayerInfo.flags |= GGPlayerFlags.finalLevel;
                    Cassie.Clear();
                    Cassie.Message("pitch_1.50 .G4", false, false, false);
                    break;
                case 3:
                    plrStats.PlayerInfo.flags |= GGPlayerFlags.preFL;
                    break;
            }
            plr.EffectsManager.EnableEffect<Invigorated>(5, true);
            plrStats.PlayerInfo.inc();
            GiveGun(plr);
        }

        public void RemoveScore(Player plr)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;

            if (plr.IsAlive)
                foreach (ItemBase item in plr.Items) //Removes current weapon(s)
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.PlayerInfo.Score).ItemType)
                        plr.RemoveItem(item);

            if (plrStats.PlayerInfo.Score > 0)
            {
                plrStats.PlayerInfo.Score--;
            }
            if (plr.IsAlive)
                GiveGun(plr);
        }
        #endregion

        #region win sequence
        public void TriggerWin(Player plr)
        {
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plrStats.PlayerInfo.flags.HasFlag(GGPlayerFlags.validFL))
                return;
            DateTime now = DateTime.Now;
            List<PlayerData> playersData = new List<PlayerData>();
            List<string> dnts = new List<string>();
            EndingStats endStats = new EndingStats(FFA, plrStats.PlayerInfo.IsNtfTeam);
            string team = "";
            if (!FFA)
                team = "\n and " + (plrStats.PlayerInfo.IsNtfTeam ? "NTF" : "Chaos") + " helped too I guess";
            SendKills = null;
            Server.ClearBroadcasts();
            Server.SendBroadcast($"<b><color=yellow>{plr.Nickname} wins!</color></b>{team}", 15, shouldClearPrevious: true);
            var bText = $"<b><color=yellow>{plr.Nickname} wins!</color></b>{team}";

            plrStats.PlayerInfo.Score++;
            Server.FriendlyFire = true;
            GunsGaming = false;
            plr.GetStatModule<HealthStat>().CurValue = 42069;
            plr.ReferenceHub.playerEffectsController.DisableAllEffects();
            ChaosSpawn = plr.Position;
            NTFSpawn = plr.Position;

            int plrsLeft = SortedPlayers.Count();
            foreach (var loserEntry in SortedPlayers)
            {
                if (!Player.TryGet(loserEntry.Key, out Player loser))
                    continue;
                var loserData = loserEntry.Value;
                if (loser.IsServer || loser.IsOverwatchEnabled || loser.IsTutorial)
                {
                    plrsLeft--;
                    continue;
                }
                
                loser.ClearInventory();
                if (loser != plr)
                {
                    loser.SetRole(RoleTypeId.ClassD);
                    loser.ReferenceHub.playerEffectsController.EnableEffect<CardiacArrest>();
                    loser.Position = plr.Position;
                }

                var place = SortedPlayers.Count() - plrsLeft + 1;

                loser.SendBroadcast(bText + $"\n\n(You came in {place}{ordinal(place)} place)", 15);

                PlayerData plrDat = new PlayerData(loser) { Kills = loserData.PlayerInfo.totKills, Deaths = loserData.PlayerInfo.totDeaths, GamesPlayed = 1};
                endStats.processPlayer(plrDat, place, loserData.IsNTF);
                    if (!loser.DoNotTrack)
                        playersData.Add(plrDat);
                plrsLeft--;
            }
            Plugin.PlayerStats.AllPlayerData.AddScores(playersData);
            ((WeaponDataWrapper)WeaponData.Wrapper).UpdateRankings(AllWeapons, Convert.ToSingle(SortedPlayers.Values.Average(x => x.PlayerInfo.totKills / x.PlayerInfo.totDeaths)));
            GunGameDataManager.SaveData(WeaponData);
            Server.SendBroadcast(endStats.RoundScreen1(), 10);
            Server.SendBroadcast(endStats.RoundScreen2(), 10);
            Firearm firearm = plr.AddItem(ItemType.GunLogicer) as Firearm;
            AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x1881, true);
            firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x1881);
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = firearm;
            });
            MEC.Timing.CallDelayed(15, () =>
            {
                Round.IsLocked = false;
                Warhead.Detonate();

                MEC.Timing.CallDelayed(15, () =>
                {
                    RoundSummary.singleton.ForceEnd();
                });
            });
        }

        private class EndingStats
        {
            private enum winningTeam
            {
                FFA,
                NTF,
                Chaos
            }
            winningTeam team;
            List<string> NTF;
            List<string> Chaos;

            public EndingStats(bool ffa, bool winT)
            {
                if (ffa)
                    team = winningTeam.FFA;
                else
                    team = winT ? winningTeam.NTF : winningTeam.Chaos;
                NTF = new List<string>();
                Chaos = new List<string>();
            }

            public void processPlayer(PlayerData plr, int pos, bool isNTF)
            {
                string line = $"{pos}{ordinal(pos)}: {plr.Nickname}\tK/D: {plr.Kills}/{plr.Deaths}";
                if (isNTF && team != winningTeam.FFA)
                    NTF.Add(line);
                else Chaos.Add(line);
            }
            public string RoundScreen1()
            {
                switch (team)
                {
                    case winningTeam.NTF:
                        return $"Results:\n<color=blue>NTF:\n{string.Join("\n", NTF.Take(3))}";
                    case winningTeam.Chaos:
                        return $"Results:\n<color=green>Chaos:\n{string.Join("\n", Chaos.Take(3))}";
                }
                return "Results:\n" + string.Join("\n", Chaos);
            }
            public string RoundScreen2()
            {
                switch (team)
                {
                    case winningTeam.NTF:
                        return $"<color=green>Chaos:\n{string.Join("\n", Chaos.Take(3))}";
                    case winningTeam.Chaos:
                        return $"<color=blue>NTF:\n{string.Join("\n", NTF.Take(3))}";
                }
                return string.Join("\n", Chaos.Skip(3).Take(3));
            }
            public string getRoundScreen()
            {
                switch (team)
                {
                    case winningTeam.NTF:
                        return $"Results:\n<color=blue>NTF:\n{string.Join("\n", NTF)}</color>\n<color=green>Chaos:\n{string.Join("\n", Chaos)}</color>";
                    case winningTeam.Chaos:
                        return $"Results:\n<color=green>Chaos:\n{string.Join("\n", Chaos)}</color>\n<color=blue>NTF:\n{string.Join("\n", NTF)}</color>";
                }
                return "Results:\n" + string.Join("\n", Chaos);
            }
        }
        #endregion
    }
}
