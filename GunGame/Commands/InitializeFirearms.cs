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
    public class InitializeFirearms : ICommand, IUsageProvider
    {

        public string Command => "InitializeFirearms";

        public string[] Aliases => null;

        public string[] Usage { get; } = { };

        public string Description => "Loads all the possible firearm attachment variations into an xml file";

        public bool SanitizeResponse => true;


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
                response = "Command recieved\n";
            try
            {
                int count = 0;
                List<ggWeapon> allFirearms = new List<ggWeapon>();
                Player plr = Player.Get(sender);

                foreach (ItemType item in new List<ItemType>
            {
                ItemType.GunCOM15,
                ItemType.MicroHID,
                ItemType.GunE11SR,
                ItemType.GunCrossvec,
                ItemType.GunFSP9,
                ItemType.GunLogicer,
                ItemType.GunCOM18,
                ItemType.GunRevolver,
                ItemType.GunAK,
                ItemType.GunShotgun,
                ItemType.ParticleDisruptor,
                ItemType.GunCom45,
                ItemType.Jailbird,
                ItemType.GunFRMG0,
                ItemType.GunA7
            })
                {
                    response += "Loading " + item.ToString() + "...\t";
                    allFirearms.Add(new ggWeapon(item));
                    count++;
                    response += "Loaded.\n";
                   
                }
                response += "Attempting save...\n";
                GunGameDataManager.SaveData<WeaponDataWrapper>(new WeaponDataWrapper(allFirearms), WeaponAttachments.FilePath);
                response += $"Loaded {count} weapons.";
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