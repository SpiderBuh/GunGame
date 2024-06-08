using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using PluginAPI.Core;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Tokens;
using static GunGame.GunGameEventCommand;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;
using static GunGame.DataSaving.WeaponAttachments;
using GunGame.DataSaving;

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
                var list = GunGameDataManager.LoadData<WeaponDataWrapper>(WeaponAttachments.FilePath);
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

        /*public List<uint> iterateAttachments(List<int> slots, uint pointer = 1)
        {
            List<uint> otherAtts;
            List<uint> result = new List<uint>();
            if (slots.Any() && slots.Count > 1)
                otherAtts = iterateAttachments(new List<int>(slots.Skip(1)), Convert.ToUInt32(pointer * Math.Pow(2, slots.FirstOrDefault())));
            else
            {
                otherAtts = new List<uint>();
                for (int i = 0; i < slots.FirstOrDefault(); i++)
                    otherAtts.Add(0);
            }

            for (int i = 0; i < slots.FirstOrDefault(); i++)
            {
                uint val = Convert.ToUInt32(pointer * Math.Pow(2, i));
                foreach (var code in otherAtts)
                    result.Add(code + val);
            }
            return result;
        }*/

    }
}