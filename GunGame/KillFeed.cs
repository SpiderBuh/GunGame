using InventorySystem.Items.Coin;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static GunGame.GunGameUtils;

namespace GunGame
{
    public class KillFeed : MonoBehaviour
    {
        private NetworkConnectionToClient playerConnection;
        //private string plrId;
        private string plrName;
        public static int numLines = 5;

        [Flags]
        public enum KillType : byte
        {
            ChaosKill = 0, //For Chaos attackers
            NtfKill = 1, //For NTF attackers
            FriendlyFire = 0b10, //For FFA mainly, but could happen in teams
            SpecialKill = 0b100, //Win kill, and maybe backstab if I add that eventually?
        }

        public KillFeed(Player plr)
        {
            playerConnection = plr.ReferenceHub.connectionToClient;
            //plrId = plr.UserId;
            plrName = plr.Nickname;
        }


        public void UpdateFeed()
        {
            int startIndex = Math.Max(0, KillList.Count - numLines);
            string sb = "";

            for (int i = startIndex; i < KillList.Count; i++)
            {
                string tag = KillList[i].vctm == plrName || KillList[i].atckr == plrName ? "<b>" : "";
                sb += ($"{tag}{$"<size=-10><align=left><pos=-8em>{KillList[i].atckr} killed {KillList[i].vctm}"}{(tag.Length > 0 ? "</b>" : "")}</align></pos></size>\n");
            }
            Server.Broadcast.TargetClearElements(playerConnection);
            Server.Broadcast.TargetAddElement(playerConnection, sb, 15, Broadcast.BroadcastFlags.Normal);
            //Server.Broadcast.TargetAddElement(playerConnection, "<align=left>amogass</b></align>", ushort.MaxValue, Broadcast.BroadcastFlags.Normal);
        }

    }
}
