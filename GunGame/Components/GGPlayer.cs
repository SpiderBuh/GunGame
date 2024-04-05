using PlayerRoles;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static GunGame.GunGameUtils;
using static GunGame.Plugin;

namespace GunGame.Components
{
    public class GGPlayer : MonoBehaviour
    {
        public ReferenceHub PlayerHub;
        public string Nickname;
        public string Id;
        public PlrInfo PlayerInfo;
        public bool isNTF => PlayerInfo.IsNtfTeam;
        public GGPlayer(Player plr, PlrInfo info)
        {
            PlayerHub = plr.ReferenceHub;
            Nickname = plr.Nickname;
            Id = plr.UserId;
            PlayerInfo = info;
        }

        public Vector2Int GetGridPos()
        {
            //var pos = StampGrid(transform.position, gridSize);
            var pos = StampGrid(PlayerHub.PlayerCameraReference.position, gridSize);
            Cassie.Message($"{Nickname}\t{pos}",true,false,true);
            return pos;
        }

        public void BlockSpawn()
        {
            if (!PlayerInfo.flags.HasFlag(GGPlayerFlags.spawned))
            {
                Cassie.Message($"{Nickname} is not alive", true, false, true);
                return;
            }

            if (isNTF || FFA)
                GG.NTFTiles.Add(GetGridPos());
            else
                GG.ChaosTiles.Add(GetGridPos());
        }
    }
}
