using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GunGame.Plugin;
using static GunGame.GunGameUtils;
using static GunGame.GunGameEventCommand;
using PluginAPI.Core;
using PluginAPI.Commands;
using PluginAPI.Events;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class FixPlayerCommand : ICommand, IUsageProvider
    {
        public string Command => "fixme";

        public string[] Aliases => null;

        public string[] Usage { get; } = { "Target (leave blank for self)" };

        public string Description => "Runs through some checks to fix a player if they aren't spawning";

        public bool FixOthers = true;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
                response = "\nCommand recieved";
            try
            {
                if (!GameInProgress) {
                    response += "\nGame not in progress.";
                    return false;                    
                }
                Player plr = FixOthers && arguments.Count > 0 ? Player.Get(int.Parse(arguments.First())) : Player.Get(sender);
                if (plr == null || plr.IsServer)
                {
                    response += "\nTarget player doesn't exist.";
                    return false;
                }
                if (plr.Role == PlayerRoles.RoleTypeId.Tutorial || plr.Role == PlayerRoles.RoleTypeId.Overwatch)
                {
                    response += $"\nPlayer is {plr.Role}, they are exempt from playing in this state.";
                    return false;
                }
                if (!AllPlayers.TryGetValue(plr.UserId, out var plrInfo))
                {
                    response += "\nPlayer not found in list, assigning team";
                    GG.AssignTeam(plr);
                }
                else
                {
                    response += "\nPlayer exists in list";
                }
                if (plr.Role == PlayerRoles.RoleTypeId.Spectator)
                    response += "\nPlayer not spawned, attempting spawn";
                else
                    response += "\nPlayer already spawned? Attempting respawn";
                GG.SpawnPlayer(plr);
                plr.ReceiveHint("You were respawned by the playerFix command :^)", 5);
                response += "\nEverything should hopefully be fixed now. If not, then something more than just the player is broken.";
                return true;
            } catch (Exception ex)
            {
                response += "\nYou are so broken, you broke the command to fix you.\n" + ex.Message;
                return false;
            }
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class FixSelfCommand : FixPlayerCommand 
    {
        public new bool FixOthers = false;
        public new string[] Usage { get; } = null;
    }






}
