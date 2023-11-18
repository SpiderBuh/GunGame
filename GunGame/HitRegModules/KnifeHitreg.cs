/*using Decals;
using GunGame.HitRegModules;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Jailbird;
using Mirror;
using PlayerStatsSystem;
using RelativePositioning;
using System;
using System.ComponentModel;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace GunGame
{
    public class KnifeHitreg : IHitregModule, IFirearmModuleBase
    {
        public static readonly LayerMask HitregMask = LayerMask.GetMask("Default", "Hitbox", "Glass", "CCTV", "Door");
        protected ReferenceHub Hub { get; set; }
        protected ItemBase Item { get; set; }
        protected IDestructible HitTarget { get; set; }
        protected RaycastHit HitRay { get; set; }
        public KnifeHitreg(ItemBase item, ReferenceHub hub)
        {
            Hub = hub;
            Item = item;
        }

        public bool Standby => true;

        public bool ClientCalculateHit(out ShotMessage message)
        {
            var cam = Hub.PlayerCameraReference;
            message = new ShotMessage() { ShooterCameraRotation = cam.rotation, ShooterPosition = new RelativePosition(Hub), ShooterWeaponSerial = Item.ItemSerial, TargetRotation = Quaternion.identity};
            if (!Physics.Raycast(Hub.PlayerCameraReference.position, Hub.PlayerCameraReference.forward, out RaycastHit rayHit, 2.5f, (int)HitregMask))
                return false;

            if (!rayHit.collider.TryGetComponent(out IDestructible destructibleComp))
                return false;

            HitRay = rayHit;
            message.TargetNetId = destructibleComp.NetworkId;
            message.TargetPosition = new RelativePosition(destructibleComp.CenterOfMass);

            if (!(destructibleComp is HitboxIdentity component))
            {
                HitTarget = destructibleComp;
                return true;
            }

            HitTarget = component;
            var hub = component.TargetHub;
            if (component.TargetHub == null)
                return true;

            message.TargetPosition = new RelativePosition(hub);
            message.TargetRotation = hub.PlayerCameraReference.rotation;

            return true;
        }

        public void ServerProcessShot(ShotMessage message)
        {
            float damage = 25;
            bool backstabbed = false;
            if (!(HitTarget is HitboxIdentity bodyPart) || !ReferenceHub.TryGetHubNetID(message.TargetNetId, out var hub))
            {
                HitTarget.Damage(damage, new KnifeDamageHandler(Hub, damage, Hub.PlayerCameraReference.forward), HitRay.point);
                //Hitmarker.SendHitmarker(Hub, 0.5f);
                return;
            }

            switch (bodyPart.HitboxType)
            {
                case HitboxType.Body:
                    damage = 20; break;
                case HitboxType.Headshot:
                    damage = 35; break;
                default:
                    damage = 15;
                    HitTarget.Damage(damage, new KnifeDamageHandler(Hub, damage, Hub.PlayerCameraReference.forward), HitRay.point);
                    //Hitmarker.SendHitmarker(Hub, 0.5f);
                    return;
            }

            //var pos = Quaternion.FromToRotation(hub.PlayerCameraReference.forward.NormalizeIgnoreY(), Vector3.left) * (Hub.PlayerCameraReference.position - hub.PlayerCameraReference.position);
            var pos = hub.PlayerCameraReference.InverseTransformPoint(Hub.PlayerCameraReference.position);
            if (pos.z < 0 && Math.Abs(pos.x) < Math.Pow(4, (-pos.z) - 0.25f))
                backstabbed = true;//damage *= 10; //Backstab calculation ^
            hub.playerStats.DealDamage(new KnifeDamageHandler(Hub, damage, Hub.PlayerCameraReference.forward, backstabbed));
            //Hitmarker.SendHitmarker(Hub, backstabbed ? 5 : 1);

        }


    }
}*/
