using CommandSystem;
using GunGame.DataSaving;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using static GunGame.DataSaving.WeaponAttachments;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class InitializeFirearms : ICommand, IUsageProvider
    {
        public string Command => "InitializeFirearms";

        public string[] Aliases => null;

        public string[] Usage { get; } = { };

        public string Description => "Loads all the possible firearm attachment variations into an xml file";

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
                GunGameDataManager.SaveData<WeaponDataWrapper>(new WeaponDataWrapper(allFirearms), WeaponAttachments.FileName);
                response += $"Loaded {count} weapons.";
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