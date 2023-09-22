using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GunGame.Plugin;
using static GunGame.GunGameEventCommand;
using PluginAPI.Core;
using PluginAPI.Commands;
using PluginAPI.Events;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ListPositionsCommand : ICommand//, IUsageProvider
    {
        public string Command => "ggPos";

        public string[] Aliases => null;

        public string Description => "Lists the current GunGame positions, if a game is active";

        //public string[] Usage { get; } = { };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!GameInProgress)
            {
                response = "Game not in progress";
                return false; 
            }
            bool indent = Player.TryGet(sender, out var plr) && !plr.IsServer;
            var plrs = GG.Positions();
            response = "\nPositions:";
            int i = 1;
            foreach (var item in plrs)
            {
                string ordinal = (i >= 11 && i <= 13) ? "th"
                       : (i % 10 == 1) ? "st"
                       : (i % 10 == 2) ? "nd"
                       : (i % 10 == 3) ? "rd"
                       : "th";
                response += $"\n{( indent&&plr.Nickname.Equals(item.Key) ? " # " : "" )} - {i}{ordinal}: {item.Key} [{item.Value}]";
                i++;
            }
            return true;
        }
    }
}
