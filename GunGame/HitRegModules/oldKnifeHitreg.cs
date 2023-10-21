using Decals;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using PlayerStatsSystem;
using System;
using UnityEngine;

namespace GunGame
{
    public class oldKnifeHitreg : StandardHitregBase
    {
        protected override Firearm Firearm { get; set; }
        protected override ReferenceHub Hub { get; set; }

        protected override DecalPoolType BulletHoleDecal => customBulletHole;
        public DecalPoolType customBulletHole;
        public oldKnifeHitreg(Firearm firearm, ReferenceHub hub, DecalPoolType holeDecal = DecalPoolType.Bullet)
        {
            Firearm = firearm;
            Hub = hub;
            customBulletHole = holeDecal;
        }
        protected override void ServerPerformShot(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit rayHit, 2, (int)HitregMask))
                return;

            if (!rayHit.collider.TryGetComponent(out HitboxIdentity component))
                return;

            var hub = component.TargetHub;
            if (component.TargetHub == null)
                return;

            //if (!ReferenceHub.TryGetHubNetID(component.NetworkId, out var hub) || !hub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
            //    return;

            float damage;
            switch (component.HitboxType)
            {
                case HitboxType.Limb:
                    damage = 15; goto DealDamage;
                case HitboxType.Body:
                    damage = 30; break;
                case HitboxType.Headshot:
                    damage = 45; break;
                default:
                    return;
            }

            var pos = Quaternion.FromToRotation(new Vector3(hub.PlayerCameraReference.forward.x, 0, hub.PlayerCameraReference.forward.y), Vector3.left)*(Hub.PlayerCameraReference.position - hub.PlayerCameraReference.position);
            if (Math.Abs(pos.z) < Math.Pow(2, pos.x - 1.5f))
                damage *= 5;

            DealDamage:
            hub.playerStats.DealDamage(new JailbirdDamageHandler(Hub, damage, Hub.PlayerCameraReference.forward));
            ShowHitIndicator(component.NetworkId, damage, ray.origin);     
            
            
        }
    }
}
