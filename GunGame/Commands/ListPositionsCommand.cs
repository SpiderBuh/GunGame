using CommandSystem;
using PluginAPI.Core;
using System;
using static GunGame.Plugin;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ListPositionsCommand : ICommand
    {
        public string Command => "ggPos";

        public string[] Aliases => null;

        public string Description => "Lists the current GunGame positions, if a game is active";

        public bool SanitizeResponse => true;

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
                string ordinal = GunGameUtils.ordinal(i);
                response += $"\n{(indent && plr.Nickname.Equals(item.Key) ? " # " : "")} - {i}{ordinal}: {item.Key} [{item.Value}]";
                i++;
            }
            return true;
        }
    }
}
