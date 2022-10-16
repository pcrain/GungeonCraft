using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class RocketMan : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Rocket Man";
            string resourceName = "NevernamedsItems/Resources/rocketman_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<RocketMan>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Gonna be a long long time";
            string longDesc = "Chance to fire random rockets." + "\n\nThe prized relic of a reclusive group of Detoknights, though in truth it does not belong to them at all...";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            RocketManID = item.PickupObjectId;
        }
        public static int RocketManID;
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            ShootRocket(effectChanceScalar);
        }
        private void PostProcessBeamTick(BeamController whatTheFuckDoesThisDo, SpeculativeRigidbody beam, float effectChanceScalar)
        {
            ShootRocket(1);
        }
        private void ShootRocket(float effectChanceScalar)
        {
            float procChance = 0.05f;
            if (Owner.HasPickupID(106))
            {
                procChance *= 3f;
            }
            procChance *= effectChanceScalar;
            if (UnityEngine.Random.value <= procChance)
            {
                int selectedRocket = BraveUtility.RandomElement(RocketIDs);
                Projectile projectile = ((Gun)ETGMod.Databases.Items[selectedRocket]).DefaultModule.projectiles[0];
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    if (Owner.HasPickupID(selectedRocket) && selectedRocket != 739)
                    {
                        component.baseData.damage *= 2f;
                    }
                    else if (Owner.HasPickupID(176) && selectedRocket == 739)
                    {
                        component.baseData.damage *= 2f;
                    }
                    component.Owner = base.Owner;
                    component.Shooter = base.Owner.specRigidbody;
                }
            }
        }
        public static List<int> RocketIDs = new List<int>()
        {
            92, //Stinger
            39, //RPG
            563, //The Exotic
            129, //Comand0
            372, //RC Rocket
            16, //Yari Launcher
            593, //Void Core Cannon
            362, //Bullet Bore
            739, //Gungeon Ant --> Great Queen Ant
        };
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeamTick -= this.PostProcessBeamTick;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeamTick += this.PostProcessBeamTick;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
                Owner.PostProcessBeamTick -= this.PostProcessBeamTick;
            }
            base.OnDestroy();
        }
    }
}
