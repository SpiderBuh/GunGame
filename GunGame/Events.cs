using CustomPlayerEffects;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using Scp914;
using System;
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
            EventInProgress = false;
        }

        [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.Highest)]
        public void PlayerDeath(PlayerDyingEvent args)
        {
            if (args.Player == null) return;

            if (EventInProgress && AllPlayers.TryGetValue(args.Player.UserId, out var plrStats))
            {
                var plr = args.Player;
                plr.ClearInventory();

                if (args.Attacker != null && args.Attacker != plr /*&& atckr.IsAlive*/)
                {
                    plr.AddItem(ItemType.Medkit);
                    var atckr = args.Attacker;
                    if (Player.TryGet(plrStats.lastHit[1], out Player assistPlr) && plrStats.lastHit[1] != plr.UserId)
                    {
                        GG.AddScore(assistPlr);
                        assistPlr.ReceiveHint("Assist", 1);
                    }
                    plrStats.lastHit = new string[] { null, null };

                    if (atckr.Role == RoleTypeId.Scp0492 || (atckr.CurrentItem.ItemTypeId == ItemType.GunCOM15 && !FFA)) //Triggers win if player is on last level
                    {
                        GG.TriggerWin(atckr);
                        return;
                    }
                    if (AllPlayers.TryGetValue(atckr.UserId, out var atckrStats))
                    {
                        plr.ReceiveHint($"{atckr.Nickname} killed you ({AllWeapons.Count - atckrStats.Score})", 2);

                        if (atckrStats.IsNtfTeam != plrStats.IsNtfTeam || FFA)
                        {
                            GG.AddScore(atckr);
                            atckr.ReceiveHint($"You killed {plr.Nickname} ({AllWeapons.Count - plrStats.Score})", 2);
                        }
                        else
                        {
                            //RemoveScore(atckr); //Removes score if you kill a teammate
                            atckr.ReferenceHub.playerEffectsController.ChangeState<Sinkhole>(3, 5, false);
                            atckr.ReceiveHint($"<color=red>{plr.Nickname} is a teammate!</color>", 5);
                        }
                    }
                }
                else
                {
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    //RemoveScore(plr); //Removes a score if a player dies to natural means
                }
                if (!FFA)
                {
                    System.Random rnd = new System.Random();
                    credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                    if (credits >= Mathf.Clamp(Player.Count * 10, 30, 100)) //Rolls next spawns if credits high enough, based on player count
                        GG.RollSpawns();
                }
                else GG.RollSpawns();
                MEC.Timing.CallDelayed(1, () =>
                {
                    GG.SpawnPlayer(plr);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        public void PlayerDamageEvent(PlayerDamageEvent args)
        {
            if (!EventInProgress || args.Target == null)
                return;
            if (!AllPlayers.TryGetValue(args.Target.UserId, out var trgtInfo))
                return;
            if (FFA)
                trgtInfo.hit(args.Player.UserId);
            else if (args.Player == null)
                return;
            if (AllPlayers.TryGetValue(args.Player.UserId, out var plrInfo) && (plrInfo.IsNtfTeam != trgtInfo.IsNtfTeam))
                trgtInfo.hit(args.Player.UserId);
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped
        {
            if (EventInProgress && !args.Player.IsTutorial && args.Item.ItemTypeId != ItemType.Medkit)
                return false;

            return true;
        }

        [PluginEvent(ServerEventType.PlayerThrowItem)]
        public bool ThrowItem(PlayerThrowItemEvent args) //Stops items from being throwed
        {
            if (EventInProgress && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        public bool DropAmmo(PlayerDropAmmoEvent args) //Stops ammo from being dropped 
        {
            if (EventInProgress && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        public bool PlayerPickup(PlayerSearchPickupEvent args)
        {
            var itemID = args.Item.Info.ItemId;
            if (EventInProgress && !args.Player.IsTutorial)
                if (itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline || itemID == ItemType.GrenadeFlash) //Allows only certain pickups
                    return true;
                else return false;

            return true;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
        {
            if (EventInProgress)
            {
                Player plr = args.Player;
                GG.AssignTeam(plr);
                plr.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=blue>Race to the final weapon!</color>", 10, shouldClearPrevious: true);
                if (plr.DoNotTrack)
                    plr.ReceiveHint("<color=red>WARNING: You have DNT enabled.\nYour score will not be saved at the end of the round if this is still the case.</color>", 15);
                MEC.Timing.CallDelayed(3, () =>
                {
                    GG.SpawnPlayer(plr);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
        {
            if (EventInProgress && AllPlayers.TryGetValue(args.Player.UserId, out PlrInfo plrInfo))
            {
                if (plrInfo.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                //GG.RemovePlayer(args.Player);
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
        {
            if (EventInProgress && args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
            {
                var newR = args.NewRole;
                MEC.Timing.CallDelayed(0.1f, () =>
                {
                    if (newR == RoleTypeId.Spectator)
                    {
                        GG.AssignTeam(args.Player);
                        GG.SpawnPlayer(args.Player);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractElevator)]
        public bool PlayerInteractElevator(PlayerInteractElevatorEvent args)
        {
            if (EventInProgress && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerHandcuff)]
        public void PlayerHandcuff(PlayerHandcuffEvent args)
        {
            if (EventInProgress)
            {
                args.Target.Health = 1;
                ExplosionUtils.ServerExplode(args.Player.ReferenceHub);
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public bool RespawnCancel(TeamRespawnEvent args)
        {
            if (Plugin.EventInProgress)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        public void PlayerEscapeEvent(PlayerEscapeEvent args)
        {
            if (!EventInProgress)
                return;
            var plr = args.Player;
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
            var knob = args.KnobSetting;
            if (EventInProgress && !args.Player.IsTutorial)
            {
                if (args.Player.CurrentItem.ItemTypeId == ItemType.MicroHID && (knob == Scp914KnobSetting.OneToOne || knob == Scp914KnobSetting.Fine || args.KnobSetting == Scp914KnobSetting.VeryFine)) //Micro recharging
                    return true;
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.Scp914ProcessPlayer)]
        public void PlayerUpgrade(Scp914ProcessPlayerEvent args) //Custom effects for player upgrading
        {
            Player plr = args.Player;
            if (EventInProgress && !plr.IsTutorial && !plr.IsSCP)
            {
                switch (args.KnobSetting)
                {
                    case Scp914KnobSetting.Rough:
                        MEC.Timing.CallDelayed((float)0.1, () =>
                        { ExplosionUtils.ServerExplode(plr.ReferenceHub); }); break;
                    case Scp914KnobSetting.Coarse:
                        plr.EffectsManager.DisableAllEffects(); break;
                    case Scp914KnobSetting.OneToOne:
                        if (FFA)
                        {
                            plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false);
                            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
                        }
                        break;
                    case Scp914KnobSetting.VeryFine:
                        plr.EffectsManager.EnableEffect<Scp207>(9999); break;
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractScp330)]
        public void InfiniteCandy(PlayerInteractScp330Event args)
        {
            Player plr = args.Player;
            if (EventInProgress && !plr.IsTutorial)
            {
                args.AllowPunishment = false;
                System.Random rnd = new System.Random();
                if (rnd.NextDouble() > 0.9f)
                    ExplosionUtils.ServerExplode(plr.ReferenceHub);
            }
            else args.AllowPunishment = true;
        }


        [PluginEvent(ServerEventType.PlayerCoinFlip)] // For testing purposes when I don't have test subjects to experiment on
        public void CoinFlip(PlayerCoinFlipEvent args)
        {
            var plr = args.Player;
            plr.ReceiveHint("Cheater", 1);
            //AddScore(plr);
            try
            {
                GG.TriggerWin(plr);
            }
            catch (Exception ex) { plr.ReceiveHint($"Something broke: {ex.Message}\n{ex.InnerException}\n{ex.StackTrace}\n{ex.Source}", 15); }
        }

        [PluginEvent(ServerEventType.PlayerUnloadWeapon)]
        public void GunUnload(PlayerUnloadWeaponEvent args)
        {
            GG.AddScore(args.Player);
        }
    }
}
