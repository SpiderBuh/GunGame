using CustomPlayerEffects;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Jailbird;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using PluginAPI.Core;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace GunGame.HitRegModules
{
    public static class Bonk
    {
        private const int MaxDetections = 128;

        private static readonly CachedLayerMask DetectionMask = new CachedLayerMask("Hitbox", "Glass");

        private static readonly CachedLayerMask LinecastMask = new CachedLayerMask("Default");



        public static bool Bonketh(Player Bonker)
        {
            if (!(Bonker.ReferenceHub.roleManager.CurrentRole is IFpcRole fpcRole))
            {
                return false;
            }
            Collider[] DetectedColliders = new Collider[128];
            IDestructible[] DetectedDestructibles = new IDestructible[128];
            HashSet<uint> DetectedNetIds = new HashSet<uint>();
            HashSet<FpcBacktracker> BacktrackedPlayers = new HashSet<FpcBacktracker>();
            int _detectionsLen;
            var playerCameraReference = Bonker.ReferenceHub.PlayerCameraReference;


            RelativePosition relativePos = new RelativePosition(fpcRole.FpcModule.Position);
            Quaternion quat = Bonker.CurrentItem.Owner.PlayerCameraReference.rotation;
            //DetectDestructibles(Bonker.ReferenceHub.PlayerCameraReference);



            Vector3 position1 = playerCameraReference.position + playerCameraReference.forward * 0.5f;
            Vector3 position2 = playerCameraReference.position + playerCameraReference.forward * 1.25f;
            _detectionsLen = 0;
            int num = Physics.OverlapCapsuleNonAlloc(position1, position2, 0.5f, DetectedColliders, DetectionMask);
            if (num > 0)
            {
                DetectedNetIds.Clear();
                for (int i = 0; i < num; i++)
                {
                    if (DetectedColliders[i].TryGetComponent<IDestructible>(out var component) && (!Physics.Linecast(playerCameraReference.position, component.CenterOfMass, out var hitInfo, LinecastMask) || !(hitInfo.collider != DetectedColliders[i])) && DetectedNetIds.Add(component.NetworkId))
                    {
                        DetectedDestructibles[_detectionsLen++] = component;
                    }
                }
            }





            if (_detectionsLen > 255)
            {
                _detectionsLen = 255;
            }
            Dictionary<ReferenceHub, RelativePosition> bonked = new Dictionary<ReferenceHub, RelativePosition>();
            for (int i = 0; i < _detectionsLen; i++)
            {
                if (DetectedDestructibles[i] is HitboxIdentity hitboxIdentity)
                {
                    ReferenceHub targetHub = hitboxIdentity.TargetHub;
                    bonked.Add(targetHub, new RelativePosition(targetHub));
                }
            }





            ReferenceHub owner = Bonker.ReferenceHub;
            bool result = false;

            BacktrackedPlayers.Add(new FpcBacktracker(owner, relativePos.Position, quat));

            foreach (var bonkee in bonked)
            {

                BacktrackedPlayers.Add(new FpcBacktracker(bonkee.Key, bonkee.Value.Position));

            }


            //DetectDestructibles(Bonker.ReferenceHub.PlayerCameraReference);



            //position = playerCameraReference.position + playerCameraReference.forward * 1.25f;
            _detectionsLen = 0;
            num = Physics.OverlapCapsuleNonAlloc(position1, position2, 0.5f, DetectedColliders, DetectionMask);
            if (num > 0)
            {
                DetectedNetIds.Clear();
                for (int i = 0; i < num; i++)
                {
                    if (DetectedColliders[i].TryGetComponent<IDestructible>(out var component) && (!Physics.Linecast(playerCameraReference.position, component.CenterOfMass, out var hitInfo, LinecastMask) || !(hitInfo.collider != DetectedColliders[i])) && DetectedNetIds.Add(component.NetworkId))
                    {
                        DetectedDestructibles[_detectionsLen++] = component;
                    }
                }
            }


            float num2 = 49;
            Vector3 forward = Bonker.ReferenceHub.PlayerCameraReference.forward;
            for (int j = 0; j < _detectionsLen; j++)
            {
                IDestructible destructible = DetectedDestructibles[j];
                //if (destructible is HitboxIdentity hitboxIdentity && Player.TryGet(hitboxIdentity.TargetHub, out Player bonkee))
                //{
                //    Vector3 relPos = relativePos.Position;
                //    Vector3 pos = bonkee.Position;
                //    if (pos.z < 0 && Math.Abs(pos.x) < Math.Pow(4, (-pos.z) - 0.75f))
                //        num2 *= 5;
                //}
                if (destructible.Damage(num2, new JailbirdDamageHandler(owner, num2, forward), destructible.CenterOfMass))
                {
                    result = true;
                }
            }

            BacktrackedPlayers.ForEach(delegate (FpcBacktracker x)
            {
                x.RestorePosition();
            });
            BacktrackedPlayers.Clear();
            return result;
        }

    }
}
