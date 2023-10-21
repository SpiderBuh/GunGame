using Discord;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using PluginAPI.Events;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GunGame.HitRegModules
{
    public class GrenadeLauncherHitreg : DisruptorHitreg//IHitregModule, IFirearmModuleBase
    {
        public GrenadeLauncherHitreg(Firearm firearm, ReferenceHub hub, ExplosionGrenade explosionSettings) : base(firearm, hub, explosionSettings)
        {
        }

        protected override void ServerPerformShot(Ray ray)
        {
            ThrowableItem result;
            if (!InventoryItemLoader.TryGetItem<ThrowableItem>(ItemType.GrenadeHE, out result) ||
                !(result.Projectile is ExplosionGrenade projectile))
                return;

            TimeGrenade grenade = (TimeGrenade)UnityEngine.Object.Instantiate(result.Projectile, ray.origin, Quaternion.Euler(ray.direction));
            grenade.NetworkInfo = new PickupSyncInfo(result.ItemTypeId, result.Weight, result.ItemSerial);
            grenade.PreviousOwner = new Footprint(Hub);
            NetworkServer.Spawn(grenade.gameObject);
            grenade.ServerActivate();
            grenade.GetComponent<Rigidbody>().AddForce(ray.direction * 2, ForceMode.VelocityChange);
        }
    }

}
