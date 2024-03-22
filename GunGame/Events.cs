using CustomPlayerEffects;
using Footprinting;
using GunGame.HitRegModules;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using Scp914;
using System;
using System.Linq;
using UnityEngine;
using Utils;
using static GunGame.GunGameEventCommand;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame
{
    public class Events
    {
        [PluginEvent(ServerEventType.RoundRestart)]
        public void OnRoundRestart()
        {
            GameInProgress = false;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart(RoundStartEvent args)
        {
            System.Random rnd = new System.Random();
            var plrCount = Player.GetPlayers().Count();
            bool ffa = rnd.Next(0, 1) == 1;
            FacilityZone trgtZone = FacilityZone.LightContainment;
            switch (rnd.Next(1, 5))
            {
                case 2:
                    trgtZone = FacilityZone.HeavyContainment;
                    break;
                case 3:
                    trgtZone = FacilityZone.Entrance;
                    break;
                case 4:
                    trgtZone = FacilityZone.Surface;
                    break;
                case 5:
                    if (plrCount > 20)
                        trgtZone = FacilityZone.Other;
                    break;
            }

            int trgtKills = Mathf.Clamp(plrCount * 5 - 10, 10, 30);
            GG = new GunGameUtils(ffa, trgtZone, trgtKills);

            foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
            {
                if (plr.IsServer)
                    continue;
                GG.AssignTeam(plr);
                GG.SpawnPlayer(plr);

                if (plr.DoNotTrack)
                    plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.\nAny existing scores will be deleted as well.</color>", 15);
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
            GameInProgress = true;
            Round.IsLocked = true;
            DecontaminationController.Singleton.enabled = false;
            Round.Start();
            Server.FriendlyFire = FFA;
            GameStarted = true;
        }

        [PluginEvent(ServerEventType.PlayerToggleFlashlight)]
        public void KnifeStab(PlayerToggleFlashlightEvent args)
        {
            if (GameInProgress && (args.Item.ItemTypeId == ItemType.Flashlight || args.Item.ItemTypeId == ItemType.Lantern) || args.Player.IsTutorial)
            {

                bool anyDamaged = Bonk.Bonketh(args.Player);
                if (anyDamaged)
                {
                    Hitmarker.SendHitmarkerDirectly(args.Player.ReferenceHub, 1f);
                }
                //var stab = new KnifeHitreg(args.Item/*args.Player.CurrentItem*/, args.Player.ReferenceHub);
                //if (stab.ClientCalculateHit(out var shot))
                //    stab.ServerProcessShot(shot);
            }
        }

        [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.Highest)]
        public void PlayerDeath(PlayerDyingEvent args)
        {
            if (!GameInProgress || args.Player == null || args.Player.IsServer) return;
            var atckr = args.Attacker ?? Server.Instance;
            var plr = args.Player;

            bool downgrade = args.DamageHandler is JailbirdDamageHandler jdh && (atckr.CurrentItem.ItemTypeId == ItemType.Flashlight || atckr.CurrentItem.ItemTypeId == ItemType.Lantern);

            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;
            try
            {
                plr.ClearInventory();
                plrStats.flags &= ~GGPlayerFlags.validFL | GGPlayerFlags.preFL | GGPlayerFlags.finalLevel;
                if (atckr.IsServer || atckr == plr || !AllPlayers.TryGetValue(atckr.UserId, out var atckrStats))
                {
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    //RemoveScore(plr); //Removes a score if a player dies to natural means
                }
                else
                {
                    plr.AddItem(ItemType.Medkit);
                    plr.ReceiveHint($"{(downgrade ? "<color=red>" : "")}{atckr.Nickname} {(downgrade ? "\"knifed\"" : "killed")} you ({atckrStats.killsLeft})", 2);
                    if (FFA || atckrStats.IsNtfTeam != plrStats.IsNtfTeam)
                    {
                        GG.AddScore(atckr);
                        atckr.ReceiveHint($"{(downgrade ? "<color=red>" : "")}You {(downgrade ? "\"knifed\"" : "killed")} {plr.Nickname} ({plrStats.killsLeft})", 2);
                        if (downgrade)
                            GG.RemoveScore(plr);
                    }
                }
            }
            catch (Exception) { }
            //GG.RollSpawns(plr.Position);
            MEC.Timing.CallDelayed(1, () =>
            {
                GG.SpawnPlayer(plr);
            });
        }
        /*public void PlayerDeath(PlayerDyingEvent args)
        {
            if (!GameInProgress || args.Player == null) return;
            var atckr = args.Attacker ?? Server.Instance;
            var plr = args.Player ?? Server.Instance;

            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return;
            try
            {
                plr.ClearInventory();
                if (atckr.IsServer || atckr == plr || !AllPlayers.TryGetValue(atckr.UserId, out var atckrStats))
                {
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    //RemoveScore(plr); //Removes a score if a player dies to natural means
                }
                else
                {
                    plr.AddItem(ItemType.Medkit);


                    if (atckr.Role == RoleTypeId.Scp0492 || (atckr.CurrentItem.ItemTypeId == ItemType.GunCOM15 && !FFA)) //Triggers win if player is on last level
                    {
                        GG.TriggerWin(atckr);
                        return;
                    }

                    plr.ReceiveHint($"{atckr.Nickname} killed you ({GG.NumKillsReq - atckrStats.Score})", 2);

                    if (atckrStats.IsNtfTeam != plrStats.IsNtfTeam || FFA)
                    {
                        GG.AddScore(atckr);
                        atckr.ReceiveHint($"You killed {plr.Nickname} ({GG.NumKillsReq - plrStats.Score})", 2);
                    }

                }
            }
            catch (Exception) { }
            GG.RollSpawns(plr.Position);
            MEC.Timing.CallDelayed(1, () =>
            {
                GG.SpawnPlayer(plr);
            });
        }*/


        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped
        {
            return !GameInProgress || args.Player.IsTutorial || args.Item.ItemTypeId == ItemType.Medkit;
        }

        [PluginEvent(ServerEventType.PlayerThrowItem)]
        public bool ThrowItem(PlayerThrowItemEvent args) //Stops items from being throwed
        {
            return !GameInProgress || args.Player.IsTutorial;
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        public bool DropAmmo(PlayerDropAmmoEvent args) //Stops ammo from being dropped 
        {
            return !GameInProgress || args.Player.IsTutorial;
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        public bool PlayerPickup(PlayerSearchPickupEvent args)
        {
            var itemID = args.Item.Info.ItemId;
            if (!GameInProgress || args.Player.IsTutorial)
                return true;

            return itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline || itemID == ItemType.GrenadeFlash;//Allows only certain pickups
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
        {
            if (!GameInProgress || args.Player.IsServer)
                return;
            var plr = args.Player;
            GG.AssignTeam(plr);
            plr.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=blue>Race to the final weapon!</color>", 10, shouldClearPrevious: true);
            if (plr.DoNotTrack)
                plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.\nAny existing scores will be deleted as well.</color>", 15);
            MEC.Timing.CallDelayed(3, () =>
            {
                GG.SpawnPlayer(plr);
            });
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
        {
            if (GameInProgress)
                GG.RemovePlayer(args.Player);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
        {
            if (!GameInProgress || !args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
                return;

            var newR = args.NewRole;
            if (newR == RoleTypeId.Overwatch)
                GG.RemovePlayer(args.Player);

            if (newR == RoleTypeId.Spectator)
                MEC.Timing.CallDelayed(0.1f, () =>
                {
                    GG.AssignTeam(args.Player);
                    GG.SpawnPlayer(args.Player);
                });
        }

        [PluginEvent(ServerEventType.PlayerInteractElevator)]
        public bool PlayerInteractElevator(PlayerInteractElevatorEvent args)
        {
            return !GameInProgress || args.Player.IsTutorial;
        }

        [PluginEvent(ServerEventType.PlayerHandcuff)]
        public bool PlayerHandcuff(PlayerHandcuffEvent args)
        {
            //if (EventInProgress)
            //{
            //    args.Target.ReferenceHub.playerEffectsController.ChangeState("SeveredHands", 1);
            //    ExplosionUtils.ServerExplode(args.Player.ReferenceHub);
            //}

            return !GameInProgress || args.Player.IsTutorial;
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public bool RespawnCancel(TeamRespawnEvent args)
        {
            return !GameInProgress;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        public void PlayerEscapeEvent(PlayerEscapeEvent args)
        {
            if (!GameInProgress)
                return;
            var plr = args.Player ?? Server.Instance;
            plr.ClearInventory();
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.ClearInventory();
                GG.GiveGun(plr);
                plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
                plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(200, 99999, false);
                plr.AddItem(ItemType.ArmorHeavy);
                plr.AddItem(ItemType.Painkillers);
                plr.AddItem(ItemType.Adrenaline);
            });

        }

        [PluginEvent(ServerEventType.Scp914UpgradeInventory)]
        public bool InventoryUpgrade(Scp914UpgradeInventoryEvent args)
        {
            var plr = args.Player ?? Server.Instance;
            if (!GameInProgress || plr.IsTutorial)
                return true;

            var knob = args.KnobSetting;
            return plr.CurrentItem.Category == ItemCategory.Medical;
        }

        [PluginEvent(ServerEventType.Scp914ProcessPlayer)]
        public void PlayerUpgrade(Scp914ProcessPlayerEvent args) //Custom effects for player upgrading
        {
            var plr = args.Player ?? Server.Instance;
            if (!GameInProgress || plr.IsTutorial || plr.IsSCP)
                return;

            switch (args.KnobSetting)
            {
                case Scp914KnobSetting.Rough:
                    MEC.Timing.CallDelayed(0.1f, () =>
                    { ExplosionUtils.ServerExplode(plr.ReferenceHub); }); break;

                case Scp914KnobSetting.Coarse:
                    plr.EffectsManager.DisableAllEffects(); break;

                case Scp914KnobSetting.OneToOne:
                    if (!FFA)
                        break;

                    plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                    plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false);
                    plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
                    break;

                case Scp914KnobSetting.VeryFine:
                    plr.EffectsManager.EnableEffect<Scp207>(9999); break;
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractScp330)]
        public void InfiniteCandy(PlayerInteractScp330Event args)
        {
            var plr = args.Player ?? Server.Instance;
            if (!GameInProgress || plr.IsTutorial)
                args.AllowPunishment = true;
            else
            {
                args.AllowPunishment = false;
                System.Random rnd = new System.Random();
                if (rnd.NextDouble() > 0.9f)
                    ExplosionUtils.ServerExplode(plr.ReferenceHub);
            }
        }
    }
}
