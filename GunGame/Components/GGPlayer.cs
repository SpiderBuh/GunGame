using PluginAPI.Core;
using UnityEngine;
using static GunGame.GunGameGame;
using static GunGame.Plugin;

namespace GunGame.Components
{
    public class GGPlayer : MonoBehaviour
    {
        public ReferenceHub PlayerHub;
        public string Nickname;
        public string Id;
        public PlrInfo PlayerInfo;
        public bool IsNTF => PlayerInfo.IsNtfTeam;
        public GGPlayer(Player plr, PlrInfo info)
        {
            PlayerHub = plr.ReferenceHub;
            Nickname = plr.Nickname;
            Id = plr.UserId;
            PlayerInfo = info;
        }

        public Vector2Int GetGridPos()
        {
            return StampGrid(PlayerHub.PlayerCameraReference.position, gridSize);
        }

        public void BlockSpawn()
        {
            if (!PlayerInfo.flags.HasFlag(GGPlayerFlags.spawned))
                return;

            if (IsNTF || GG.FFA)
                GG.NTFTiles.Add(GetGridPos());
            else
                GG.ChaosTiles.Add(GetGridPos());
        }
        void OnDestroy()
        {
            GG.RefreshBlocklist -= BlockSpawn;
        }
    }
}
