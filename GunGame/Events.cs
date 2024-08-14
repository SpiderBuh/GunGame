using CustomPlayerEffects;
using GunGame.Components;
using InventorySystem;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
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
            GG = null;
            bodies = 0;
        }
        int bodies = 0;
        public static FacilityZone trgtZone = FacilityZone.Surface;

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart(RoundStartEvent args)
        {
            System.Random rnd = new System.Random();
            var plrCount = Player.GetPlayers().Count();
            bool ffa = !(plrCount > 16) && (plrCount < 8 || rnd.Next(0, 2) == 1);

            trgtZone = (FacilityZone)(((int)trgtZone + rnd.Next(1, 4) - 1) % 4 + 1); //Random zone excluding the previous one

            int trgtKills = Mathf.Clamp(plrCount * 5 - 10, 10, 30);
            GG = new GunGameUtils(ffa, trgtZone, trgtKills);

            GG.Start();
        }

        [PluginEvent(ServerEventType.PlayerDamage), PluginPriority(LoadPriority.Highest)]
        public bool PlayerDamage(PlayerDamageEvent args)
        {
            if (!GameInProgress || args.Target == null || args.Target.IsServer) return true;
            if ((args.DamageHandler is UniversalDamageHandler UDH) && UDH.TranslationId == DeathTranslations.Falldown.Id) return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.High)]
        public bool PlayerDeath(PlayerDyingEvent args)
        {
            if (!GameInProgress || args.Player == null || args.Player.IsServer) return true;
            var plr = args.Player;
            var atckr = args.Attacker ?? plr;//Server.Instance;
            KillFeed.KillType type = FFA ? KillFeed.KillType.FriendlyFire : 0;

            if ((args.DamageHandler is UniversalDamageHandler UDH) && UDH.TranslationId == DeathTranslations.Falldown.Id) return false;


            if (!AllPlayers.TryGetValue(plr.UserId, out var plrStats))
                return true;
            try
            {
                plr.ClearInventory();
                plrStats.PlayerInfo.flags &= ~GGPlayerFlags.validFL | GGPlayerFlags.preFL | GGPlayerFlags.finalLevel;
                plrStats.PlayerInfo.totDeaths++;
                AllWeapons[plrStats.PlayerInfo.Score].deaths++;
                if (atckr.IsServer || atckr == plr || !AllPlayers.TryGetValue(atckr.UserId, out var atckrStats))
                {
                    type |= KillFeed.KillType.FriendlyFire;
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    //RemoveScore(plr); //Removes a score if a player dies to natural means
                }
                else
                {
                    plr.AddItem(ItemType.Medkit);
                    plr.ReceiveHint($"{atckr.Nickname} killed you \n<alpha=#A0>({atckrStats.PlayerInfo.killsLeft})", 2);

                    if (FFA || atckrStats.PlayerInfo.IsNtfTeam != plrStats.PlayerInfo.IsNtfTeam)
                    {
                        type |= atckrStats.PlayerInfo.IsNtfTeam ? KillFeed.KillType.NtfKill : 0;
                        atckrStats.PlayerInfo.totKills++;
                        AllWeapons[atckrStats.PlayerInfo.Score].kills++;
                        GG.AddScore(atckr);
                        atckr.ReceiveHint($"You killed {plr.Nickname} \n<alpha=#A0>({plrStats.PlayerInfo.killsLeft})", 2);
                        //    if (downgrade)
                        //        GG.RemoveScore(plr);
                    }
                }

                
                KillList.Add(new KillInfo(atckr.Nickname, plr.Nickname, type));
                GG.SendKills();
            }
            catch (Exception) { }
            MEC.Timing.CallDelayed(1, () =>
            {
                GG.SpawnPlayer(plr);
            });

            if (InventoryItemLoader.TryGetItem<Medkit>(ItemType.Medkit, out var med))
            {
                ItemPickupBase medkit = UnityEngine.Object.Instantiate(med.PickupDropModel, plr.Position, UnityEngine.Random.rotation);
                medkit.NetworkInfo = new PickupSyncInfo(ItemType.Medkit, med.Weight);
                var timer = medkit.gameObject.AddComponent(typeof(TimedObject));
                timer = new TimedObject(45, medkit.gameObject);
                NetworkServer.Spawn(medkit.gameObject);
            }

                bodies++;
            if (bodies >= 75)
            {
                BasicRagdoll[] array = (from r in UnityEngine.Object.FindObjectsOfType<BasicRagdoll>()
                                        orderby r.Info.CreationTime descending
                                        select r).ToArray();
                for (int i = 0; i < 25; i++)
                {
                    NetworkServer.Destroy(array[i].gameObject);
                }
                bodies -= 25;
            }
            return true;
        }


        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped
        {
            return !GameInProgress || args.Player.IsTutorial || args.Item.ItemTypeId == ItemType.Medkit;
        }

        [PluginEvent(ServerEventType.PlayerThrowItem)]
        public bool ThrowItem(PlayerThrowItemEvent args) //Stops items from being thrown
        {
            return !GameInProgress || args.Player.IsTutorial || args.Item.ItemTypeId == ItemType.Medkit;
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

            return itemID == ItemType.SCP330 || itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline || itemID == ItemType.GrenadeFlash;//Allows only certain pickups
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
        {
            if (!GameInProgress || args.Player.IsServer)
                return;
            var plr = args.Player;
            GG.AssignTeam(plr);
            plr.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \nRace to the final weapon!", 10, shouldClearPrevious: true);
            //   if (plr.DoNotTrack)
            //       plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.\nAny existing scores will be deleted as well.</color>", 15);
            MEC.Timing.CallDelayed(3, () =>
            {
                GG.SpawnPlayer(plr);
            });
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
        {
            if (GameInProgress)
                GG.RemovePlayer(args.Player.UserId);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
        {
            if (!GameInProgress || !args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
                return;

            var newR = args.NewRole;
            if (newR == RoleTypeId.Overwatch)
                GG.RemovePlayer(args.Player.UserId);

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
                    plr.EffectsManager.EnableEffect<CustomPlayerEffects.Scp207>(9999); break;
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
