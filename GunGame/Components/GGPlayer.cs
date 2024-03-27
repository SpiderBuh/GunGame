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
        public GGPlayerFlags Flags;
        public bool isNTF => PlayerInfo.IsNtfTeam;
        public GGPlayer(Player plr, PlrInfo info)
        {
            PlayerHub = plr.ReferenceHub;
            Nickname = plr.Nickname;
            Id = plr.UserId;
            PlayerInfo = info;
            Flags = info.IsNtfTeam ? GGPlayerFlags.NTF : GGPlayerFlags.Chaos;
        }

        public Vector2Int GetGridPos()
        {
            return StampGrid(transform.parent.position, gridSize);
        }

        public void BlockSpawn()
        {
            if (!Flags.HasFlag(GGPlayerFlags.spawned))
                return;

            if (isNTF)
                GG.NTFTiles.Add(GetGridPos());
            else
                GG.ChaosTiles.Add(GetGridPos());
        }
    }
}
