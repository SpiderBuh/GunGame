using CommandSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GunGame.Commands
{/*
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    //[CommandHandler(typeof(ClientCommandHandler))]
    public class GetLeaderboardCommand : ICommand//, IUsageProvider
    {
        public string Command => "ggScores";

        public string[] Aliases => null;

        //public string[] Usage { get; } = { };

        public string Description => "Returns the GunGame leaderboard";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool indent = Player.TryGet(sender, out var plr) && !plr.IsServer;
            var data = GunGameDataManager.LoadData();
            Dictionary<PlayerData, int> PointTotals = data.Players.ToDictionary(k => k, v => data.Scores //Grabs from PlayerData and ScoreData
                                                                                .Where(x => x.UserID.Equals(v.UserID)).Sum(y => y.Score)) //Gets all scores linked to v's UserID, and sums them
                                                                                .OrderByDescending(z => z.Value).ThenBy(z => z.Key.Nickname) //Orders by score then name
                                                                                .ToDictionary(k => k.Key, v => v.Value); //ToDictionary cus it shits itself otherwise
            response = "\nLeaderboard:";
            int i = 1;
            foreach (var item in PointTotals)
            {
                string ordinal = (i >= 11 && i <= 13) ? "th"
                       : (i % 10 == 1) ? "st"
                       : (i % 10 == 2) ? "nd"
                       : (i % 10 == 3) ? "rd"
                       : "th";
                response += $"\n{(indent && plr.UserId.Equals(item.Key.UserID) ? " # " : "")} - {i}{ordinal}: {item.Key.Nickname}\tScore: {item.Value}";
                i++;
            }
            return true;
        }
    }







    */
}
