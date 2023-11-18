using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.MarshmallowMan;
using InventorySystem.Items.ToggleableLights.Lantern;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GunGame.Plugin;

namespace GunGame
{
    public class GunGameUtils
    {
        public static bool GameStarted;
        public static bool FFA;
        public static FacilityZone zone;
        public static byte credits;
        public static byte Tntf = 0; //Number of NTF
        public static byte Tchaos = 0; //Number of chaos

        public static short[] InnerPosition;
        public static Dictionary<string, PlrInfo> AllPlayers; //UserID, PlrInfo
        public static Dictionary<string, PlrInfo> SortedPlayers => AllPlayers.OrderByDescending(pair => pair.Value.Score)
                                         .ThenBy(pair => pair.Value.InnerPos)
                                         .ToDictionary(pair => pair.Key, pair => pair.Value);
        public static int gridSize => FFA || zone.Equals(FacilityZone.Surface) ? 4 : 5;
        public static List<RoomIdentifier> BlacklistRooms;
        public static Dictionary<Vector3, Vector2Int> Spawns;
        public static Vector3 NTFSpawn; //Current NTF spawn
        public static Vector3 ChaosSpawn; //Current chaos spawn
        public static Vector3[] LoadingRoom;

        /// <summary>
        /// The shuffled list of weapons for each GunGame round
        /// </summary>
        public static List<Gat> AllWeapons;

        public readonly List<Gat> Tier1 = new List<Gat>() { //Hex for attachment code much more compact than binary
            new Gat(ItemType.Jailbird, 1),

            new Gat(ItemType.GunCom45, 1, 255), //What da dog doin?

            new Gat(ItemType.GunLogicer, 0x1881, 255), //Run n gun
            
            new Gat(ItemType.GunShotgun, 0x452), //The Viper

            new Gat(ItemType.GunCrossvec, 0xA054), //Where's that D boy?

            new Gat(ItemType.GunE11SR, 0x542504), //The Sleek

            new Gat(ItemType.GunRevolver, 0x452), //The Head-popper

            new Gat(ItemType.GunAK, 0x41422), //I hope you can aim!

            new Gat(ItemType.GunFRMG0, 0x19102, 255), //Lawn Mower
        };

        public readonly List<Gat> Tier2 = new List<Gat>() {
           // new Gat(ItemType.SCP330), // pink candy
           // new Gat(ItemType.MicroHID, 1),

            new Gat(ItemType.GunCrossvec, 0x84A1), //Faster than reloading!

            new Gat(ItemType.GunE11SR, 0x521241), //Rambo style

            new Gat(ItemType.GunAK, 0x24301, 200), //Rambo 2 electric boogaloo

            new Gat(ItemType.GunCOM18, 0x44A), //Heavy armory moment

            new Gat(ItemType.GunRevolver, 0x18A, 30), //It's high noon

            new Gat(ItemType.GunFSP9, 0x2922), //D-boy genocide time!
        };

        public readonly List<Gat> Tier3 = new List<Gat>() {
            new Gat(ItemType.GunA7, 1),

            new Gat(ItemType.GunShotgun, 0x245), //Spray n pray

            new Gat(ItemType.GunE11SR, 0x110A510), //Silent but deadly

            new Gat(ItemType.GunFRMG0, 0x24841), //100 round mag go brrr

            new Gat(ItemType.ParticleDisruptor, 1, 69),

            new Gat(ItemType.GunCrossvec), //Random
            new Gat(ItemType.GrenadeHE, 4),

            new Gat(ItemType.GunAK), //Random
        };

        public readonly List<Gat> Tier4 = new List<Gat>() {
            new Gat(ItemType.GunCom45, 1),

            new Gat(ItemType.GunCOM18), //Random

            new Gat(ItemType.GunFSP9), //Random

            new Gat(ItemType.GunRevolver), //Random
        };
        public readonly List<Gat> FinalTier = new List<Gat>()
        {
            new Gat(ItemType.GunCOM15),
            new Gat(ItemType.Lantern) //Bonk Stick
            //new Gat(ItemType.Jailbird)
        };

