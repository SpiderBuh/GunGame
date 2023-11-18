using CommandSystem;
using PluginAPI.Core;
using System;
using System.Linq;
using static GunGame.GunGameEventCommand;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ForceFlags : ICommand, IUsageProvider
    {
        public string Command => "ForceFlag";

        public string[] Aliases => null;

        public string[] Usage { get; } = { "Target", "Flags int value" };

        public string Description => "Forces player flags to whatever is specified";


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "\nCommand recieved";
            try
            {
                if (!int.TryParse(arguments.ElementAt(0), out int plrNetID))
                    return false;
                response += $"\nID: \"{plrNetID}\"";

                if (!int.TryParse(arguments.ElementAt(2), out int flags))
                    return false;
                response += $"\tflags: \"{flags}\"";

               if (!Player.TryGet(plrNetID, out var plr))
                    return false;
                response += $"\n{plr.LogName} found";

                if (!AllPlayers.TryGetValue(plr.UserId, out var plrInfo))
                    return false;
                response += "\nplayer info found";

                plrInfo.flags = (GGPlayerFlags)flags;

                response += "\nFlags set successfully.";

                return true;
            }
            catch (Exception ex)
            {
                response += "\nSomething went wrong.\n" + ex.Message;
                return false;
            }
        }
    }







}
