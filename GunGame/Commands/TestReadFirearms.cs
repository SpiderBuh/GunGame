using CommandSystem;
using GunGame.DataSaving;
using System;
using static GunGame.DataSaving.WeaponAttachments;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TestReadFirearms : ICommand, IUsageProvider
    {

        public string Command => "TestReadFirearms";

        public string[] Aliases => null;

        public string[] Usage { get; } = { };

        public string Description => "Reads the loaded weapons from the XML file";

        public bool SanitizeResponse => true;


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
                response = "Command recieved\n";
            try
            {
                response += "Attempting file read...\n";
                var list = GunGameDataManager.LoadData<WeaponDataWrapper>(WeaponAttachments.FileName);
                response += "Number of weapons in list: " + list.Weapons.Count + "\n";
                response += "Attempting to get random firearm with attachments...\n";
                var rnd = list.GetRandomGat(true, false);
                response += $"Weapon: {rnd.ItemType}\tcode: {rnd.Mod}\n";

                response += "Checking if WeaponData is loaded in Plugin...\n";
                if (AllWeapons == null)
                    response += "WeaponData is null.\n";
                else
                {
                    response += "WeaponData exist with ";
                    response += WeaponData.Weapons.Count + " elements\n";
                    response += "Attempting to grab random weapon from WeaponData...\n";
                    var rnd2 = WeaponData.GetRandomGat();
                    response += $"Weapon: {rnd2.ItemType}\tcode: {rnd2.Mod}\n";
                }
                response += "Testing random code generator...\n";
                var wep = new ggWeapon(ItemType.GunE11SR);
                response += "Random code: " + wep.GetRandomAttachments() + "\n";

                return true;
            }
            catch (Exception e)
            {
                response += "\n" + e.Message + "\n" + e.TargetSite;
                return false;
            }
        }
    }
}