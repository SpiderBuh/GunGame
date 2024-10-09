using CommandSystem;
using GunGame.DataSaving;
using System;
using System.Collections.Generic;
using static GunGame.DataSaving.WeaponAttachments;
using static GunGame.GunGameGame;
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
                WeaponDataWrapper list = (WeaponDataWrapper)new WeaponAttachments().Wrapper;
                response += "Number of weapons in list: " + list.Weapons.Count + "\n";

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