        /// <summary>
        /// The number of possible guns, EXCLUDING the final levels
        /// </summary>
        public byte NumGuns => (byte)(Tier1.Count + Tier2.Count + Tier3.Count + Tier4.Count);
        /// <summary>
        /// The total number of guns this round, INCLUDING the final levels
        /// </summary>
        public byte NumKillsReq => (byte)AllWeapons.Count;
        public int currRound = -1;// RoundsPlayed + 1;

        public readonly RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.ChaosRepressor, RoleTypeId.NtfCaptain }, { RoleTypeId.ChaosMarauder, RoleTypeId.NtfSergeant }, { RoleTypeId.ChaosConscript, RoleTypeId.NtfPrivate }, { RoleTypeId.ClassD, RoleTypeId.Scientist } }; //Player visual levels
        public readonly List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", /*"LczClassDSpawn",*/ "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106", "Hcz079", "Lcz173" };
        public readonly Vector3[] LROffset = new Vector3[] { new Vector3(3, 15.28f, 7.8f), new Vector3(-3.5f, -4.28f, -12), new Vector3(6f, 3.83f, -3.5f), new Vector3(-13, 14.46f, -33f), new Vector3(0, 0.96f, -1.5f), };
        public readonly RoomName[] LRNames = new RoomName[] { RoomName.Lcz173, RoomName.Hcz079, RoomName.EzGateB, RoomName.Outside, RoomName.EzEvacShelter };
        public readonly List<Vector3> SurfaceSpawns = new List<Vector3> { new Vector3(0f, 1001f, 0f), new Vector3(8f, 991.65f, -43f), new Vector3(10.5f, 997.47f, -23.25f), new Vector3(63.5f, 995.69f, -34f), new Vector3(63f, 991.65f, -51f), new Vector3(114.5f, 995.45f, -43f), new Vector3(137f, 995.46f, -60f), new Vector3(38.5f, 1001f, -52f), new Vector3(0f, 1001f, -42f) }; //Extra surface spawns
        public readonly List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding

        public static Func<int, string> ordinal = i => (i >= 11 && i <= 13) ? "th"
                                : (i % 10 == 1) ? "st"
                                : (i % 10 == 2) ? "nd"
                                : (i % 10 == 3) ? "rd"
                                : "th";
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
        public Vector2Int StampGrid(Vector3 spawn, int shift = 5) => new Vector2Int((int)spawn.x >> shift, (int)spawn.z >> shift); // Divide by 2^shift

        public GunGameUtils(bool ffa = false, FacilityZone targetZone = FacilityZone.LightContainment, int numKills = 20)
        {
            FFA = ffa;
            zone = targetZone;
            SetNumWeapons((byte)Mathf.Clamp(numKills, 2, 255));

            GameStarted = false;
            LoadSpawns(true);
            InnerPosition = new short[256];// [AllWeapons.Count + 2];
            AllPlayers = new Dictionary<string, PlrInfo>();
            Tntf = 0;
            Tchaos = 0;
            credits = 0;
        }

        [Flags]
        public enum GGPlayerFlags : byte
        {
            NTF = 1,
            alive = 2,
            onMap = 4,
            spawned = alive | onMap,
            preFL = 8,
            finalLevel = 16,
            validFL = finalLevel | preFL | spawned,
        }
        public class PlrInfo
        {
            public GGPlayerFlags flags;
            public byte Score { get; set; }
            public short InnerPos { get; set; }
            public bool IsNtfTeam => flags.HasFlag(GGPlayerFlags.NTF);
            public int killsLeft => GunGameEventCommand.GG.NumKillsReq - Score;// - (flags.HasFlag(GGPlayerFlags.validFL) ? 1 : 0);
            public void SetTeam(bool NTF) => flags = flags & ~GGPlayerFlags.NTF | (NTF ? GGPlayerFlags.NTF : 0);
            public void inc() => InnerPos = ++InnerPosition[++Score];

            public PlrInfo(bool isNtfTeam, byte score = 0)
            {
                SetTeam(isNtfTeam);
                Score = score;
                InnerPos = ++InnerPosition[score];
            }
        }

        public class Gat
        {
            public ItemType ItemType { get; set; }
            public uint Mod { get; set; } = 0;
            public byte Ammo { get; set; } = 0;

            public Gat(ItemType itemType, uint modCode = 0, byte ammoCount = 0)
            {
                ItemType = itemType;
                Mod = modCode;
                Ammo = ammoCount;
            }
        }

        public Dictionary<string, int> Positions()
        {
            var list = new Dictionary<string, int>();
            foreach (var item in SortedPlayers)
                if (Player.TryGet(item.Key, out Player plr))
                    list.Add(plr.Nickname, item.Value.Score);

            return list;
        }

        public void SetNumWeapons(byte num = 20)
        {
            num -= 2; //Excludes the final levels in shuffle target
            byte T1 = (byte)Math.Round((double)Tier1.Count / NumGuns * num);
            byte T2 = (byte)Math.Round((double)Tier2.Count / NumGuns * num);
            byte T3 = (byte)Math.Round((double)Tier3.Count / NumGuns * num);
            byte T4 = (byte)(num - T1 - T2 - T3);

            AllWeapons = ProcessTier(Tier1, T1).Concat(ProcessTier(Tier2, T2)).Concat(ProcessTier(Tier3, T3)).Concat(ProcessTier(Tier4, T4)).Concat(FinalTier).ToList();
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
        public void LoadSpawns(bool forceAll = false)
        {
            if (!GameStarted || forceAll)
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

            Spawns = new Dictionary<Vector3, Vector2Int>();
            if (zone == FacilityZone.Surface)
                foreach (Vector3 spot in SurfaceSpawns)
                    Spawns.Add(spot, StampGrid(spot));

            bool dual = zone == FacilityZone.Other;
            FacilityZone tempZone = !dual ? zone : FacilityZone.HeavyContainment;
            FacilityZone dualtempZone = !dual ? zone : FacilityZone.Entrance;
            foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list
            {
                if (!(door is ElevatorDoor || door is CheckpointDoor) && !door.Rooms.Any(x => BlacklistRooms.Contains(x)) && door.Rooms.Any(z => z.Zone == tempZone || z.Zone == dualtempZone))
                {
                    Vector3 doorpos = door.gameObject.transform.position + new Vector3(0, 1, 0);
                    Spawns.Add(doorpos, StampGrid(doorpos));
                    door.NetworkTargetState = true;
                }
                else
                {
                    bool lockState = (dual || zone == FacilityZone.LightContainment) && door is CheckpointDoor;
                    door.ServerChangeLock(DoorLockReason.Warhead, lockState);
                    door.NetworkTargetState = lockState;
                }
            }
            RollSpawns();
        }
        public void RollSpawns(Vector3 deathPos) // Current system should be good for Teams, but meh FFA
        {
            if (FFA || Vector2Int.Distance(StampGrid(ChaosSpawn - deathPos, gridSize + 1), Vector2Int.zero) < 1 || Vector2Int.Distance(StampGrid(NTFSpawn - deathPos, gridSize + 1), Vector2Int.zero) < 1)
                RollSpawns();
            else
            {
                System.Random rnd = new System.Random();
                credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                if (credits >= Mathf.Clamp(Player.Count * 10, 30, 100)) //Rolls next spawns if credits high enough, based on player count
                    RollSpawns();
            }
        }
        public void RollSpawns() // Current system should be good for Teams, but meh FFA
        {
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
            foreach (Player plr in Player.GetPlayers())
                if (plr.IsAlive)
                    blocked.Add(StampGrid(plr.Position, gridSize));

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
        }

        public void AssignTeam(Player plr) //Assigns player to team
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial)
                return;
            bool teams = (Tntf < Tchaos) && !FFA;
            if (!AllPlayers.TryGetValue(plr.UserId, out PlrInfo plrInfo))
                AllPlayers.Add(plr.UserId, new PlrInfo(teams)); //Adds player to list, uses bool operations to determine teams
            else plrInfo.SetTeam(teams);
            if (teams)
                Tntf++;
            else
                Tchaos++;
        }

        public void RemovePlayer(Player plr) //Removes player from list
        {
            if (AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plrStats.flags &= ~GGPlayerFlags.validFL | GGPlayerFlags.preFL;
                if (plrStats.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                //AllPlayers.Remove(plr.UserId);
            }
        }

        public void SpawnPlayer(Player plr) //Spawns player
        {
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return;
            }
            Vector3 deathPosition = plr.Position;
            //if (plrStats.flags.HasFlag(GGPlayerFlags.alive))
            //    return;
            int level = FFA||plrStats.flags.HasFlag(GGPlayerFlags.preFL) ? 3 : Mathf.Clamp((int)Math.Round((double)plrStats.Score / AllWeapons.Count * 3), 0, 2);
            plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, (int)(plrStats.flags & GGPlayerFlags.NTF)], RoleChangeReason.Respawn, RoleSpawnFlags.None); //Uses bool in 2d array to determine spawn class
            plr.ClearInventory();
            plr.Position = LoadingRoom[((int)zone) - 1];
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>((byte)plrStats.killsLeft, 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            //plr.AddItem(ItemType.Radio);
            plr.SendBroadcast($"Kills left: {plrStats.killsLeft}", 5);
            plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(200, 5, false);
            foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
                plr.SetAmmo(ammo, 420);
            plrStats.flags |= GGPlayerFlags.alive;
            MEC.Timing.CallDelayed(4, () =>
            {
                RollSpawns(deathPosition);
                plrStats.flags |= GGPlayerFlags.onMap;
                plr.Position = plrStats.IsNtfTeam ? NTFSpawn : ChaosSpawn;
            plr.AddItem(ItemType.Flashlight);
                GiveGun(plr, 1.5f);
                plr.ReferenceHub.playerEffectsController.ChangeState<Invigorated>(127, 5, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(100, 3, true);
                plr.ReferenceHub.playerEffectsController.ChangeState<Invisible>(127, 2, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Ensnared>(127, 1, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Blinded>(127, 0.5f, false);
            });
        }

        ///<summary>Gives player their next gun and equips it, and removes old gun</summary>
        public void GiveGun(Player plr, float delay = 0)
        {
            
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plrStats.flags.HasFlag(GGPlayerFlags.spawned))
                return;

            if (plrStats.Score > 0)
                foreach (ItemBase item in plr.Items.ToList()) //Removes last gun(s)                
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.Score - 1).ItemType || item.ItemTypeId == AllWeapons.ElementAtOrDefault(plrStats.Score).ItemType)
                        plr.RemoveItem(item);
            if (plrStats.killsLeft <= 2)
                plrStats.flags |= GGPlayerFlags.preFL;
            if (plrStats.flags.HasFlag(GGPlayerFlags.finalLevel))
            {
                plr.RemoveItems(ItemType.Flashlight);
            }
            MEC.Timing.CallDelayed(delay, () =>
        {
            Gat currGun = AllWeapons.ElementAt(plrStats.Score);
            ItemBase weapon = plr.AddItem(currGun.ItemType);
            if (weapon is Firearm firearm)
            {
                uint attachment_code = currGun.Mod == 0 ? AttachmentsUtils.GetRandomAttachmentsCode(firearm.ItemTypeId) : currGun.Mod; //Random attachments if no attachment code specified
                AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                byte ammo_count = currGun.Ammo == 0 ? firearm.AmmoManagerModule.MaxAmmo : currGun.Ammo;
                firearm.Status = new FirearmStatus(ammo_count, FirearmStatusFlags.MagazineInserted | FirearmStatusFlags.Cocked | FirearmStatusFlags.Chambered, attachment_code);
            }
            /*else if (weapon is Scp330Bag bag)
            {
                List<CandyKindID> bagCandies = new List<CandyKindID>();
                while (bagCandies.Count < 5)
                    bagCandies.Add((CandyKindID)new System.Random().Next(1, 7));
                bagCandies.Add(CandyKindID.Pink);
                bagCandies.ShuffleList();
                bag.Candies = bagCandies;
                bag.ServerRefreshBag();
            }*/
            else
                for (var i = currGun.Mod; i > 0 && !plr.IsInventoryFull; i--)
                    plr.AddItem(currGun.ItemType);

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

            switch (plrStats.killsLeft)
            {
                case 1:
                    plrStats.inc(); //Adds 1 to score
                    TriggerWin(plr);
                    return;
                case 2:
                    plrStats.flags |= GGPlayerFlags.finalLevel;
                    //plr.ReferenceHub.roleManager.ServerSetRole(FFA ? RoleTypeId.FacilityGuard : Roles[3, (int)(plrStats.flags & GGPlayerFlags.NTF)], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                    //plr.Damage(plr.Health/4, "krill issue (your health was too low somehow)");
                    Cassie.Clear();
                    Cassie.Message("pitch_1.50 .G4", false, false, false);
                    break;
                case 3:
                    plrStats.flags |= GGPlayerFlags.preFL;
                    break;
            }
            plr.EffectsManager.EnableEffect<Invigorated>(5, true);
            plrStats.inc(); //Adds 1 to score
            GiveGun(plr);
            plr.SendBroadcast($"{plrStats.killsLeft}", 1);
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
            }
                if (plr.IsAlive)
                    GiveGun(plr);
        }

        public void TriggerWin(Player plr) //Win sequence
        {
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plrStats.flags.HasFlag(GGPlayerFlags.validFL))
                return;
            DateTime now = DateTime.Now;
            RoundData round = new RoundData(currRound, FFA, zone, NumKillsReq, now, out string roundID);
            List<PlayerData> playersData = new List<PlayerData>();
            List<ScoreData> scores = new List<ScoreData>();
            List<string> dnts = new List<string>();
            EndingStats endStats = new EndingStats(FFA, plrStats.IsNtfTeam);
            string team = "";
            if (!FFA)
                team = "\n and " + (plrStats.IsNtfTeam ? "NTF" : "Chaos") + " helped too I guess";
            //Server.SendBroadcast($"<b><color=yellow>{plr.Nickname} wins!</color></b>{team}", 15, shouldClearPrevious: true);
            var bText = $"<b><color=yellow>{plr.Nickname} wins!</color></b>{team}";

            plrStats.Score++;
            Server.FriendlyFire = true;
            GameInProgress = false;
            //plr.Health = 42069; //Broke after 13.2 update
            plr.GetStatModule<HealthStat>().CurValue = 42069;
            plr.ReferenceHub.playerEffectsController.DisableAllEffects();
            ChaosSpawn = plr.Position;
            NTFSpawn = plr.Position;

            int plrsLeft = SortedPlayers.Count();
            foreach (var loserEntry in SortedPlayers)
            {
                if (!Player.TryGet(loserEntry.Key, out Player loser))
                    continue;
                if (loser.IsServer || loser.IsOverwatchEnabled || loser.IsTutorial)
                {
                    plrsLeft--;
                    continue;
                }
                //int teamBonus = (!FFA && (loserEntry.Value.IsNtfTeam == plrStats.IsNtfTeam)) ? 2 : 0;
                int teamBonus = FFA ? 0 : (int)((~loserEntry.Value.flags ^ plrStats.flags) & GGPlayerFlags.NTF) * 2;
                int positionScore = Convert.ToInt32((double)(plrsLeft + 1) / SortedPlayers.Count() * 13);

                loser.ClearInventory();
                if (loser != plr)
                {
                    loser.SetRole(RoleTypeId.ClassD);
                    loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                    loser.Position = plr.Position;
                }
                else positionScore = 15;

                var totp = positionScore + teamBonus;
                var plce = SortedPlayers.Count() - plrsLeft + 1;

                loser.SendBroadcast(bText + $"\n\n(You came in {plce}{ordinal(plce)} place and got {totp} points)", 15);

                PlayerData plrDat = new PlayerData(loser);
                ScoreData scrDat = new ScoreData(loser.UserId, roundID, totp, plce, loserEntry.Value.IsNtfTeam);
                endStats.processPlayer(plrDat, scrDat);
                if (loser.DoNotTrack)
                    dnts.Add(loser.UserId);
                else
                {
                    playersData.Add(plrDat);
                    scores.Add(scrDat);
                }
                plrsLeft--;
            }
            GunGameDataManager.AddScores(playersData, scores, round);
            GunGameDataManager.UserScrub(dnts);
            Server.SendBroadcast(endStats.getRoundScreen() + "\n(Type \".ggScores\" in your console to see the leaderboard)", 15);
            Firearm firearm = plr.AddItem(ItemType.GunLogicer) as Firearm;
            AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x1881, true);
            firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x1881);
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = firearm;
            });
            MEC.Timing.CallDelayed(30, () =>
            {
                Round.IsLocked = false;
                Warhead.Detonate();
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

            public void processPlayer(PlayerData plr, ScoreData scr)
            {
                string line = $"{scr.Position}{ordinal(scr.Position)}:\t(+{scr.Score}) {plr.Nickname}";
                if (scr.NTF && team != winningTeam.FFA)
                    NTF.Add(line);
                else Chaos.Add(line);
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
    }
}
