using CustomPlayerEffects;
using GunGame.Components;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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


        #region map
        public static Dictionary<Vector2Int, HashSet<Vector3>> Spawns;
        public static Vector3 NTFSpawn; //Current NTF spawn
        public static Vector3 ChaosSpawn; //Current chaos spawn
        public readonly List<Vector3> SurfaceSpawns = new List<Vector3> { new Vector3(0f, 1001f, 0f), new Vector3(8f, 991.65f, -43f), new Vector3(10.5f, 997.47f, -23.25f), new Vector3(63.5f, 995.69f, -34f), new Vector3(63f, 991.65f, -51f), new Vector3(114.5f, 995.45f, -43f), new Vector3(137f, 995.46f, -60f), new Vector3(38.5f, 1001f, -52f), new Vector3(0f, 1001f, -42f) }; //Extra surface spawns
        
        public static int gridSize => FFA || zone.Equals(FacilityZone.Surface) ? 4 : 5;
        public HashSet<Vector2Int> ChaosTiles = new HashSet<Vector2Int>();
        public HashSet<Vector2Int> NTFTiles = new HashSet<Vector2Int>();
        public Action RefreshBlocklist = () => { };
        
        public static List<RoomIdentifier> BlacklistRooms;
        public readonly List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", /*"LczClassDSpawn",*/ "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106", "Hcz079", "Lcz173" };
        
        public static Vector3[] LoadingRoom;
        public readonly Vector3[] LROffset = new Vector3[] { new Vector3(3, 15.28f, 7.8f), new Vector3(-3.5f, -4.28f, -12), new Vector3(6f, 3.83f, -3.5f), new Vector3(-13, 14.46f, -33f), new Vector3(0, 0.96f, -1.5f), };
        public readonly RoomName[] LRNames = new RoomName[] { RoomName.Lcz173, RoomName.Hcz079, RoomName.EzGateB, RoomName.Outside, RoomName.EzEvacShelter };
        
        
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
        #endregion

        #region players
        public static short[] InnerPosition;
        public static Dictionary<string, GGPlayer> AllPlayers; //UserID, PlrInfo
        public static Dictionary<string, GGPlayer> SortedPlayers => AllPlayers.OrderByDescending(pair => pair.Value.PlayerInfo.Score)
                                         .ThenBy(pair => pair.Value.PlayerInfo.InnerPos)
                                         .ToDictionary(pair => pair.Key, pair => pair.Value);
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
            Chaos = 0,
            NTF = 1,
            alive = 2,
            onMap = 4,
            spawned = alive | onMap,
            preFL = 8,
            finalLevel = 16,
            validFL = finalLevel | preFL | spawned,
        }
        public readonly RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.ChaosRepressor, RoleTypeId.NtfCaptain }, { RoleTypeId.ChaosMarauder, RoleTypeId.NtfSergeant }, { RoleTypeId.ChaosConscript, RoleTypeId.NtfPrivate }, { RoleTypeId.ClassD, RoleTypeId.Scientist } }; //Player visual levels
        public class PlrInfo
        {
            public GGPlayerFlags flags;
            public byte Score { get; set; }
            public short InnerPos { get; set; }
            public bool IsNtfTeam => flags.HasFlag(GGPlayerFlags.NTF);
            public int killsLeft => GG.NumKillsReq - Score;// - (flags.HasFlag(GGPlayerFlags.validFL) ? 1 : 0);
            public int totKills = 0;
            public int totDeaths = 0;
            public void SetTeam(bool NTF) => flags = flags & ~GGPlayerFlags.NTF | (NTF ? GGPlayerFlags.NTF : 0);
            public void inc() => InnerPos = ++InnerPosition[++Score];

            public PlrInfo(bool isNtfTeam, byte score = 0)
            {
                SetTeam(isNtfTeam);
                Score = score;
                InnerPos = ++InnerPosition[score];
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

        public static Func<int, string> ordinal = i => (i >= 11 && i <= 13) ? "th"
                                : (i % 10 == 1) ? "st"
                                : (i % 10 == 2) ? "nd"
                                : (i % 10 == 3) ? "rd"
                                : "th";
        #endregion

        #region weapons
        public struct Gat
        {
            public ItemType ItemType;
            public uint Mod;
            public byte Ammo;

            public Gat(ItemType itemType, uint modCode = 0, byte ammoCount = 0)
            {
                ItemType = itemType;
                Mod = modCode;
                Ammo = ammoCount;
            }
        }

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

            //new Gat(ItemType.GunRevolver, 0x452), //The Head-popper

            new Gat(ItemType.GunAK, 0x41422), //I hope you can aim!

            new Gat(ItemType.GunFRMG0, 0x19102, 255), //Lawn Mower
        //};

        //public readonly List<Gat> Tier2 = new List<Gat>() {
           // new Gat(ItemType.SCP330), // pink candy
           // new Gat(ItemType.MicroHID, 1),

            new Gat(ItemType.GunCrossvec, 0x84A1), //Faster than reloading!

            new Gat(ItemType.GunE11SR, 0x521241), //Rambo style

            new Gat(ItemType.GunAK, 0x24301, 200), //Rambo 2 electric boogaloo

            new Gat(ItemType.GunCOM18, 0x44A), //Heavy armory moment

            //new Gat(ItemType.GunRevolver, 0x18A, 30), //It's high noon

            new Gat(ItemType.GunFSP9, 0x2922), //D-boy genocide time!
        //};

        //public readonly List<Gat> Tier3 = new List<Gat>() {
            new Gat(ItemType.GunA7, 1),

            new Gat(ItemType.GunShotgun, 0x245), //Spray n pray

            new Gat(ItemType.GunE11SR, 0x110A510), //Silent but deadly

            new Gat(ItemType.GunFRMG0, 0x24841), //100 round mag go brrr

            new Gat(ItemType.ParticleDisruptor, 1, 69),

            new Gat(ItemType.GunCrossvec), //Random
            //new Gat(ItemType.GrenadeHE, 4),

            new Gat(ItemType.GunAK), //Random
        //};

        //public readonly List<Gat> Tier4 = new List<Gat>() {
            new Gat(ItemType.GunCom45, 1),


            new Gat(ItemType.GunFSP9), //Random

            new Gat(ItemType.GunRevolver), //Random
        };
        public readonly List<Gat> FinalTier = new List<Gat>()
        {
            new Gat(ItemType.GunCOM18), //Random
            //new Gat(ItemType.GunCOM15),
           // new Gat(ItemType.Lantern) //Bonk
            //new Gat(ItemType.Jailbird)
        };

        /// <summary>
        /// The number of possible guns, EXCLUDING the final levels
        /// </summary>
        public byte NumGuns => (byte)(Tier1.Count);// + Tier2.Count + Tier3.Count + Tier4.Count);
        /// <summary>
        /// The total number of guns this round, INCLUDING the final levels
        /// </summary>
        public byte NumKillsReq => (byte)AllWeapons.Count;
        //public int currRound = -1;// RoundsPlayed + 1;

        public readonly List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding
        #endregion


        public GunGameUtils(bool ffa = false, FacilityZone targetZone = FacilityZone.LightContainment, int numKills = 20)
        {
            FFA = ffa;
            zone = targetZone;
            AllWeapons = ProcessTier(Tier1, (byte)(Mathf.Clamp(numKills, 2, 255) - 2)).Concat(FinalTier).ToList();

            GameStarted = false;
            LoadSpawns(true);
            InnerPosition = new short[256];// [AllWeapons.Count + 2];
            AllPlayers = new Dictionary<string, GGPlayer>();
            Tntf = 0;
            Tchaos = 0;
            credits = 0;
            KillList = new List<KillInfo>();
        }

        public void Start()
        {
            GameInProgress = true;
            GameStarted = true;
            foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
            {
                if (plr.IsServer)
                    continue;
                AssignTeam(plr);
                SpawnPlayer(plr);

                // if (plr.DoNotTrack)
                //     plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.\nAny existing scores will be deleted as well.</color>", 15);
            }
            Server.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \nRace to the final weapon!", 10, shouldClearPrevious: true);

            if (zone == FacilityZone.Surface && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244a, out var gma) && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out var gpa)) //SCP244 obsticals on surface
            {
                /*ExplosionUtils.ServerExplode(new Vector3(72f, 992f, -43f), new Footprint()); //Bodge to get rid of old grandma's if the round didn't restart
                ExplosionUtils.ServerExplode(new Vector3(11.3f, 997.47f, -35.3f), new Footprint());*/

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
        ///<summary>Takes in a list and returns a list of the target length comprising of the input's values</summary>
        public List<Gat> ProcessTier(List<Gat> tier, byte target)
        {
            List<Gat> outTier = tier.OrderBy(w => Guid.NewGuid()).Take(target).ToList();
            var additionalCount = target - tier.Count;
            if (additionalCount > 0)
            {
                System.Random rnd = new System.Random();
                while (additionalCount > 0)
                {
                    outTier.Add(tier[rnd.Next(tier.Count)]);
                    additionalCount--;
                }
            }
            return outTier;
        }

        #region spawning
        public void AssignTeam(Player plr) //Assigns player to team
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial)
                return;
            bool teams = (Tntf < Tchaos) && !FFA;
            if (!AllPlayers.TryGetValue(plr.UserId, out GGPlayer plrInfo))
            {
                var plrComp = plr.GameObject.AddComponent<GGPlayer>();
                plrComp = new GGPlayer(plr, new PlrInfo(teams));
                AllPlayers.Add(plr.UserId, plrComp); //Adds player to list, uses bool operations to determine teams
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
        public void RemovePlayer(Player plr) //Removes player from list
        {
            if (AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plrStats.PlayerInfo.flags &= ~GGPlayerFlags.validFL | GGPlayerFlags.preFL | GGPlayerFlags.finalLevel;
                if (plrStats.PlayerInfo.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                //AllPlayers.Remove(plr.UserId);
            }
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
            RefreshBlocklist();
            Dictionary<Vector2Int, HashSet<Vector3>> noNTF = Spawns.Where(x => !ChaosTiles.Contains(x.Key)).ToDictionary(k => k.Key, v => v.Value);
            Dictionary<Vector2Int, HashSet<Vector3>> noChaos = Spawns.Where(x => !NTFTiles.Contains(x.Key)).ToDictionary(k => k.Key, v => v.Value);
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

        public void SpawnPlayer(Player plr) //Spawns player
        {
            if (!GameInProgress) return;
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return;
            }
            Vector3 deathPosition = plr.Position;
            //if (plrStats.flags.HasFlag(GGPlayerFlags.alive))
            //    return;
            int level = FFA || plrStats.PlayerInfo.flags.HasFlag(GGPlayerFlags.preFL) ? 3 : Mathf.Clamp((int)Math.Round((double)plrStats.PlayerInfo.Score / AllWeapons.Count * 3), 0, 2);
            plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, (int)(plrStats.PlayerInfo.flags & GGPlayerFlags.NTF)], RoleChangeReason.Respawn, RoleSpawnFlags.None); //Uses bool in 2d array to determine spawn class
            plr.ClearInventory();
            plr.Position = LoadingRoom[((int)zone) - 1];
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>((byte)plrStats.PlayerInfo.killsLeft, 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            //plr.AddItem(ItemType.Radio);
            plr.ReceiveHint($"\n\nKills left: {plrStats.PlayerInfo.killsLeft}", 5);
            plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(200, 5, false);
            foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
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
                //plr.ReferenceHub.playerEffectsController.ChangeState<Invisible>(127, 2, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Ensnared>(127, 1, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Blinded>(127, 0.5f, false);
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

            //plr.RemoveItems(ItemType.Flashlight);

            MEC.Timing.CallDelayed(delay, () =>
        {
            Gat currGun = AllWeapons.ElementAt(plrStats.PlayerInfo.Score);
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
            /*  if (!plrStats.flags.HasFlag(GGPlayerFlags.finalLevel))
                  plr.AddItem(ItemType.Flashlight);*/
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

            switch (plrStats.PlayerInfo.killsLeft)
            {
                case 1:
                    plrStats.PlayerInfo.inc(); //Adds 1 to score
                    TriggerWin(plr);
                    return;
                case 2:
                    plrStats.PlayerInfo.flags |= GGPlayerFlags.finalLevel;
                    //plr.ReferenceHub.roleManager.ServerSetRole(FFA ? RoleTypeId.FacilityGuard : Roles[3, (int)(plrStats.flags & GGPlayerFlags.NTF)], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                    //plr.Damage(plr.Health/4, "krill issue (your health was too low somehow)");
                    Cassie.Clear();
                    Cassie.Message("pitch_1.50 .G4", false, false, false);
                    break;
                case 3:
                    plrStats.PlayerInfo.flags |= GGPlayerFlags.preFL;
                    break;
            }
            plr.EffectsManager.EnableEffect<Invigorated>(5, true);
            plrStats.PlayerInfo.inc(); //Adds 1 to score
            GiveGun(plr);
            //plr.SendBroadcast($"{plrStats.killsLeft}", 1);
        }

        public void RemoveScore(Player plr)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;

            if (plr.IsAlive)
                foreach (ItemBase item in plr.Items) //Removes last gun
                {
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.PlayerInfo.Score).ItemType)
                    {
                        plr.RemoveItem(item);
                        break;
                    }
                }

            if (plrStats.PlayerInfo.Score > 0)
            {
                plrStats.PlayerInfo.Score--;
            }
            if (plr.IsAlive)
                GiveGun(plr);
        }
        #endregion
        public void TriggerWin(Player plr) //Win sequence
        {
            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats) || !plrStats.PlayerInfo.flags.HasFlag(GGPlayerFlags.validFL))
                return;
            //DateTime now = DateTime.Now;
            //RoundData round = new RoundData(currRound, FFA, zone, NumKillsReq, now, out string roundID);
            //List<PlayerData> playersData = new List<PlayerData>();
            //List<ScoreData> scores = new List<ScoreData>();
            //List<string> dnts = new List<string>();
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
                //    int teamBonus = FFA ? 0 : (int)((~loserEntry.Value.flags ^ plrStats.flags) & GGPlayerFlags.NTF) * 2;
                //    int positionScore = Convert.ToInt32((double)(plrsLeft + 1) / SortedPlayers.Count() * 13);

                loser.ClearInventory();
                if (loser != plr)
                {
                    loser.SetRole(RoleTypeId.ClassD);
                    loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                    loser.Position = plr.Position;
                }
                //    else positionScore = 15;

                //    var totp = positionScore + teamBonus;
                var plce = SortedPlayers.Count() - plrsLeft + 1;

                loser.SendBroadcast(bText + $"\n\n(You came in {plce}{ordinal(plce)} place)"/* and got {totp} points)"*/, 15);

                PlayerData plrDat = new PlayerData(loser);
                ScoreData scrDat = new ScoreData(loser.UserId, loserEntry.Value.PlayerInfo.totKills, loserEntry.Value.PlayerInfo.totDeaths, plce, loserEntry.Value.PlayerInfo.IsNtfTeam);
                endStats.processPlayer(plrDat, scrDat);
                /*    if (loser.DoNotTrack)
                        dnts.Add(loser.UserId);
                    else
                    {
                        playersData.Add(plrDat);
                        scores.Add(scrDat);
                    }*/
                plrsLeft--;
            }
            //GunGameDataManager.AddScores(playersData, scores, round);
            //GunGameDataManager.UserScrub(dnts);
            Server.SendBroadcast(endStats.RoundScreen1(), 10);
            Server.SendBroadcast(endStats.RoundScreen2(), 10);// + "\n(Type \".ggScores\" in your console to see the leaderboard)", 10);
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

                MEC.Timing.CallDelayed(10, () =>
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

            public void processPlayer(PlayerData plr, ScoreData scr)
            {
                string line = $"{scr.Position}{ordinal(scr.Position)}: "/*\t(+{scr.Score})*/ + $"{plr.Nickname}\tK/D: {scr.kills}/{scr.deaths}";
                if (scr.NTF && team != winningTeam.FFA)
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
    }
}
