using CommandSystem;
using PluginAPI.Core;
using System;
using System.Linq;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame.Commands
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GunGameEventCommand : ICommand, IUsageProvider
    {
        public string Command => "gungame";

        public string[] Aliases => null;

        public string Description => "Starts the GunGame event. (Args optional, [X]=default)";

        public bool SanitizeResponse => true;

        public string[] Usage { get; } = { "FFA? (y/[n])", "Zone? ([L]/H/E/S/O)", "Kills to win? [27]", "Round number", /*"Full Shuffle? (y/[n])"*/ };


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                GG = new GunGameUtils(
                    (arguments.Count > 0 && arguments.ElementAt(0).ToUpper() == "Y"),
                    (arguments.Count > 1 ? charZone(arguments.ElementAt(1).ToUpper()[0]) : charZone('L')),
                    ((arguments.Count > 2 && int.TryParse(arguments.ElementAt(2), out int parsedValue)) ? parsedValue : 20)
                    );

                GG.Start();

                response = $"GunGame event has begun. \nFFA: {FFA} | Zone: {zone} | Levels: {GG.NumKillsReq}";
                return true;
            }
            catch (Exception e)
            {
                response = $"An error has occurred: {e.Message}\n{e.TargetSite}\n{e.StackTrace}";
                Round.IsLocked = false;
                return false;
            }
        }
    }
}