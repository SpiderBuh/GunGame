using CommandSystem;
using GunGame.Game;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using PluginAPI.Core;
using System;
using System.Linq;
using static GunGame.GunGameGame;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SaveWeapon : ICommand
    {
        public string Command => "SaveWeapon";

        public string[] Aliases => null;

        public string Description => "Saves the current weapon being held (with its attachments) to the list of weapons that can appear in Gun Game";
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                var plr = Player.Get(sender);
                var item = plr.CurrentItem;
                if (item == null)
                {
                    response = "You aren't holding anything!";
                    return false;
                }

                if (item.Category != ItemCategory.Firearm)
                    if (arguments.Any() && uint.TryParse(arguments.FirstOrDefault(), out var mod))
                        WeaponManager.AddWeapon(new Gatito(item.ItemTypeId, mod));
                    else
                    {
                        response = "You aren't holding an explicit firearm. If you're holding something like a keycard, people can get stuck and wont be able to play when they reach it.\nOtherwise if you want to override this, specify the number of *additional* items you want to be given out when chosen, as an argument after the command.";
                        return true;
                    }
                else
                    WeaponManager.AddWeapon(new Gatito(item.ItemTypeId, ((Firearm)item).GetCurrentAttachmentsCode()));

                response = "Weapon saved successfully!";
                return true;
            }
            catch (Exception ex)
            {
                response = "Something went wrong.\n" + ex.Message;
                return false;
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TestWeapon : ICommand, IUsageProvider
    {
        public string Command => "TestWeapon";

        public string[] Aliases => null;
        public string[] Usage { get; } = { "Weapon # (leave blank to see all weapons)" };

        public string Description => "Gives you a chosen Gun Game weapon from the saved list to test out";
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                var plr = Player.Get(sender);
                
                if (plr == null || !plr.IsAlive)
                {
                    response = "You must be alive to run this command!";
                    return false;
                }

                if (!arguments.Any())
                {
                    response = WeaponManager.ListWeapons();
                    return true;
                }

                if (!int.TryParse(arguments.FirstOrDefault(), out var idx) || idx >= WeaponManager.AllWeapons.Count || idx < 0)
                {
                    response = "Invalid index";
                    return false;
                }

                Gat currGun = new Gat(WeaponManager.AllWeapons[idx]);
                ItemBase weapon = plr.AddItem(currGun.ItemType);
                if (weapon is Firearm firearm)
                {
                    uint attachment_code = AttachmentsUtils.ValidateAttachmentsCode(firearm, currGun.Mod);
                    AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                    byte ammo_count = currGun.Ammo == 0 ? firearm.AmmoManagerModule.MaxAmmo : currGun.Ammo;
                    firearm.Status = new FirearmStatus(ammo_count, FirearmStatusFlags.MagazineInserted | FirearmStatusFlags.Cocked | FirearmStatusFlags.Chambered, attachment_code);
                }
                else
                    for (var i = currGun.Mod; i > 0 && !plr.IsInventoryFull; i--)
                        plr.AddItem(currGun.ItemType);
                MEC.Timing.CallDelayed(0.1f, () =>
                {
                    plr.CurrentItem = weapon;
                });

                response = $"Weapon {idx} given successfully!";
                return true;
            }
            catch (Exception ex)
            {
                response = "Something went wrong.\n" + ex.Message;
                return false;
            }
        }
    }


    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DeleteWeapon : ICommand, IUsageProvider
    {
        public string Command => "DeleteWeapon";

        public string[] Aliases => null;
        public string[] Usage { get; } = { "Weapon # (leave blank to see all weapons)" };
        public string Description => "Removes a chosen Gun Game weapon from the list (cannot be undone)";
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                if (!arguments.Any())
                {
                    response = WeaponManager.ListWeapons();
                    return true;
                }

                if (!int.TryParse(arguments.FirstOrDefault(), out var idx) || idx >= WeaponManager.AllWeapons.Count || idx < 0)
                {
                    response = "Invalid index";
                    return false;
                }

                WeaponManager.RemoveWeapon(idx);

                response = $"Weapon {idx} deleted successfully!";
                return true;
            }
            catch (Exception ex)
            {
                response = "Something went wrong.\n" + ex.Message;
                return false;
            }
        }
    }
}
