using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GunGame
{
    public class KillFeedComp : Component
    {
        public ReferenceHub hub { get; internal set; }
        //public Dictionary<string, float> feeds { get; internal set; }
        public List<string> feed {  get; internal set; }

        public KillFeedComp(Player player)
        {
            hub = player.ReferenceHub;
        }

        public void RecieveMessage(string victim, string attacker, string mod)
        {
            feed.Prepend(victim + " was killed by " + attacker + " " + mod);
            while (feed.Count > 5) 
                feed.RemoveAt(4);
        }
    }
}
